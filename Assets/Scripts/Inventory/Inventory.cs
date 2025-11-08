using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour, IInventory
{
    Dictionary<int, ItemStack> slotToStack = new Dictionary<int, ItemStack>();

    //IInventory interface
    Dictionary<int, ItemStack> IInventory.SlotToStack => slotToStack;
}
