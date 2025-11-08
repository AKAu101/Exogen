using UnityEngine;

/// <summary>
///     Represents a single inventory slot in the UI.
///     Holds a slot index used for drag-drop and item placement.
/// </summary>
public class SlotView : MonoBehaviour
{
    [SerializeField] private int slot;
    [SerializeField] private InventoryUI UIHandler;
    [SerializeField] private bool allowInput = true;
    public int Slot => slot;
    public InventoryUI InventoryUI => UIHandler;

    public bool AllowInput => allowInput;
}