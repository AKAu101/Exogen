using Generals;
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
    [SerializeField] private LayerMask dropAreaLayer;

    //Properties
    private Transform dragStartParent;
    private Vector3 dragStartPosition;
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

        wrapper.SetActive(true);
        dragStartPosition = transform.position;
        dragStartParent = transform.parent;

        // Move to InventoryUIManager root for free positioning
        transform.SetParent(InventoryUIManager.Instance.gameObject.transform);
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

        if (Physics.Raycast(transform.position, transform.forward, out var hit, raycastDistance, dropAreaLayer))
        {
            var slotView = hit.transform.gameObject.GetComponent<SlotView>();
            if (slotView != null)
            {
                var targetSlot = slotView.Slot;

                if (inventoryManagement != null &&
                    inventoryManagement.TryMoveItem(itemView.CurrentSlotIndex, targetSlot))
                    // Item moved successfully, let InventoryUIManager handle view updates
                    return;
            }
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