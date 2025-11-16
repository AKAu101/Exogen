using Generals;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEditor.UIElements;

/// <summary>
///     Handles drag and drop behavior for inventory items.
///     Creates a visual drag icon, keeps slots stationary.
/// </summary>
public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //References
    [SerializeField] public LayerMask slotAreaLayer;
    [SerializeField] public LayerMask dropAreaLayer;

    //Properties
    private GameObject dragIcon;
    private InventoryUI dragStartUI;
    private int dragStartSlot;

    private IInventorySystem inventoryManagement;
    private GraphicRaycaster graphicRaycaster;
    private EventSystem eventSystem;

    //Class References
    private ItemSlot itemView;

    private void Awake()
    {
        itemView = GetComponent<ItemSlot>();

        if (itemView == null)
            Debug.LogError("ItemDragHandler requires ItemSlot component on the same GameObject!");

        slotAreaLayer = InventorySystem.Instance.slotAreaLayer;
        dropAreaLayer = InventorySystem.Instance.dropAreaLayer;
    }

    private void Start()
    {
        // Get the inventory service from the ServiceLocator
        // Using Start() instead of Awake() to ensure singletons have registered themselves
        if (ServiceLocator.Instance.IsRegistered<IInventorySystem>())
            inventoryManagement = ServiceLocator.Instance.Get<IInventorySystem>();

        // Get UI raycasting components
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        }
        eventSystem = EventSystem.current;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemView == null || !itemView.HasItem)
        {
            return;
        }

        // Lazy initialization if service wasn't available at Start()
        if (inventoryManagement == null && ServiceLocator.Instance.IsRegistered<IInventorySystem>())
            inventoryManagement = ServiceLocator.Instance.Get<IInventorySystem>();

        // Store starting info
        dragStartUI = itemView.InventoryUI;
        dragStartSlot = itemView.CurrentSlotIndex;

        // Hide the original icon
        itemView.HideIcon();

        // Create drag icon
        CreateDragIcon();
    }

    private void CreateDragIcon()
    {
        // Create a temporary GameObject for the drag icon
        dragIcon = new GameObject("DragIcon");

        // Get the canvas to parent the drag icon to
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            dragIcon.transform.SetParent(canvas.transform, false);
        }

        // Add Image component and copy the item icon
        RectTransform dragRect = dragIcon.AddComponent<RectTransform>();
        dragRect.sizeDelta = new Vector2(50, 50);

        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = itemView.CurrentItem.icon;
        dragImage.raycastTarget = false; // Important: don't block raycasts

        // Add CanvasGroup to make it ignore raycasts completely
        CanvasGroup canvasGroup = dragIcon.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        // Set semi-transparent
        Color color = dragImage.color;
        color.a = 0.6f;
        dragImage.color = color;

        // Position at mouse
        dragIcon.transform.position = MouseInputUtility.GetRawMouse();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.transform.position = MouseInputUtility.GetRawMouse();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Destroy the drag icon
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        if (itemView == null || !itemView.HasItem)
        {
            return;
        }

        bool validDrop = false;

        // Find which slot we're hovering over using UI raycasting
        ItemSlot targetSlot = GetSlotUnderMouse(eventData);

        if (targetSlot != null)
        {
            Debug.Log($"Dropped on slot: {targetSlot.SlotIndex}");
            Debug.Log($"Target slot InventoryUI: {targetSlot.InventoryUI}");
            Debug.Log($"Drag start InventoryUI: {dragStartUI}");

            if (targetSlot.AllowInput && inventoryManagement != null)
            {
                InventoryUI endInventory = targetSlot.InventoryUI;

                if (endInventory == null)
                {
                    Debug.LogError("Target slot has null InventoryUI!");
                    return;
                }

                // If dropping on the same slot, just show the icon again
                if (endInventory == dragStartUI && targetSlot.SlotIndex == dragStartSlot)
                {
                    Debug.Log("Dropped on same slot, showing icon again");
                    itemView.ShowIcon();
                    return;
                }

                Debug.Log($"Calling TryMoveItem: from {dragStartUI.AssignedInventory} slot {dragStartSlot} to {endInventory.AssignedInventory} slot {targetSlot.SlotIndex}");

                bool success = inventoryManagement.TryMoveItem(
                    dragStartUI.AssignedInventory, dragStartSlot,
                    endInventory.AssignedInventory, targetSlot.SlotIndex);

                validDrop = success;
                Debug.Log($"Move success: {success}");
            }
            else
            {
                Debug.Log($"Slot doesn't allow input: {!targetSlot.AllowInput}, inventory management is null: {inventoryManagement == null}");
            }
        }
        else
        {
            Debug.Log("No slot found under mouse, checking if should drop item");
            ///old with Physics raycasting and not UI
            //// Check if dropped in drop area (3D physics for world drop)
            //var raycastDistance = 100f;
            //Vector3 rayStart = MouseInputUtility.GetRawMouse() + Vector3.back;
            //
            //if (Physics.Raycast(rayStart, Vector3.forward, out var dropHit, raycastDistance, dropAreaLayer))
            //{
            //    Debug.Log("DropArea hit");
            //    if (inventoryManagement != null)
            //    {
            //        inventoryManagement.DropItem(dragStartUI.AssignedInventory, dragStartSlot);
            //        validDrop = true;
            //    }
            //}
            if(ShouldDrop(eventData))
            {
                Debug.Log("Trying to drop item");
                InventorySystem.Instance.DropItem(dragStartUI.AssignedInventory,dragStartSlot);
                validDrop = true; // Mark as valid so icon doesn't reappear
            }


        }

        // If drop was invalid, show the icon again in the original slot
        if (!validDrop)
        {
            Debug.Log("Invalid drop, showing icon again");
            itemView.ShowIcon();
        }
    }

    private ItemSlot GetSlotUnderMouse(PointerEventData eventData)
    {
        if (graphicRaycaster == null || eventSystem == null)
        {
            Debug.LogWarning("GraphicRaycaster or EventSystem not found!");
            return null;
        }

        // Set up the raycast data
        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = Input.mousePosition;

        // Raycast to find UI elements under mouse
        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerData, results);

        Debug.Log($"UI Raycast found {results.Count} objects under mouse");

        //custom layer check
        foreach (RaycastResult result in results)
        {
            Debug.Log($"slotAreaLayer is {slotAreaLayer}");
            if (LayerHelpers.IsInLayerMask(result.gameObject,slotAreaLayer))
            {
                Debug.Log($"{result.gameObject.name} has slotAreaLayer");

                ItemSlot slot = result.gameObject.GetComponent<ItemSlot>();
                if (slot != null)
                {
                    Debug.Log($"    Found ItemSlot on object itself: {slot.SlotIndex}");
                    return slot;
                }
            }
            else Debug.Log($"{result.gameObject.name} doesnt have slotAreaLayer its Layer is {result.gameObject.layer}");

        }

        Debug.Log("  No ItemSlot found in any raycast results");
        return null;
    }


    bool ShouldDrop(PointerEventData eventData)
    {
        if (graphicRaycaster == null || eventSystem == null)
        {
            Debug.LogWarning("GraphicRaycaster or EventSystem not found!");
            return false;
        }

        // Set up the raycast data
        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = Input.mousePosition;

        // Raycast to find UI elements under mouse
        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if(LayerHelpers.IsInLayerMask(result.gameObject, dropAreaLayer))
            {
                return true;
            }
        }
        return false;
    }
}