using Generals;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Displays an inventory item with its icon and count.
/// Drag-drop functionality is handled by ItemDragDropHandler component.
/// </summary>
public class InventoryItemUI : MonoBehaviour, IPointerClickHandler
{
    //References
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Vector2 iconSize = new Vector2(200f, 200f); // Adjust this size as needed

    //Class Reference
    private ItemSO currentItemData;

    //Properties
    public int CurrentSlotIndex { get; private set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Check for right-click
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log($"Right-clicked on item in slot {CurrentSlotIndex}");

            // Find the context menu in the scene
            var contextMenu = FindFirstObjectByType<InventoryItemContextMenu>();
            if (contextMenu != null)
                // Show menu at mouse position with item data
                contextMenu.ShowMenu(MouseInputUtility.GetRawMouse(), CurrentSlotIndex, this, currentItemData);
            else
                Debug.LogWarning("InventoryItemContextMenu not found in scene!");
        }
    }

    public void Setup(ItemSO itemType, int slot, int amount)
    {
        currentItemData = itemType;
        image.sprite = itemType.icon;
        CurrentSlotIndex = slot;

        // Resize the icon
        RectTransform rectTransform = image.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = iconSize;
        }

        UpdateAmount(amount);
    }

    public void UpdateAmount(int amount)
    {
        if (countText != null)
        {
            if (amount > 1)
            {
                countText.text = amount.ToString();
                countText.gameObject.SetActive(true);
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }
    }

    public void SetReferencedSlot(int slot)
    {
        CurrentSlotIndex = slot;
    }
}