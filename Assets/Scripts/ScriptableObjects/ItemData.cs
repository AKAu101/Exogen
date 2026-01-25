using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Information")]
    public string itemName;
    public string description;
    public Sprite icon;
    public GameObject itemPrefab;
    public GameObject chargedPrefab; // Optional: alternate prefab when item is "charged" (e.g., glowing lantern)
    public int maxStack = 1;
    
    [Header("Display Settings")]
    public Vector2 iconSize = new Vector2(100, 100);
    
    [Header("Item Type & Category")]
    public ItemType itemType;
    public ItemCategory itemCategory; // NEW: More specific category
    public bool isConsumable = false;
    
    [Header("Animation Settings")]
    public HandItemAnimation animationType = HandItemAnimation.Nothing; // NEW
    
    [Header("Food Properties (if consumable)")]
    public float healthRestore = 0f;
    public float oxygenRestore = 0f;
    public float staminaRestore = 0f;
    public float consumeTime = 1f;
    
    public enum ItemType
    {
        Resource,
        Consumable,
        Equipment,
        Quest
    }
    
    // NEW: Specific categories for better organization
    public enum ItemCategory
    {
        General,
        Lantern,
        Scanner,
        Tool,
        Weapon,
        Food
    }
    
    // NEW: Animation types that match your Animator parameters
    public enum HandItemAnimation
    {
        Nothing = 0,
        Lantern = 1,
        Radar = 2,    // Scanner
        Other = 3
    }
}