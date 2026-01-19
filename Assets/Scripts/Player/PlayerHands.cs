using UnityEngine;

public class PlayerHands : MonoBehaviour
{
    [Header("Hand Visuals")]
    [SerializeField] private Transform rightHand;
    [SerializeField] private Transform leftHand;

    [Header("Arm Segments")]
    [SerializeField] private Transform rightUpperArm;
    [SerializeField] private Transform rightForearm;
    [SerializeField] private Transform leftUpperArm;
    [SerializeField] private Transform leftForearm;

    [Header("Body Configuration")]
    [SerializeField] private float shoulderWidth = 0.3f;
    [SerializeField] private float shoulderHeight = 0.9f; // From player center
    [SerializeField] private float upperArmLength = 0.45f; // Longer arms
    [SerializeField] private float forearmLength = 0.45f;  // Longer arms
    [SerializeField] private float handSpacing = 0.15f;

    [Header("References")]
    [SerializeField] private Transform weapon;

    private float weaponLength;

    // Shoulder positions
    private Vector3 RightShoulderPos => transform.position +
                                        transform.right * shoulderWidth +
                                        transform.up * shoulderHeight;
    private Vector3 LeftShoulderPos => transform.position +
                                       transform.right * (shoulderWidth * 0.5f) +
                                       transform.up * shoulderHeight;

    private void Start()
    {
        CreateArmsAndHands();
    }

    private void CreateArmsAndHands()
    {
        Color skinColor = new Color(0.9f, 0.75f, 0.6f);
        Color armColor = new Color(0.3f, 0.3f, 0.5f); // Slightly blue for visibility

        // Right arm
        rightUpperArm = CreateArmSegment("RightUpperArm", armColor);
        rightForearm = CreateArmSegment("RightForearm", armColor);
        rightHand = CreateHand("RightHand", skinColor);

        // Left arm
        leftUpperArm = CreateArmSegment("LeftUpperArm", armColor);
        leftForearm = CreateArmSegment("LeftForearm", armColor);
        leftHand = CreateHand("LeftHand", skinColor);
    }

    private Transform CreateArmSegment(string segmentName, Color color)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        segment.name = segmentName;
        segment.transform.SetParent(transform);

        // Remove collider
        Destroy(segment.GetComponent<Collider>());

        // Set color
        var renderer = segment.GetComponent<Renderer>();
        renderer.material.color = color;

        return segment.transform;
    }

    private Transform CreateHand(string handName, Color color)
    {
        GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hand.name = handName;
        hand.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        hand.transform.SetParent(transform);

        // Remove collider
        Destroy(hand.GetComponent<Collider>());

        // Set color
        var renderer = hand.GetComponent<Renderer>();
        renderer.material.color = color;

        return hand.transform;
    }

    private void LateUpdate()
    {
        if (weapon == null) return;

        UpdateHandPositions();
        UpdateArmIK();
    }

    private void UpdateHandPositions()
    {
        weaponLength = weapon.localScale.z;

        // Hilt center (back of sword)
        Vector3 hiltCenter = weapon.position - weapon.forward * (weaponLength * 0.5f);

        // Right hand closer to blade, left hand at pommel
        rightHand.position = hiltCenter + weapon.forward * handSpacing;
        leftHand.position = hiltCenter - weapon.forward * handSpacing;

        rightHand.rotation = weapon.rotation;
        leftHand.rotation = weapon.rotation;
    }

    private void UpdateArmIK()
    {
        // Simple two-bone IK for each arm
        SolveArmIK(RightShoulderPos, rightHand.position, rightUpperArm, rightForearm, upperArmLength, forearmLength);
        SolveArmIK(LeftShoulderPos, leftHand.position, leftUpperArm, leftForearm, upperArmLength, forearmLength);
    }

    private void SolveArmIK(Vector3 shoulder, Vector3 hand, Transform upperArm, Transform forearm, float upperLen, float lowerLen)
    {
        Vector3 toHand = hand - shoulder;
        float distance = toHand.magnitude;

        // Clamp distance to arm reach
        float maxReach = upperLen + lowerLen - 0.01f;
        float minReach = Mathf.Abs(upperLen - lowerLen) + 0.01f;
        distance = Mathf.Clamp(distance, minReach, maxReach);

        // Recalculate hand position if clamped
        Vector3 direction = toHand.normalized;
        Vector3 targetHand = shoulder + direction * distance;

        // Calculate elbow position using law of cosines
        // a = upperLen, b = lowerLen, c = distance
        float cosAngle = (upperLen * upperLen + distance * distance - lowerLen * lowerLen) / (2 * upperLen * distance);
        cosAngle = Mathf.Clamp(cosAngle, -1f, 1f);
        float angle = Mathf.Acos(cosAngle);

        // Elbow bends "outward" - use a hint direction
        Vector3 elbowHint = transform.up + transform.right * 0.5f; // Elbow points up and out
        Vector3 cross = Vector3.Cross(direction, elbowHint).normalized;
        Vector3 elbowDir = Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, cross) * direction;

        Vector3 elbow = shoulder + elbowDir * upperLen;

        // Position and orient upper arm (shoulder to elbow)
        PositionArmSegment(upperArm, shoulder, elbow, upperLen);

        // Position and orient forearm (elbow to hand)
        PositionArmSegment(forearm, elbow, targetHand, lowerLen);
    }

    private void PositionArmSegment(Transform segment, Vector3 start, Vector3 end, float length)
    {
        Vector3 center = (start + end) * 0.5f;
        Vector3 direction = (end - start).normalized;

        segment.position = center;
        segment.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);
        segment.localScale = new Vector3(0.06f, length * 0.5f, 0.06f); // Capsule scale
    }

    public void SetWeapon(Transform weaponTransform)
    {
        weapon = weaponTransform;
    }

    // Get the position where the hilt should be (center between hands)
    public Vector3 GetHandPosition()
    {
        return transform.position +
               transform.right * 0.6f +   // Further from body
               transform.forward * 0.3f + // Slightly in front
               transform.up * 0.7f;
    }

    // For debug visualization
    private void OnDrawGizmos()
    {
        // Draw shoulders
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(RightShoulderPos, 0.05f);
        Gizmos.DrawWireSphere(LeftShoulderPos, 0.05f);
    }
}
