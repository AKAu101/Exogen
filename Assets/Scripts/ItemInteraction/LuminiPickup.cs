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
        lanternController = FindFirstObjectByType<LanternController>();
        if (lanternController == null)
        {
            DebugManager.LogWarning("LuminiPickup: Could not find LanternController in scene!");
        }
    }

    private void Update()
    {
        // Check if player has lantern equipped
        if (lanternController != null && interactable != null)
        {
            bool hasLanternEquipped = HasLanternEquipped();

            // Enable/disable interactable based on lantern status
            interactable.enabled = hasLanternEquipped;

            // Update message based on lantern status
            if (hasLanternEquipped)
            {
                interactable.message = originalMessage;
            }
            else
            {
                interactable.message = noLanternMessage;
            }
        }
    }

    /// <summary>
    /// Checks if the player has a lantern equipped in either hand slot.
    /// </summary>
    private bool HasLanternEquipped()
    {
        if (lanternController == null) return false;

        // Access the concrete InventorySystem to get PlayerInventory
        var inventorySystem = InventorySystem.Instance;
        if (inventorySystem != null)
        {
            var playerInventory = inventorySystem.PlayerInventory;

            if (playerInventory != null)
            {
                // Check left hand slot (17) and right hand slot (18)
                int leftHandSlot = 17;
                int rightHandSlot = 18;
                string lanternItemName = "Lantern";

                // Check left hand
                if (playerInventory.SlotToStack.TryGetValue(leftHandSlot, out var leftStack))
                {
                    if (leftStack.ItemType.name.Equals(lanternItemName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Check right hand
                if (playerInventory.SlotToStack.TryGetValue(rightHandSlot, out var rightStack))
                {
                    if (rightStack.ItemType.name.Equals(lanternItemName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
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

        // Find and recharge the lantern
        var lanternController = FindFirstObjectByType<LanternController>();
        if (lanternController != null)
        {
            lanternController.RechargeLantern(rechargeTime);
        }
        else
        {
            DebugManager.LogWarning("LuminiPickup: Could not find LanternController in scene!");
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
