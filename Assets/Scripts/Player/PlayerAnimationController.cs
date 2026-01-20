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
    private static readonly int PARAM_ITEM_LEFT = Animator.StringToHash("Item_Links");
    private static readonly int PARAM_ITEM_RIGHT = Animator.StringToHash("Item_Rechts");
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

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    #region Public API

    /// <summary>
    /// Set the left hand item animation.
    /// </summary>
    public void SetLeftHandItem(HandItem item)
    {
        if (animator != null)
        {
            animator.SetInteger(PARAM_ITEM_LEFT, (int)item);
        }
    }

    /// <summary>
    /// Set the right hand item animation.
    /// </summary>
    public void SetRightHandItem(HandItem item)
    {
        if (animator != null)
        {
            animator.SetInteger(PARAM_ITEM_RIGHT, (int)item);
        }
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
