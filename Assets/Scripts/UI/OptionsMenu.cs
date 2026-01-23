using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OptionsMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject optionsMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Audio Settings")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private AudioManager audioManager;

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