using UnityEngine;
using UnityEditor;

public class GameSetup : EditorWindow
{
    [MenuItem("Physics Fighter/Setup Test Scene")]
    public static void SetupTestScene()
    {
        // Create Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10, 1, 10); // 100x100 units
        ground.layer = LayerMask.NameToLayer("Default"); // Will need Ground layer

        // Create Ground layer if it doesn't exist and assign
        SetupGroundLayer(ground);

        // Create Player
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0, 1.5f, 0);

        Rigidbody playerRb = player.AddComponent<Rigidbody>();
        playerRb.mass = 70f; // Average human mass
        playerRb.freezeRotation = true;
        playerRb.interpolation = RigidbodyInterpolation.Interpolate;

        PlayerMovement playerMovement = player.AddComponent<PlayerMovement>();

        // Add hands component (will auto-create visual hands)
        PlayerHands playerHands = player.AddComponent<PlayerHands>();

        // Create Weapon (elongated cube = sword shape)
        GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        weapon.name = "Weapon";
        weapon.transform.localScale = new Vector3(0.1f, 0.1f, 1.5f); // Thin, long sword shape

        // Position in front of player at hand height
        weapon.transform.position = player.transform.position +
                                    player.transform.forward * 1f +
                                    player.transform.up * 0.3f;

        // Orient sword pointing forward
        weapon.transform.rotation = player.transform.rotation;

        Rigidbody weaponRb = weapon.AddComponent<Rigidbody>();
        weaponRb.mass = 5f;
        weaponRb.useGravity = false;
        weaponRb.angularDamping = 2f;
        weaponRb.linearDamping = 1f;

        WeaponPhysics weaponPhysics = weapon.AddComponent<WeaponPhysics>();
        // Link weapon to player and camera
        SerializedObject weaponSO = new SerializedObject(weaponPhysics);
        weaponSO.FindProperty("playerMovement").objectReferenceValue = playerMovement;
        if (Camera.main != null)
        {
            weaponSO.FindProperty("cameraTransform").objectReferenceValue = Camera.main.transform;
        }
        weaponSO.ApplyModifiedProperties();

        // Create Input Handler (empty GameObject)
        GameObject inputHandler = new GameObject("InputHandler");
        inputHandler.AddComponent<InputHandler>();

        // Setup Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 5, -8);
            mainCam.transform.LookAt(player.transform);

            ThirdPersonCamera camScript = mainCam.gameObject.AddComponent<ThirdPersonCamera>();
            SerializedObject camSO = new SerializedObject(camScript);
            camSO.FindProperty("target").objectReferenceValue = player.transform;
            camSO.FindProperty("playerMovement").objectReferenceValue = playerMovement;
            camSO.ApplyModifiedProperties();
        }

        // Select player for easy inspection
        Selection.activeGameObject = player;

        Debug.Log("Test scene setup complete! Press Play to test.");
        Debug.Log("Controls: Move mouse to swing weapon. Right-click + drag to orbit camera. ESC to unlock cursor.");
    }

    private static void SetupGroundLayer(GameObject ground)
    {
        // Try to find or create Ground layer
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        int groundLayerIndex = -1;

        // Find existing Ground layer or first empty slot (after built-in layers)
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (layer.stringValue == "Ground")
            {
                groundLayerIndex = i;
                break;
            }
            if (groundLayerIndex == -1 && string.IsNullOrEmpty(layer.stringValue))
            {
                groundLayerIndex = i;
                layer.stringValue = "Ground";
                tagManager.ApplyModifiedProperties();
                Debug.Log("Created 'Ground' layer at index " + i);
                break;
            }
        }

        if (groundLayerIndex != -1)
        {
            ground.layer = groundLayerIndex;
        }
    }

    [MenuItem("Physics Fighter/Reset Player Position")]
    public static void ResetPlayerPosition()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            player.transform.position = new Vector3(0, 1.5f, 0);
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
