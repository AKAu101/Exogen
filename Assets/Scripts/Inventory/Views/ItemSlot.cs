using Generals;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
///     Represents a single inventory slot that displays item icons directly.
/// </summary>
public class ItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Configuration")]
    [SerializeField] private int slotIndex;
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private bool allowInput = true;

    [Header("Slot Visuals (Optional Hover)")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Sprite defaultSlotSprite;
    [SerializeField] private Sprite hoverSlotSprite;

    // Runtime references
    private Image itemIcon;
    private TextMeshProUGUI itemCountText;
    private ItemData currentItem;
    private int currentAmount;

    // Properties
    public int SlotIndex => slotIndex;
    public InventoryUI InventoryUI => inventoryUI;
    public bool AllowInput => allowInput;
    public bool HasItem => currentItem != null;
    public ItemData CurrentItem => currentItem;
    public int CurrentAmount => currentAmount;
    public int CurrentSlotIndex { get; private set; }

    private void Awake()
    {
        CurrentSlotIndex = slotIndex;

        // Only setup display if not initialized via Initialize() method
        if (itemIcon == null)
        {
            SetupIconDisplay();
        }
    }

    /// <summary>
    ///     Initializes the slot with index and UI reference (called when added at runtime).
    /// </summary>
    public void Initialize(int index, InventoryUI ui)
    {
        slotIndex = index;
        CurrentSlotIndex = index;
        inventoryUI = ui;
        SetupIconDisplay();
    }

    private void SetupIconDisplay()
    {
        CreateIconImage();
        CreateCountText();
    }

    private void CreateIconImage()
    {
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = Vector2.zero;
        iconRect.anchoredPosition = Vector2.zero;

        itemIcon = iconObj.AddComponent<Image>();
        itemIcon.raycastTarget = false;
        itemIcon.enabled = false;
    }

    private void CreateCountText()
    {
        GameObject countObj = new GameObject("ItemCount");
        countObj.transform.SetParent(transform, false);

        RectTransform countRect = countObj.AddComponent<RectTransform>();
        countRect.anchorMin = new Vector2(1, 0);
        countRect.anchorMax = new Vector2(1, 0);
        countRect.pivot = new Vector2(1, 0);
        countRect.anchoredPosition = new Vector2(-5, 5);
        countRect.sizeDelta = new Vector2(50, 30);

        itemCountText = countObj.AddComponent<TextMeshProUGUI>();
        itemCountText.alignment = TextAlignmentOptions.BottomRight;
        itemCountText.fontSize = 60;
        itemCountText.color = Color.white;
        itemCountText.raycastTarget = false;
        itemCountText.enabled = false;
    }

    /// <summary>
    ///     Updates the slot index reference (used when items move between slots).
    /// </summary>
    public void SetReferencedSlot(int slot)
    {
        CurrentSlotIndex = slot;
        slotIndex = slot;
    }

    /// <summary>
    ///     Sets up the slot with item data, slot index, and amount.
    /// </summary>
    public void Setup(ItemData itemData, int slot, int amount)
    {
        SetReferencedSlot(slot);
        currentItem = itemData;
        currentAmount = amount;

        if (itemData != null && itemData.icon != null)
        {
            itemIcon.sprite = itemData.icon;
            itemIcon.enabled = true;
            UpdateAmount(amount);
        }
    }

    /// <summary>
    ///     Updates the amount displayed.
    /// </summary>
    public void UpdateAmount(int amount)
    {
        currentAmount = amount;

        if (amount > 1)
        {
            itemCountText.text = amount.ToString();
            itemCountText.enabled = true;
        }
        else
        {
            itemCountText.enabled = false;
        }
    }

    /// <summary>
    ///     Clears the item display when the slot is emptied.
    /// </summary>
    public void ClearDisplay()
    {
        itemIcon.sprite = null;
        itemIcon.enabled = false;
        itemCountText.enabled = false;
        currentItem = null;
        currentAmount = 0;
    }

    /// <summary>
    ///     Hides the icon temporarily (used during drag).
    /// </summary>
    public void HideIcon()
    {
        if (itemIcon != null)
        {
            itemIcon.enabled = false;
        }
        if (itemCountText != null)
        {
            itemCountText.enabled = false;
        }
    }

    /// <summary>
    ///     Shows the icon again (used after drag cancelled).
    /// </summary>
    public void ShowIcon()
    {
        if (itemIcon != null && currentItem != null)
        {
            itemIcon.enabled = true;
        }
        if (itemCountText != null && currentAmount > 1)
        {
            itemCountText.enabled = true;
        }
    }

    /// <summary>
    ///     Handles pointer click events - right-click shows context menu.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasItem) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log($"Right-clicked on item in slot {CurrentSlotIndex}");

            var contextMenu = FindFirstObjectByType<ItemContextMenu>();
            if (contextMenu != null)
            {
                contextMenu.ShowMenu(MouseInputUtility.GetRawMouse(), CurrentSlotIndex, this, CurrentItem);
            }
            else
            {
                Debug.LogWarning("ItemContextMenu not found in scene!");
            }
        }
    }

    /// <summary>
    ///     Hover enter - change slot background sprite if configured.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotBackground != null && hoverSlotSprite != null)
        {
            slotBackground.sprite = hoverSlotSprite;
        }
    }

    /// <summary>
    ///     Hover exit - restore default slot background sprite.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (slotBackground != null && defaultSlotSprite != null)
        {
            slotBackground.sprite = defaultSlotSprite;
        }
    }
}
