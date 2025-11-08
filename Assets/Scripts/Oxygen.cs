using UnityEngine;
using UnityEngine.UI;

public class Oxygen : MonoBehaviour
{
    [SerializeField] private float oxygenLevel;
    [SerializeField] private float oxygenMaxCapacity;
    [SerializeField] private bool isInBreathableAir = false;
    
    //References
    [Header("References")]
    [SerializeField] private Slider oxygenSlider;
    [SerializeField] private FirstPersonController playerController;
    
    [Header("Depletion Rates")]
    [SerializeField] private float normalDepletionRate = 1f;
    [SerializeField] private float sprintDepletionRate = 2.5f;
    
    private bool isDead = false;
    
    void Start()
    {
        UpdateOxygenUI();
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