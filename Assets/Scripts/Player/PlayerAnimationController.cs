 using UnityEngine;

/// <summary>
///     Centralized controller for all player animations.
///     Listens to events from other systems and updates the animator accordingly.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Item Configuration")]
    [SerializeField] private string lanternItemName = "Lantern";
    [SerializeField] private string radarItemName = "Radar";
    [SerializeField] private int leftHandSlot = 17;
    [SerializeField] private int rightHandSlot = 18;

    // Animation parameter names
    private static readonly int PARAM_ITEM_LEFT = Animator.StringToHash("Item_Links");
    private static readonly int PARAM_ITEM_RIGHT = Animator.StringToHash("Item_Rechts");
    private static readonly int PARAM_SPEED = Animator.StringToHash("Speed");
    private static readonly int PARAM_IS_MOVING = Animator.StringToHash("IsMoving");
    private static readonly int PARAM_ATTACK = Animator.StringToHash("Attack");
    private static readonly int PARAM_GRAB = Animator.StringToHash("Grab");

    // Hand item values
    public enum HandItem
    {
        Nothing = 0,
        Lantern = 1,
        Radar = 2,
        Other = 3
    }

    // Cached references
    private IInventorySystem inventorySystem;
    private IInventoryData playerInventory;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        // Get inventory system
        if (ServiceLocator.Instance.IsRegistered<IInventorySystem>())
        {
            inventorySystem = ServiceLocator.Instance.Get<IInventorySystem>();
            playerInventory = InventorySystem.Instance.PlayerInventory;

            // Subscribe to inventory events
            inventorySystem.OnItemAdded += OnInventoryChanged;
            inventorySystem.OnItemRemoved += OnInventoryChanged;
            inventorySystem.OnItemMoved += OnItemMoved;
            inventorySystem.OnItemSwapped += OnItemSwapped;

            DebugManager.Log("PlayerAnimationController: Subscribed to inventory events");
        }
        else
        {
            DebugManager.LogWarning("PlayerAnimationController: IInventorySystem not found!");
        }

        // Initialize hand animations
        UpdateHandAnimations();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (inventorySystem != null)
        {
            inventorySystem.OnItemAdded -= OnInventoryChanged;
            inventorySystem.OnItemRemoved -= OnInventoryChanged;
            inventorySystem.OnItemMoved -= OnItemMoved;
            inventorySystem.OnItemSwapped -= OnItemSwapped;
        }
    }

    #region Inventory Event Handlers

    private void OnInventoryChanged(IInventoryData inv, ItemData itemData, int slot)
    {
        if (inv != playerInventory) return;
        if (slot != leftHandSlot && slot != rightHandSlot) return;

        UpdateHandAnimations();
    }

    private void OnItemMoved(IInventoryData invOne, int sourceSlot, IInventoryData invTwo, int targetSlot)
    {
        bool involvesHands = IsHandSlot(invOne, sourceSlot) || IsHandSlot(invTwo, targetSlot);
        if (involvesHands)
        {
            UpdateHandAnimations();
        }
    }

    private void OnItemSwapped(IInventoryData invOne, int sourceSlot, IInventoryData invTwo, int targetSlot)
    {
        bool involvesHands = IsHandSlot(invOne, sourceSlot) || IsHandSlot(invTwo, targetSlot);
        if (involvesHands)
        {
            UpdateHandAnimations();
        }
    }

    private bool IsHandSlot(IInventoryData inv, int slot)
    {
        return inv == playerInventory && (slot == leftHandSlot || slot == rightHandSlot);
    }

    #endregion

    #region Hand Animations

    private void UpdateHandAnimations()
    {
        if (animator == null || playerInventory == null) return;

        HandItem leftHandItem = HandItem.Nothing;
        HandItem rightHandItem = HandItem.Nothing;

        // Check left hand
        if (playerInventory.SlotToStack.TryGetValue(leftHandSlot, out var leftStack))
        {
            leftHandItem = GetHandItemType(leftStack.ItemType.name);
        }

        // Check right hand
        if (playerInventory.SlotToStack.TryGetValue(rightHandSlot, out var rightStack))
        {
            rightHandItem = GetHandItemType(rightStack.ItemType.name);
        }

        // Update animator
        animator.SetInteger(PARAM_ITEM_LEFT, (int)leftHandItem);
        animator.SetInteger(PARAM_ITEM_RIGHT, (int)rightHandItem);

        DebugManager.Log($"PlayerAnimationController: Hands updated - Left: {leftHandItem}, Right: {rightHandItem}");
    }

    private HandItem GetHandItemType(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return HandItem.Nothing;

        if (itemName.Equals(lanternItemName, System.StringComparison.OrdinalIgnoreCase))
            return HandItem.Lantern;

        if (itemName.Equals(radarItemName, System.StringComparison.OrdinalIgnoreCase))
            return HandItem.Radar;

        return HandItem.Other;
    }

    #endregion

    #region Public API - For Other Scripts to Trigger Animations

    /// <summary>
    /// Set the left hand item animation directly.
    /// </summary>
    public void SetLeftHandItem(HandItem item)
    {
        if (animator != null)
        {
            animator.SetInteger(PARAM_ITEM_LEFT, (int)item);
        }
    }

    /// <summary>
    /// Set the right hand item animation directly.
    /// </summary>
    public void SetRightHandItem(HandItem item)
    {
        if (animator != null)
        {
            animator.SetInteger(PARAM_ITEM_RIGHT, (int)item);
        }
    }

    /// <summary>
    /// Trigger an animation by name.
    /// </summary>
    public void TriggerAnimation(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }

    /// <summary>
    /// Set a boolean animation parameter.
    /// </summary>
    public void SetBool(string paramName, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(paramName, value);
        }
    }

    /// <summary>
    /// Set a float animation parameter.
    /// </summary>
    public void SetFloat(string paramName, float value)
    {
        if (animator != null)
        {
            animator.SetFloat(paramName, value);
        }
    }

    /// <summary>
    /// Update movement animation based on whether the player is moving.
    /// Speed is an integer: 0 = idle, 1 = moving
    /// </summary>
    public void SetMoving(bool isMoving)
    {
        if (animator == null) return;

        animator.SetInteger(PARAM_SPEED, isMoving ? 1 : 0);
        animator.SetBool(PARAM_IS_MOVING, isMoving);
    }

    /// <summary>
    /// Trigger the attack animation.
    /// </summary>
    public void TriggerAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger(PARAM_ATTACK);
        }
    }

    /// <summary>
    /// Trigger the grab/pickup animation.
    /// </summary>
    public void TriggerGrab()
    {
        if (animator != null)
        {
            animator.SetTrigger(PARAM_GRAB);
        }
    }

    #endregion
}
