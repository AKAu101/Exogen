using UnityEngine;

public class UILayouter : MonoBehaviour
{

    [SerializeField] private Transform InventoryTransform;
    [SerializeField] private GameObject InventoryDecorWrapper;
    [SerializeField] private float inventoryXOffsetPos;
    private float inventoryXoriginal;

    IUIStateManagement stateManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        //stateManager = ServiceLocator.Instance.Get<IUIStateManagement>();
        inventoryXoriginal = InventoryTransform.position.x;

    }
    private void OnEnable()
    {
        UIStateManager.EnsureInstance().OnInventoryVisibilityChanged += OnStateChanged;
    }
    private void OnDisable()
    {
        UIStateManager.EnsureInstance().OnInventoryVisibilityChanged -= OnStateChanged;
    }

    void OnStateChanged(bool b)
    {
        if (UIStateManager.EnsureInstance().visibleInventories > 1) //crafting must be open
        {
           // Vector3 pos = InventoryTransform.position;
           // pos.x = inventoryXOffsetPos;
           // InventoryTransform.position = pos;
           InventoryDecorWrapper.SetActive(false);
        }
        else
        {
            InventoryDecorWrapper.SetActive(true);
            // Vector3 pos = InventoryTransform.position;
            // pos.x = inventoryXoriginal;
            // InventoryTransform.position = pos;
        }
    }
}
