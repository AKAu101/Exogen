using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     Displays an inventory item with its icon and count.
///     This is the visual representation that gets instantiated dynamically based on ItemData.
///     Items define HOW they look (icon, iconSize).
/// </summary>
public class ItemDisplay : MonoBehaviour
{
    [Header("Display Components")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;

    private ItemData currentItemData;

    /// <summary>
    ///     Sets up the display with item data and amount.
    ///     Reads visual properties from ItemData (icon, iconSize).
    /// </summary>
    public void Setup(ItemData itemData, int amount)
    {
        currentItemData = itemData;

        // Auto-find components if not assigned
        if (itemIcon == null)
        {
            itemIcon = GetComponent<Image>();
        }
        if (countText == null)
        {
            countText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Set icon from ItemData
        if (itemIcon != null && itemData != null && itemData.icon != null)
        {
            itemIcon.sprite = itemData.icon;
            itemIcon.enabled = true;

            // Resize icon based on ItemData's iconSize
            RectTransform iconRect = itemIcon.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.sizeDelta = itemData.iconSize;
            }
        }

        UpdateAmount(amount);
    }

    /// <summary>
    ///     Updates the displayed amount/count.
    /// </summary>
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

    /// <summary>
    ///     Gets the current item data being displayed.
    /// </summary>
    public ItemData GetItemData()
    {
        return currentItemData;
    }
}
