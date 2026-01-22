using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Information")]
    public string itemName;
    public string description;
    public Sprite icon;
    public GameObject itemPrefab;
    public int maxStack = 1;
    
    [Header("Display Settings")]
    public Vector2 iconSize = new Vector2(100, 100); // Add this line
    
    [Header("Item Type")]
    public ItemType itemType;
    public bool isConsumable = false;
    
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
    

}