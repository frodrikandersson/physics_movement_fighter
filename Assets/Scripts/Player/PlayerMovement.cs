using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float momentumTransferRate = 0.5f;
    [SerializeField] private float maxHorizontalSpeed = 20f;
    [SerializeField] private float maxVerticalSpeed = 15f;
    [SerializeField] private float groundDrag = 3f;
    [SerializeField] private float airDrag = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Facing Direction")]
    [SerializeField] private float turnSpeed = 10f;

    private Rigidbody rb;

    public bool IsGrounded { get; private set; }
    public Vector3 Velocity => rb.linearVelocity;

    // Directional momentum (now includes full 3D direction)
    private Vector3 pendingMomentumDirection;
    private float pendingMomentumMagnitude;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        CheckGround();
        ApplyDrag();
        ProcessPendingMomentum();
        ClampSpeed();
    }

    private void CheckGround()
    {
        IsGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance + 0.1f,
            groundLayer
        );
    }

    private void ApplyDrag()
    {
        rb.linearDamping = IsGrounded ? groundDrag : airDrag;
    }

    // New: accepts direction and magnitude (for full 3D momentum including Y)
    public void ApplyWeaponMomentum(Vector3 direction, float magnitude)
    {
        pendingMomentumDirection = direction.normalized;
        pendingMomentumMagnitude += magnitude;
    }

    // Legacy: for backward compatibility (uses player forward)
    public void ApplyWeaponMomentum(float forwardMomentum)
    {
        ApplyWeaponMomentum(transform.forward, forwardMomentum);
    }

    private void ProcessPendingMomentum()
    {
        if (pendingMomentumMagnitude <= 0.01f)
        {
            pendingMomentumMagnitude = 0;
            return;
        }

        // Apply force in the sword's swing direction (full 3D)
        Vector3 force = pendingMomentumDirection * pendingMomentumMagnitude * momentumTransferRate;
        rb.AddForce(force, ForceMode.Force);

        pendingMomentumMagnitude = 0;
    }

    private void ClampSpeed()
    {
        Vector3 vel = rb.linearVelocity;

        // Clamp horizontal speed
        Vector3 horizontalVel = new Vector3(vel.x, 0, vel.z);
        if (horizontalVel.magnitude > maxHorizontalSpeed)
        {
            horizontalVel = horizontalVel.normalized * maxHorizontalSpeed;
        }

        // Clamp vertical speed (but allow falling)
        float verticalVel = Mathf.Clamp(vel.y, -50f, maxVerticalSpeed);

        rb.linearVelocity = new Vector3(horizontalVel.x, verticalVel, horizontalVel.z);
    }

    public void SetFacingDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        direction.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    public void LookAt(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        SetFacingDirection(direction);
    }
}
