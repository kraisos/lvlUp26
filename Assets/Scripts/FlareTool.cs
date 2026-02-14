using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FlareTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Inventory inventory;
    [SerializeField] private GameObject flarePrefab;

    [Header("Placement")]
    [SerializeField] private float footOffset = 0.05f;
    [SerializeField] private bool usePlayerForwardRotation = false;

    [Header("Inventory")]
    [SerializeField] private string requiredItemId = "flare";
    [SerializeField] private int requiredItemAmount = 1;

    private Collider playerCollider;
    private CharacterController playerCharacterController;

    private void Awake()
    {
        if (playerTransform == null)
        {
            playerTransform = transform;
        }

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }

        playerCollider = playerTransform.GetComponent<Collider>();
        playerCharacterController = playerTransform.GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!IsPrimaryPressedThisFrame() || flarePrefab == null || playerTransform == null)
        {
            return;
        }

        if (!TryConsumeRequiredItem())
        {
            return;
        }

        var spawnPosition = GetPlayerFeetPosition();
        var spawnRotation = usePlayerForwardRotation ? Quaternion.LookRotation(playerTransform.forward, Vector3.up) : Quaternion.identity;

        Instantiate(flarePrefab, spawnPosition, spawnRotation);
    }

    private Vector3 GetPlayerFeetPosition()
    {
        var basePosition = playerTransform.position;

        if (playerCharacterController != null)
        {
            return basePosition + playerCharacterController.center + Vector3.down * (playerCharacterController.height * 0.5f - footOffset);
        }

        if (playerCollider != null)
        {
            return new Vector3(basePosition.x, playerCollider.bounds.min.y + footOffset, basePosition.z);
        }

        return basePosition + Vector3.up * footOffset;
    }

    private bool TryConsumeRequiredItem()
    {
        if (inventory == null)
        {
            return false;
        }

        return inventory.TryConsumeItem(requiredItemId, requiredItemAmount);
    }

    private static bool IsPrimaryPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}
