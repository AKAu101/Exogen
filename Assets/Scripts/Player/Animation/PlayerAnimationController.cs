using UnityEngine;

/// <summary>
///     Centralized controller for player hand animations.
///     Provides public API for other scripts to set hand items and trigger animations.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    // Animation parameter names
    private static readonly int PARAM_ITEM_LEFT = Animator.StringToHash("ItemLeft");
    private static readonly int PARAM_ITEM_RIGHT = Animator.StringToHash("ItemRight");
    private static readonly int PARAM_GRAB = Animator.StringToHash("Grab");

    // Hand item values
    public enum HandItem
    {
        Nothing = 0,
        Lantern = 1,
        Radar = 2,
        Other = 3
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        Debug.Log($"PlayerAnimationController: Initialized with Animator '{animator?.name}' on GameObject '{gameObject.name}'");
    }

    #region Public API

    /// <summary>
    /// Set the left hand item animation.
    /// </summary>
    public void SetLeftHandItem(HandItem item)
    {
        if (animator != null)
        {
            int value = (int)item;
            animator.SetInteger(PARAM_ITEM_LEFT, value);

            // Debug: verify the value was actually set
            int actualValue = animator.GetInteger(PARAM_ITEM_LEFT);
            Debug.Log($"PlayerAnimationController: SetLeftHandItem({item}) = {value}, Animator now has: {actualValue}");
        }
        else
        {
            Debug.LogWarning("PlayerAnimationController: SetLeftHandItem called but animator is null!");
        }
    }

    /// <summary>
    /// Set the right hand item animation.
    /// </summary>
    public void SetRightHandItem(HandItem item)
    {
        if (animator != null)
        {
            int value = (int)item;
            animator.SetInteger(PARAM_ITEM_RIGHT, value);

            // Debug: verify the value was actually set
            int actualValue = animator.GetInteger(PARAM_ITEM_RIGHT);
            Debug.Log($"PlayerAnimationController: SetRightHandItem({item}) = {value}, Animator now has: {actualValue}, Animator: {animator.name}");
        }
        else
        {
            Debug.LogWarning("PlayerAnimationController: SetRightHandItem called but animator is null!");
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
