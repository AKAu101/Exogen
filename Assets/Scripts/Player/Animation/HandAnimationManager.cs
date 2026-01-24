using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages hand animations based on equipped items
/// Coordinates between EquipmentManager and PlayerAnimationController
/// </summary>
public class HandAnimationManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private PlayerAnimationController animationController;

    [Header("Hand Slot Configuration")]
    [SerializeField] private int leftHandSlot = 16;
    [SerializeField] private int rightHandSlot = 17;

    // Current state tracking
    private Dictionary<int, ItemData.HandItemAnimation> handAnimations = new Dictionary<int, ItemData.HandItemAnimation>();

    private void Start()
    {
        if (equipmentManager == null)
        {
            equipmentManager = FindObjectOfType<EquipmentManager>();
        }

        if (animationController == null)
        {
            animationController = GetComponentInChildren<PlayerAnimationController>();
        }

        Initialize();
    }

    private void Initialize()
    {
        handAnimations[leftHandSlot] = ItemData.HandItemAnimation.Nothing;
        handAnimations[rightHandSlot] = ItemData.HandItemAnimation.Nothing;

        UpdateAllHandAnimations();
    }

    public void UpdateHandAnimation(int slot, ItemData itemData)
    {
        if (itemData == null)
        {
            SetHandAnimation(slot, ItemData.HandItemAnimation.Nothing);
            return;
        }

        SetHandAnimation(slot, itemData.animationType);
    }

    private void SetHandAnimation(int slot, ItemData.HandItemAnimation animationType)
    {
        if (!handAnimations.ContainsKey(slot))
        {
            handAnimations[slot] = animationType;
        }
        else
        {
            handAnimations[slot] = animationType;
        }

        if (animationController != null)
        {
            if (slot == leftHandSlot)
            {
                animationController.SetLeftHandItem(ConvertToHandItem(animationType));
            }
            else if (slot == rightHandSlot)
            {
                animationController.SetRightHandItem(ConvertToHandItem(animationType));
            }
        }
    }

    private PlayerAnimationController.HandItem ConvertToHandItem(ItemData.HandItemAnimation animationType)
    {
        return (PlayerAnimationController.HandItem)animationType;
    }

    private void UpdateAllHandAnimations()
    {
        UpdateHandAnimationFromEquipment(leftHandSlot);
        UpdateHandAnimationFromEquipment(rightHandSlot);
    }

    private void UpdateHandAnimationFromEquipment(int slot)
    {
        // Placeholder - could check EquipmentManager or Inventory for current items
    }

    public int LeftHandSlot => leftHandSlot;
    public int RightHandSlot => rightHandSlot;

    public bool IsHandSlot(int slot)
    {
        return slot == leftHandSlot || slot == rightHandSlot;
    }

    public string GetHandTypeForSlot(int slot)
    {
        if (slot == leftHandSlot) return "Left Hand";
        if (slot == rightHandSlot) return "Right Hand";
        return "Not a hand slot";
    }
}
