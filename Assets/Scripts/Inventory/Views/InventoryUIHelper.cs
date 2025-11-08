using System.Collections.Generic;
using UnityEngine;

public static class InventoryUIHelper
{
    public static Dictionary<IInventory, InventoryUI> inventoryToUI = new Dictionary<IInventory, InventoryUI>();

    public static bool SwapViewDictEntries(InventoryUI invOne, int sourceSlot, InventoryUI invTwo, int targetSlot)
    {

        if (!invOne.slotToView.ContainsKey(sourceSlot) || !invTwo.slotToView.ContainsKey(targetSlot))
        {
            Debug.LogError(
                $"SwapViewDictEntries: Source slot {sourceSlot} or target slot {targetSlot} not found in slotToView dictionary!");
            return false;
        }

        invOne.slotToView[sourceSlot].SetReferencedSlot(targetSlot);
        invTwo.slotToView[targetSlot].SetReferencedSlot(sourceSlot);

        return invOne.slotToView.SwapEntries(sourceSlot, targetSlot, invTwo.slotToView);

        //return slotToView.SwapEntries(sourceSlot, targetSlot);
    }

    public static void MoveViewDict(InventoryUI sourceInvUI, int sourceSlot, InventoryUI targetInvUI,int targetSlot)
    {
        //if (inv != assignedInventory)
        //{
        //    Debug.Log("Not my Inventory!");
        //    return;
        //}

        //if (!sourceInvUI.slotToView.ContainsKey(sourceSlot))
        //{
        //    Debug.LogError($"MoveViewDict: Source slot {sourceSlot} not found in slotToView dictionary!");
        //    return;
        //}

        //reparenting is missing probably
        Debug.Log($"Moving {sourceSlot} from {sourceInvUI} to {targetInvUI} at {targetSlot}");
        Debug.Log($"Source Slotview Ref: {sourceInvUI.slotToView[sourceSlot].CurrentSlotIndex}");

        targetInvUI.slotToView[targetSlot] = sourceInvUI.slotToView[sourceSlot];
        sourceInvUI.slotToView.Remove(sourceSlot);
        targetInvUI.slotToView[targetSlot].SetReferencedSlot(targetSlot);
        Debug.Log($"target Slotview Ref: {targetInvUI.slotToView[targetSlot].CurrentSlotIndex}");
        //targetInvUI.slotToView[targetSlot].transform.SetParent(targetInvUI.Gets);
        var view = targetInvUI.slotToView[targetSlot];
        view.transform.SetParent(targetInvUI.SlotIndexToContainer[view.CurrentSlotIndex].transform);

        sourceInvUI.UpdateView();
        targetInvUI.UpdateView();


    }

    public static bool Register(IInventory inv,InventoryUI ui)
    {
        Debug.Log($"Registered{inv} - {ui}");
        inventoryToUI.Add(inv, ui);
        return true;
    }

    public static InventoryUI GetUI(IInventory inv)
    {
        return inventoryToUI[inv];
    }
}
