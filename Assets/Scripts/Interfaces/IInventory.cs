using System.Collections.Generic;
using UnityEngine;

public interface IInventory
{
    Dictionary<int, ItemStack> SlotToStack { get; }

}
