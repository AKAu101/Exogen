using System.Collections.Generic;

/// <summary>
///     Minimal interface representing an inventory data container.
///     Supports multiple inventory types (Player, Chest, etc.)
/// </summary>
public interface IInventoryData
{
    Dictionary<int, ItemStack> SlotToStack { get; }
}
