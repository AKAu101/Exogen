using UnityEngine;

/// <summary>
///     Scriptable Object defining an item's properties including icon, name, description, and prefab.
///     Used for both inventory items and world item pickups.
/// </summary>
[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
public class ItemSO : ScriptableObject
{
    public Sprite icon;
    public new string name;
    public string description;
    public int amount;
    public bool isConsumable;
    public GameObject itemPrefab;
}