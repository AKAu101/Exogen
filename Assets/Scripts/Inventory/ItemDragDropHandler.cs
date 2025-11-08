using Generals;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
///     Handles drag and drop behavior for inventory items.
///     Separates drag-drop concerns from visual representation (InventoryItemUI).
/// </summary>
public class ItemDragDropHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //References
    [SerializeField] private GameObject wrapper;
    [SerializeField] private LayerMask slotAreaLayer;
    [SerializeField] private LayerMask dropAreaLayer;

    //Properties
    private Transform dragStartParent;
    private Vector3 dragStartPosition;
    InventoryUI dragStartUI;

    private IInventoryManagement inventoryManagement;

    //Class References
    private InventoryItemUI itemView;

    private void Awake()
    {
        itemView = GetComponent<InventoryItemUI>();

        if (itemView == null)
            Debug.LogError("ItemDragDropHandler requires InventoryItemUI component on the same GameObject!");
    }

    private void Start()
    {
        // Get the inventory service from the ServiceLocator
        // Using Start() instead of Awake() to ensure singletons have registered themselves
        if (ServiceLocator.Instance.IsRegistered<IInventoryManagement>())
            inventoryManagement = ServiceLocator.Instance.Get<IInventoryManagement>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemView == null)
        {
            Debug.LogError("OnBeginDrag: itemView is null!");
            return;
        }

        if (Physics.Raycast(transform.position + Vector3.back, transform.forward, out var hit,100f, slotAreaLayer))
        {
            Debug.Log("Hit something, looking for InventoryUI");
            //dragStartUI = hit.transform.gameObject.GetComponentInParent<InventoryUI>();
            dragStartUI = hit.transform.gameObject.GetComponent<SlotView>().InventoryUI;
        }
        else
        {
            Debug.LogWarning("InventoryUI not found! using fallback");
            dragStartUI = wrapper.GetComponentInParent<InventoryUI>();
        }

        if (dragStartUI == null) { Debug.LogError("InventoryUI not found in parent"); }
        else { }

            wrapper.SetActive(true);
        dragStartPosition = transform.position;
        dragStartParent = transform.parent;

        // Move to InventoryUIManager root for free positioning
        transform.SetParent(dragStartUI.gameObject.transform);
        transform.position = MouseInputUtility.GetRawMouse();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = MouseInputUtility.GetRawMouse();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (itemView == null) return;

        // Lazy initialization if service wasn't available at Start()
        if (inventoryManagement == null && ServiceLocator.Instance.IsRegistered<IInventoryManagement>())
            inventoryManagement = ServiceLocator.Instance.Get<IInventoryManagement>();

        var origin = transform.position;
        var raycastDistance = 100f;

        if (Physics.Raycast(transform.position + Vector3.back, transform.forward, out var hit, raycastDistance, slotAreaLayer))
        {
            var slotView = hit.transform.gameObject.GetComponent<SlotView>();

            if(!slotView.AllowInput)
            {
                Debug.Log("This slot is output only! Give Feedback  to player");
                ReturnToOriginalPosition();
                return;

            }

            InventoryUI endInventory = slotView.InventoryUI;
            Debug.Log($"startInventory {dragStartUI}");
            Debug.Log($"endInventory {endInventory}");
            if (slotView != null)
            {
                var targetSlot = slotView.Slot;

                if (inventoryManagement != null &&
                    inventoryManagement.TryMoveItem(dragStartUI.AssignedInventory,itemView.CurrentSlotIndex,endInventory.AssignedInventory, targetSlot))
                    // Item moved successfully, let InventoryUIManager handle view updates
                    return;
            }
        }
        else if (Physics.Raycast(transform.position + Vector3.back, transform.forward, out var dropHit, raycastDistance, dropAreaLayer))
        {
            Debug.Log("DropArea hit");
            InventoryManager.Instance.DropItem(dragStartUI.AssignedInventory, itemView.CurrentSlotIndex);
            return;
        }

        // Failed to drop or no valid drop area, return to original position
        ReturnToOriginalPosition();
    }

    private void ReturnToOriginalPosition()
    {
        if (dragStartParent != null)
        {
            transform.SetParent(dragStartParent);
            transform.position = dragStartPosition;
        }
    }
}