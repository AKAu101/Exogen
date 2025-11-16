using UnityEngine;

/// <summary>
///     Temporary test script to simulate player death and inventory dump.
///     Press K to dump inventory and spawn drop chest.
/// </summary>
public class TestInventoryDump : MonoBehaviour
{
    [SerializeField] private KeyCode dumpKey = KeyCode.K;
    [SerializeField] private InventoryData playerInventory;

    private IInventorySystem inventorySystem;

    private void Start()
    {
        inventorySystem = ServiceLocator.Instance.Get<IInventorySystem>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(dumpKey))
        {
            // Lazy initialization
            if (inventorySystem == null)
            {
                inventorySystem = ServiceLocator.Instance.Get<IInventorySystem>();
            }

            if (inventorySystem == null)
            {
                Debug.LogError("InventorySystem not found!");
                return;
            }

            Debug.Log("Testing inventory dump...");

            // Spawn the drop chest at player position
            var inventorySystemConcrete = inventorySystem as InventorySystem;
            if (inventorySystemConcrete != null)
            {
                var chest = inventorySystemConcrete.SpawnDropChestAtPlayer(playerInventory, transform);

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
}
