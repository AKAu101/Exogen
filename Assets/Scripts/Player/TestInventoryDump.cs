using UnityEngine;

/// <summary>
///     Temporary test script to simulate player death and inventory dump.
///     Press K to dump inventory and spawn drop chest.
/// </summary>
public class TestInventoryDump : MonoBehaviour
{
    [SerializeField] private KeyCode dumpKey = KeyCode.K;

    private void Update()
    {
        if (Input.GetKeyDown(dumpKey))
        {
            if (InventorySystem.Instance == null)
            {
                Debug.LogError("InventorySystem.Instance is null!");
                return;
            }

            var playerInventory = InventorySystem.Instance.PlayerInventory;
            if (playerInventory == null)
            {
                Debug.LogError("PlayerInventory is null!");
                return;
            }

            int itemCount = playerInventory.SlotToStack?.Count ?? 0;
            Debug.Log($"Testing inventory dump... Inventory has {itemCount} item stacks");

            var chest = InventorySystem.Instance.SpawnDropChestAtPlayer(playerInventory, transform);

            if (chest != null)
            {
                Debug.Log($"Drop chest spawned successfully at {transform.position}");
            }
            else
            {
                Debug.Log("No chest spawned - check InventorySystem for inventoryDropChestPrefab assignment");
            }
        }
    }
}
