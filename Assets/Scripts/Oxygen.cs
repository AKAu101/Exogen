using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class Oxygen : MonoBehaviour
{
    [SerializeField] private float oxygenLevel;
    [SerializeField] private float oxygenMaxCapacity;
    [SerializeField] private bool isInBreathableAir = false;

    //References
    [Header("References")]
    [SerializeField] private Slider oxygenSlider;
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private Volume globalVolume;

    [Header("Depletion Rates")]
    [SerializeField] private float normalDepletionRate = 1f;
    [SerializeField] private float sprintDepletionRate = 2.5f;

    [Header("Vignette Settings")]
    [SerializeField] private float vignetteThreshold = 8f; // Show vignette when oxygen <= this value
    [SerializeField] private float maxVignetteIntensity = 1f; // Max intensity of vignette (0-1)

    private bool isDead = false;
    private UnityEngine.Rendering.Universal.Vignette vignette;
    
    void Start()
    {
        UpdateOxygenUI();

        // Get vignette component from the global volume
        if (globalVolume != null && globalVolume.profile.TryGet(out vignette))
        {
            vignette.active = true;
            Debug.Log("Vignette component found and initialized");
        }
        else
        {
            Debug.LogWarning("Vignette component not found in Global Volume!");
        }

        UpdateVignette();
    }
    
    void Update()
    {
        if (isDead) return;
        
        if (!isInBreathableAir)
        {
            // Only deplete oxygen when NOT in safe zone
            float currentDepletionRate = normalDepletionRate;
            if (playerController != null && playerController.IsSprinting)
            {
                currentDepletionRate = sprintDepletionRate;
            }

            oxygenLevel -= Time.deltaTime * currentDepletionRate;
            oxygenLevel = Mathf.Clamp(oxygenLevel, 0, oxygenMaxCapacity);
        }
        else
        {
            // Restore oxygen when in safe zone
            oxygenLevel += Time.deltaTime * 2f;
            oxygenLevel = Mathf.Clamp(oxygenLevel, 0, oxygenMaxCapacity);
        }

        UpdateOxygenUI();
        UpdateVignette();

        if (oxygenLevel <= 0.0f)
        {
            isDead = true;
            Debug.Log("Oxygen depleted");
        }
    }
    
    private void UpdateOxygenUI()
    {
        if (oxygenSlider != null)
        {
            oxygenSlider.value = oxygenLevel / oxygenMaxCapacity;
        }
    }

    private void UpdateVignette()
    {
        if (vignette == null) return;

        // Calculate vignette intensity based on oxygen level
        float vignetteIntensity = 0f;

        if (oxygenLevel <= vignetteThreshold)
        {
            // Map oxygen level to vignette intensity
            // When oxygen = vignetteThreshold, intensity = 0
            // When oxygen = 0, intensity = maxVignetteIntensity
            vignetteIntensity = Mathf.Lerp(maxVignetteIntensity, 0f, oxygenLevel / vignetteThreshold);
        }

        // Apply the intensity to the vignette
        vignette.intensity.value = vignetteIntensity;
    }

    // For trigger colliders
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("OxygenArea"))
        {
            isInBreathableAir = true;
            Debug.Log("Entered safe zone - oxygen restoring");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("OxygenArea"))
        {
            isInBreathableAir = false;
            Debug.Log("Left safe zone - oxygen depleting");
        }
    }
}