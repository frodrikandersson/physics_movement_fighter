using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WeaponPhysics : MonoBehaviour
{
    [Header("Weapon Properties")]
    [SerializeField] private float weaponMass = 5f;
    [SerializeField] private float weaponLength = 1.5f;

    [Header("Character Strength")]
    [SerializeField] private float characterStrength = 1f;

    [Header("Control Settings")]
    [SerializeField] private float swingSpeed = 0.03f;
    [SerializeField] private float followForce = 300f;  // Stronger attraction to orbit

    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 1.5f;
    [SerializeField] private float minAngle = -90f;   // Back of character
    [SerializeField] private float maxAngle = 90f;    // Front of character

    [Header("Debug")]
    [SerializeField] private bool showOrbit = true;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    private ThirdPersonCamera thirdPersonCamera;

    private Rigidbody rb;
    private PlayerHands playerHands;

    // Swing angle: 0 = right side, +90 = front, -90 = back
    private float swingAngle = 0f;

    // Orbit visualization object
    private LineRenderer orbitLine;

    private Vector3 TipPosition => transform.position + transform.forward * (weaponLength * 0.5f);
    private Vector3 HiltPosition => transform.position - transform.forward * (weaponLength * 0.5f);

    // Orbit center is at shoulder height
    private Vector3 OrbitCenter => playerMovement.transform.position +
                                    playerMovement.transform.up * 0.9f;

    public Vector3 Velocity => rb.linearVelocity;
    public float WeaponMass => weaponMass;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = weaponMass;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 8f;   // Higher damping for less overshoot
        rb.angularDamping = 8f;
    }

    private void Start()
    {
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();

        playerHands = playerMovement.GetComponent<PlayerHands>();
        if (playerHands == null)
            playerHands = playerMovement.gameObject.AddComponent<PlayerHands>();
        playerHands.SetWeapon(transform);

        CreateOrbitVisual();
        PositionSword();
    }

    private void CreateOrbitVisual()
    {
        GameObject orbitObj = new GameObject("OrbitVisual");
        orbitObj.transform.SetParent(playerMovement.transform);
        orbitLine = orbitObj.AddComponent<LineRenderer>();
        orbitLine.positionCount = 51;
        orbitLine.startWidth = 0.05f;
        orbitLine.endWidth = 0.05f;
        orbitLine.useWorldSpace = true;
        orbitLine.material = new Material(Shader.Find("Sprites/Default"));
        orbitLine.startColor = Color.yellow;
        orbitLine.endColor = Color.yellow;
        orbitLine.loop = false;
    }

    private void PositionSword()
    {
        Vector3 tipTarget = GetPointOnOrbit(swingAngle);
        Vector3 handPos = playerHands.GetHandPosition();
        Vector3 direction = (tipTarget - handPos).normalized;

        transform.position = handPos + direction * (weaponLength * 0.5f);
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Update()
    {
        if (showOrbit)
            UpdateOrbitVisual();
    }

    private void UpdateOrbitVisual()
    {
        if (orbitLine == null || playerMovement == null) return;

        orbitLine.enabled = true;

        for (int i = 0; i <= 50; i++)
        {
            float t = (float)i / 50f;
            float angle = Mathf.Lerp(minAngle, maxAngle, t);
            Vector3 point = GetPointOnOrbit(angle);
            orbitLine.SetPosition(i, point);
        }
    }

    private void FixedUpdate()
    {
        if (InputHandler.Instance == null || playerMovement == null) return;

        UpdateSwingAngle();
        MoveTowardOrbit();
        KeepHiltAtHand();
        TransferMomentumToPlayer();
    }

    private void UpdateSwingAngle()
    {
        Vector2 input = InputHandler.Instance.WeaponInput;

        if (Mathf.Abs(input.y) > 0.001f)
        {
            float effectiveSpeed = swingSpeed * characterStrength / Mathf.Sqrt(weaponMass);
            swingAngle += input.y * effectiveSpeed * 100f;
            swingAngle = Mathf.Clamp(swingAngle, minAngle, maxAngle);
        }

        InputHandler.Instance.ConsumeWeaponInput();
    }

    private Vector3 GetPointOnOrbit(float angle)
    {
        Vector3 center = OrbitCenter;

        // Get camera angles directly - this ensures orbit follows camera instantly
        float cameraYaw = thirdPersonCamera != null ? thirdPersonCamera.Yaw : playerMovement.transform.eulerAngles.y;
        float cameraPitch = thirdPersonCamera != null ? thirdPersonCamera.Pitch : 0f;

        // Build the full rotation: first yaw (horizontal), then pitch (vertical tilt)
        Quaternion yawRotation = Quaternion.Euler(0, cameraYaw, 0);

        // Calculate point on a flat circle (in local space)
        // X = right, Z = forward relative to camera facing
        float angleRad = angle * Mathf.Deg2Rad;
        Vector3 localPoint = new Vector3(
            Mathf.Cos(angleRad) * orbitRadius,  // X = right
            0,                                    // Y = up (will be tilted by pitch)
            Mathf.Sin(angleRad) * orbitRadius   // Z = forward
        );

        // Apply yaw rotation to get world-space right axis for pitch rotation
        Vector3 worldRight = yawRotation * Vector3.right;

        // Apply pitch rotation around the yaw-rotated right axis
        Quaternion pitchRotation = Quaternion.AngleAxis(cameraPitch, worldRight);

        // First rotate by yaw, then tilt by pitch
        Vector3 rotatedPoint = pitchRotation * (yawRotation * localPoint);

        // Convert to world space (just add to center since rotations are already world-space)
        Vector3 worldPoint = center + rotatedPoint;

        return worldPoint;
    }

    private void MoveTowardOrbit()
    {
        // Get the target point on the orbit for current swing angle
        Vector3 targetTip = GetPointOnOrbit(swingAngle);
        Vector3 currentTip = TipPosition;
        Vector3 toTarget = targetTip - currentTip;

        float effectiveForce = followForce * characterStrength / Mathf.Sqrt(weaponMass);
        rb.AddForceAtPosition(toTarget * effectiveForce, currentTip, ForceMode.Force);
    }

    private void KeepHiltAtHand()
    {
        Vector3 handPos = playerHands.GetHandPosition();
        Vector3 currentHilt = HiltPosition;
        Vector3 correction = handPos - currentHilt;

        rb.AddForceAtPosition(correction * 500f, currentHilt, ForceMode.Force);
    }

    private Vector3 GetTipVelocity()
    {
        Vector3 radiusToTip = TipPosition - rb.worldCenterOfMass;
        return rb.linearVelocity + Vector3.Cross(rb.angularVelocity, radiusToTip);
    }

    private void TransferMomentumToPlayer()
    {
        if (playerMovement == null) return;

        Vector3 tipVelocity = GetTipVelocity();
        Vector3 swordDirection = transform.forward;

        float forwardSpeed = Vector3.Dot(tipVelocity, swordDirection);

        if (forwardSpeed > 0)
        {
            Vector3 momentumDirection = swordDirection.normalized;
            float momentumMagnitude = forwardSpeed * weaponMass;
            playerMovement.ApplyWeaponMomentum(momentumDirection, momentumMagnitude);
        }
    }

    public void SetMass(float newMass)
    {
        weaponMass = newMass;
        rb.mass = weaponMass;
    }

    public void SetStrength(float newStrength)
    {
        characterStrength = newStrength;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || playerHands == null || playerMovement == null) return;

        // Draw orbit center
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(OrbitCenter, 0.1f);

        // Draw target point on orbit
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GetPointOnOrbit(swingAngle), 0.12f);

        // Draw current tip
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(TipPosition, 0.08f);

        // Draw hand position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(playerHands.GetHandPosition(), 0.08f);
    }
}
