using UnityEngine;
using UnityEngine.UI;

public class HealthIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthImage;

    [Header("Color Settings")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color midHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    
    [Header("Health Thresholds")]
    [SerializeField] private float midHealthThreshold = 0.6f;  // 60% - green to yellow
    [SerializeField] private float lowHealthThreshold = 0.3f;  // 30% - yellow to red

    void Start()
    {
        // Try to find player health if not assigned
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        // Try to find Image component if not assigned
        if (healthImage == null)
        {
            healthImage = GetComponent<Image>();
        }

        // Initial update
        UpdateImageColor();
    }

    void Update()
    {
        UpdateImageColor();
    }

    private void UpdateImageColor()
    {
        if (playerHealth == null || healthImage == null) return;

        float healthPercentage = playerHealth.CurrentHealth / playerHealth.MaxHealth;
        healthImage.color = GetHealthColor(healthPercentage);
    }

    private Color GetHealthColor(float healthPercentage)
    {
        if (healthPercentage > midHealthThreshold)
        {
            // Green to Yellow transition (1.0 to 0.6)
            float t = (healthPercentage - midHealthThreshold) / (1f - midHealthThreshold);
            return Color.Lerp(midHealthColor, fullHealthColor, t);
        }
        else if (healthPercentage > lowHealthThreshold)
        {
            // Yellow to Red transition (0.6 to 0.3)
            float t = (healthPercentage - lowHealthThreshold) / (midHealthThreshold - lowHealthThreshold);
            return Color.Lerp(lowHealthColor, midHealthColor, t);
        }
        else
        {
            // Red zone (0.3 to 0)
            return lowHealthColor;
        }
    }

    /// <summary>
    /// Force an immediate update of the health indicator
    /// </summary>
    public void ForceUpdate()
    {
        UpdateImageColor();
    }

    /// <summary>
    /// Set custom colors for the health indicator
    /// </summary>
    public void SetColors(Color fullColor, Color midColor, Color lowColor)
    {
        fullHealthColor = fullColor;
        midHealthColor = midColor;
        lowHealthColor = lowColor;
        ForceUpdate();
    }

    /// <summary>
    /// Set health thresholds for color changes
    /// </summary>
    public void SetThresholds(float midThreshold, float lowThreshold)
    {
        midHealthThreshold = Mathf.Clamp01(midThreshold);
        lowHealthThreshold = Mathf.Clamp01(lowThreshold);
        
        // Ensure thresholds are in correct order
        if (midHealthThreshold < lowHealthThreshold)
        {
            float temp = midHealthThreshold;
            midHealthThreshold = lowHealthThreshold;
            lowHealthThreshold = temp;
        }
        
        ForceUpdate();
    }
}