using TMPro;
using UnityEngine;

public class OxygenDisplay : MonoBehaviour
{
    public enum DisplayFormat
    {
        RawValue, // "50"
        Percentage, // "50%"
        ValueAndPercentage // "50/100 (50%)"
    }

    [Header("References")] [SerializeField]
    private Oxygen oxygenComponent;

    [SerializeField] private TMP_Text oxygenText;

    [Header("Display Settings")] [SerializeField]
    private DisplayFormat displayFormat = DisplayFormat.RawValue;

    [SerializeField] private bool showDecimal;
    [SerializeField] private string formatString = "F0"; // "F0" = no decimals, "F1" = 1 decimal, etc.

    [Header("Color Settings (Optional)")] [SerializeField]
    private bool useColorGradient;

    [SerializeField] private Color fullOxygenColor = Color.green;
    [SerializeField] private Color lowOxygenColor = Color.red;
    [SerializeField] private float lowOxygenThreshold = 0.25f; // 25% remaining

    private void Start()
    {
        // Try to find oxygen component if not assigned
        if (oxygenComponent == null)
        {
            oxygenComponent = FindObjectOfType<Oxygen>();
            if (oxygenComponent == null)
            {
                Debug.LogError("OxygenDisplay: No Oxygen component found in scene!");
                enabled = false;
                return;
            }
        }

        // Check if text component is assigned
        if (oxygenText == null)
        {
            oxygenText = GetComponent<TMP_Text>();
            if (oxygenText == null)
            {
                Debug.LogError("OxygenDisplay: No TMP_Text component found!");
                enabled = false;
            }
        }

        // Initial update
        UpdateDisplay();
    }

    private void Update()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (oxygenComponent == null || oxygenText == null) return;

        var currentOxygen = oxygenComponent.OxygenLevel;
        var maxOxygen = oxygenComponent.MaxOxygen;
        var percentage = oxygenComponent.OxygenPercentage * 100f;

        // Format the display text based on selected format
        var displayText = "";

        switch (displayFormat)
        {
            case DisplayFormat.RawValue:
                displayText = FormatValue(currentOxygen);
                break;

            case DisplayFormat.Percentage:
                displayText = $"{FormatValue(percentage)}%";
                break;

            case DisplayFormat.ValueAndPercentage:
                displayText = $"{FormatValue(currentOxygen)}/{FormatValue(maxOxygen)} ({FormatValue(percentage)}%)";
                break;
        }

        // Update text
        oxygenText.text = displayText;

        // Update color if gradient is enabled
        if (useColorGradient) UpdateTextColor(oxygenComponent.OxygenPercentage);
    }

    private string FormatValue(float value)
    {
        if (showDecimal) return value.ToString(formatString);

        return Mathf.RoundToInt(value).ToString();
    }

    private void UpdateTextColor(float oxygenPercentage)
    {
        if (!useColorGradient || oxygenText == null) return;

        // Lerp color based on oxygen percentage
        var t = Mathf.Clamp01(oxygenPercentage / lowOxygenThreshold);
        var targetColor = Color.Lerp(lowOxygenColor, fullOxygenColor, t);

        oxygenText.color = targetColor;
    }

    /// <summary>
    ///     Manually refresh the display (useful if format changes at runtime)
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateDisplay();
    }

    /// <summary>
    ///     Change display format at runtime
    /// </summary>
    public void SetDisplayFormat(DisplayFormat newFormat)
    {
        displayFormat = newFormat;
        UpdateDisplay();
    }

    /// <summary>
    ///     Toggle whether to show decimal values
    /// </summary>
    public void SetShowDecimal(bool show)
    {
        showDecimal = show;
        UpdateDisplay();
    }
}