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
        // Initialize dictionary with configured slots
        handAnimations[leftHandSlot] = ItemData.HandItemAnimation.Nothing;
        handAnimations[rightHandSlot] = ItemData.HandItemAnimation.Nothing;
        
        Debug.Log($"HandAnimationManager: Initialized with left hand slot = {leftHandSlot}, right hand slot = {rightHandSlot}");
        
        // Set initial state
        UpdateAllHandAnimations();
    }
    
    public void UpdateHandAnimation(int slot, ItemData itemData)
    {
        if (itemData == null)
        {
            SetHandAnimation(slot, ItemData.HandItemAnimation.Nothing);
            return;
        }
        
        // Use the animation type from ItemData
        SetHandAnimation(slot, itemData.animationType);
    }
    
    private void SetHandAnimation(int slot, ItemData.HandItemAnimation animationType)
    {
        // If slot is not in dictionary, add it (for tracking)
        if (!handAnimations.ContainsKey(slot))
        {
            Debug.LogWarning($"HandAnimationManager: Adding unknown slot {slot} to tracking");
            handAnimations[slot] = animationType;
        }
        else
        {
            // Update state
            handAnimations[slot] = animationType;
        }
    
        // Only apply to animation controller for configured hand slots
        if (animationController != null)
        {
            if (slot == leftHandSlot)
            {
                animationController.SetLeftHandItem(ConvertToHandItem(animationType));
                Debug.Log($"HandAnimationManager: Left hand (slot {leftHandSlot}) -> {animationType}");
            }
            else if (slot == rightHandSlot)
            {
                animationController.SetRightHandItem(ConvertToHandItem(animationType));
                Debug.Log($"HandAnimationManager: Right hand (slot {rightHandSlot}) -> {animationType}");
            }
            else
            {
                // For debugging, you might want to know about other slots
                Debug.Log($"HandAnimationManager: Updated slot {slot} -> {animationType} (not a configured hand slot)");
            }
        }
    }
    
    private PlayerAnimationController.HandItem ConvertToHandItem(ItemData.HandItemAnimation animationType)
    {
        Debug.Log($"Converting {animationType} to PlayerAnimationController.HandItem...");
    
        // Ensure the enum values match exactly
        PlayerAnimationController.HandItem result = (PlayerAnimationController.HandItem)animationType;
    
        Debug.Log($"Converted {animationType} ({(int)animationType}) to {result} ({(int)result})");
    
        return result;
    }
    
    private void UpdateAllHandAnimations()
    {
        // Update both configured hand slots
        UpdateHandAnimationFromEquipment(leftHandSlot);
        UpdateHandAnimationFromEquipment(rightHandSlot);
    }
    
    private void UpdateHandAnimationFromEquipment(int slot)
    {
        // This would need to check what's actually equipped
        // You might need to get this from EquipmentManager or Inventory
        // For now, this is a placeholder
        // Debug.Log($"HandAnimationManager: Would update animation for slot {slot}");
    }
    
    // Public getters if needed by other scripts
    public int LeftHandSlot => leftHandSlot;
    public int RightHandSlot => rightHandSlot;
    
    // Helper method to check if a slot is a hand slot
    public bool IsHandSlot(int slot)
    {
        return slot == leftHandSlot || slot == rightHandSlot;
    }
    
    // Helper method to get the hand type for a slot
    public string GetHandTypeForSlot(int slot)
    {
        if (slot == leftHandSlot) return "Left Hand";
        if (slot == rightHandSlot) return "Right Hand";
        return "Not a hand slot";
    }
}