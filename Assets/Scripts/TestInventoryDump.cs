using UnityEngine;

/// <summary>
///     Temporary test script to simulate player death and inventory dump.
///     Press K to dump inventory and spawn drop chest.
/// </summary>
public class TestInventoryDump : MonoBehaviour
{
    [SerializeField] private KeyCode dumpKey = KeyCode.K;
    [SerializeField] private Inventory playerInventory;

    private void Update()
    {
        if (Input.GetKeyDown(dumpKey))
        {
            Debug.Log("Testing inventory dump...");

            // Spawn the drop chest at player position
            var chest = InventoryManager.Instance.SpawnDropChestAtPlayer(playerInventory,transform);

            if (chest != null)
            {
                Debug.Log($"Drop chest spawned successfully at {transform.position}");
            }
            else
            {
                Debug.Log("No chest spawned (inventory might be empty or prefab not assigned)");
            }
        }
    }
}
