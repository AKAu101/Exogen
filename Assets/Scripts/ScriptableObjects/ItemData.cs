using UnityEngine;

/// <summary>
///     Scriptable Object defining an item's properties including icon, name, description, and prefab.
///     Used for both inventory items and world item pickups.
/// </summary>
[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public new string name;
    public Sprite icon;
    public Vector2 iconSize = new Vector2(200f, 200f);
    public string description;
    public int amount;
    public bool isConsumable;
    public GameObject itemPrefab; //world item that spawns when dropping
}