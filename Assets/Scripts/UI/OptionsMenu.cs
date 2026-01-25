using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class OptionsMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject optionsMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Audio Settings")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private AudioManager audioManager;

    public bool IsOpen { get; private set; } = false;

    private void Start()
    {
        if (optionsMenuPanel != null)
        {
            optionsMenuPanel.SetActive(false);
        }

        // Initialize volume slider if it exists
        if (volumeSlider != null && audioManager != null)
        {
            volumeSlider.value = audioManager.GetMasterVolume();
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    public void OpenOptionsMenu()
    {
        if (optionsMenuPanel != null)
        {
            optionsMenuPanel.SetActive(true);
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        IsOpen = true;

        DebugManager.Log("OptionsMenu: Opened");
    }

    public void CloseOptionsMenu()
    {
        if (optionsMenuPanel != null)
        {
            optionsMenuPanel.SetActive(false);
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        IsOpen = false;

        DebugManager.Log("OptionsMenu: Closed, returned to pause menu");
    }

    private void OnVolumeChanged(float value)
    {
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(value);
        }
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }
    }
}