using UnityEngine;

/// <summary>
/// Interactable lumini that recharges the player's lantern when picked up.
/// Destroys itself after being picked up.
/// </summary>
public class LuminiPickup : MonoBehaviour
{
    [Header("Lumini Settings")]
    [SerializeField] private float rechargeTime = 30f;
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Optional Effects")]
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private AudioClip pickupSound;

    [Header("Requirements")]
    [SerializeField] private string noLanternMessage = "You need a lantern to collect Lumini";
    [SerializeField] private string fullLanternMessage = "Lantern is fully charged";

    private Interactable interactable;
    private AudioSource audioSource;
    private LanternController lanternController;
    private string originalMessage;
    private bool isBeingPickedUp;

    // Getter for external systems (like audio)
    public bool IsBeingPickedUp => isBeingPickedUp;

    private void Awake()
    {
        // Get or add Interactable component
        interactable = GetComponent<Interactable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<Interactable>();
            interactable.message = "Pick up Lumini";
        }

        // Store the original message
        originalMessage = interactable.message;

        // Subscribe to the interaction event
        interactable.OnInteract.AddListener(OnLuminiPickedUp);

        // Setup audio source if needed
        if (pickupSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.clip = pickupSound;
        }
    }

    private void Start()
    {
        // Find the lantern controller
        FindLanternController();
    }

    private void Update()
    {
        // Update lantern controller reference if needed
        if (lanternController == null)
        {
            FindLanternController();
        }

        // Check if player has lantern equipped AND can recharge
        if (lanternController != null && interactable != null)
        {
            bool hasLantern = lanternController.HasLanternEquipped;
            bool canRecharge = lanternController.CanRecharge();

            // Enable/disable interactable based on lantern status
            interactable.enabled = hasLantern && canRecharge;

            // Update message based on lantern status
            if (!hasLantern)
            {
                interactable.message = noLanternMessage;
            }
            else if (!canRecharge)
            {
                interactable.message = fullLanternMessage;
            }
            else
            {
                interactable.message = originalMessage;
            }
        }
    }

    private void FindLanternController()
    {
        lanternController = FindFirstObjectByType<LanternController>();
        if (lanternController == null)
        {
            DebugManager.LogWarning("LuminiPickup: Could not find LanternController in scene!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from event
        if (interactable != null)
        {
            interactable.OnInteract.RemoveListener(OnLuminiPickedUp);
        }
    }

    /// <summary>
    /// Called when the lumini is picked up by the player.
    /// </summary>
    /// <param name="interactor">The GameObject that interacted (player)</param>
    private void OnLuminiPickedUp(GameObject interactor)
    {
        isBeingPickedUp = true;
        DebugManager.Log($"Lumini picked up by {interactor.name}, recharging lantern for {rechargeTime} seconds");

        // Trigger grab animation
        var animController = interactor.GetComponent<PlayerAnimationController>();
        if (animController != null)
        {
            animController.TriggerGrab();
        }

        // Recharge the lantern (lanternController should already be found)
        if (lanternController != null && lanternController.CanRecharge())
        {
            lanternController.RechargeLantern(rechargeTime);
        }
        else
        {
            DebugManager.LogWarning("LuminiPickup: Cannot recharge - lantern not equipped or already full!");
            isBeingPickedUp = false;
            return;
        }

        // Play pickup sound
        if (pickupSound != null && audioSource != null)
        {
            audioSource.Play();
        }

        // Spawn pickup effect
        if (pickupEffectPrefab != null)
        {
            Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
        }

        // Destroy or disable the lumini
        if (destroyOnPickup)
        {
            // If we have a sound, wait for it to finish before destroying
            if (pickupSound != null && audioSource != null)
            {
                Destroy(gameObject, pickupSound.length);

                // Disable visuals immediately
                var meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null) meshRenderer.enabled = false;

                var collider = GetComponent<Collider>();
                if (collider != null) collider.enabled = false;

                // Disable interactable
                interactable.enabled = false;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}