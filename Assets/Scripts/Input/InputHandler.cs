using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }

    [Header("Mouse Settings")]
    [SerializeField] private float mouseSensitivity = 1f;

    // Current frame's mouse delta (raw input)
    public Vector2 MouseDelta { get; private set; }

    // Accumulated input for weapon (both X and Y)
    public Vector2 WeaponInput { get; private set; }

    private Mouse mouse;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mouse = Mouse.current;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (mouse == null) return;

        Vector2 rawDelta = mouse.delta.ReadValue();
        MouseDelta = rawDelta * mouseSensitivity;

        // Only accumulate weapon input when NOT holding right mouse button
        // (right mouse is for camera control)
        if (!mouse.rightButton.isPressed)
        {
            WeaponInput += MouseDelta;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                ? CursorLockMode.None
                : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }
    }

    public void ConsumeWeaponInput()
    {
        WeaponInput = Vector2.zero;
    }
}
