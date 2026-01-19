using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Position Settings")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float followSpeed = 10f;

    [Header("Rotation Settings")]
    [SerializeField] private float horizontalSensitivity = 0.3f;
    [SerializeField] private float verticalSensitivity = 0.3f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;

    private float yaw;
    private float pitch = 15f;

    public float Pitch => pitch;
    public float Yaw => yaw;

    private Mouse mouse;

    private void Start()
    {
        mouse = Mouse.current;

        if (target == null)
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                target = player.transform;
                playerMovement = player;
            }
        }

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    private void LateUpdate()
    {
        if (target == null || mouse == null) return;

        HandleCameraInput();
        UpdateCameraPosition();
        UpdateCharacterFacing();
    }

    private void HandleCameraInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // Only rotate camera when RIGHT MOUSE BUTTON is held
        if (mouse.rightButton.isPressed)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();

            yaw += mouseDelta.x * horizontalSensitivity;
            pitch -= mouseDelta.y * verticalSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        Vector3 offset = rotation * new Vector3(0, 0, -distance);
        offset.y += height;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        transform.LookAt(target.position + Vector3.up * 1f);
    }

    private void UpdateCharacterFacing()
    {
        if (playerMovement == null) return;

        // Character faces camera direction (yaw only)
        Vector3 facingDirection = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        playerMovement.SetFacingDirection(facingDirection);
    }
}
