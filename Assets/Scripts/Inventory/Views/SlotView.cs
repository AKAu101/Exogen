using UnityEngine;

/// <summary>
///     Represents a single inventory slot in the UI.
///     Holds a slot index used for drag-drop and item placement.
/// </summary>
public class SlotView : MonoBehaviour
{
    [SerializeField] private int slot;
    public int Slot => slot;
}