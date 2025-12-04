using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the pause menu UI and handles pause/resume functionality.
/// Listens to FirstPersonController.OnPausePressed event (Escape key).
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;

    private IUIStateManagement uiStateManagement;
    private bool isPaused = false;
    private bool justClosedInventory = false;

    private void OnEnable()
    {
        // Subscribe to pause input event from FirstPersonController
        FirstPersonController.OnPausePressed += HandlePauseInput;
    }

    private void OnDisable()
    {
        // Unsubscribe from pause input event
        FirstPersonController.OnPausePressed -= HandlePauseInput;
    }

    private void Start()
    {
        // Ensure UIStateManager exists
        UIStateManager.EnsureInstance();

        // Get UI state management from ServiceLocator
        if (ServiceLocator.Instance.IsRegistered<IUIStateManagement>())
        {
            uiStateManagement = ServiceLocator.Instance.Get<IUIStateManagement>();

            // Reset pause state when scene loads (in case it persisted from previous scene)
            if (uiStateManagement.IsPauseMenuVisible)
            {
                DebugManager.Log("PauseMenu: Resetting persisted pause state from previous scene");
                // Access UIStateManager directly to reset without triggering cursor lock
                var uiStateManager = uiStateManagement as UIStateManager;
                if (uiStateManager != null)
                {
                    uiStateManager.ResetPauseState();
                }
            }

            uiStateManagement.OnPauseMenuVisibilityChanged += HandlePauseMenuVisibilityChanged;
            DebugManager.Log("PauseMenu: UIStateManagement registered successfully");
        }
        else
        {
            DebugManager.LogError("PauseMenu: IUIStateManagement not found in ServiceLocator!");
        }

        // Hide pause menu initially
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Ensure game is unpaused when pause menu is destroyed (scene transition)
        Time.timeScale = 1f;

        // Unsubscribe from events
        if (uiStateManagement != null)
        {
            uiStateManagement.OnPauseMenuVisibilityChanged -= HandlePauseMenuVisibilityChanged;
        }

        DebugManager.Log("PauseMenu: OnDestroy - reset time scale");
    }

    private void HandlePauseInput()
    {
        DebugManager.Log($"PauseMenu.HandlePauseInput: uiStateManagement={uiStateManagement != null}, IsInventoryVisible={uiStateManagement?.IsInventoryVisible}, isPaused={isPaused}, justClosedInventory={justClosedInventory}");

        // If we just closed inventory, skip this input to prevent pause menu from opening
        if (justClosedInventory)
        {
            DebugManager.Log("PauseMenu: Just closed inventory, resetting flag and ignoring input");
            justClosedInventory = false;
            return;
        }

        // If inventory is open, close it and DO NOT open pause menu
        if (uiStateManagement != null && uiStateManagement.IsInventoryVisible)
        {
            DebugManager.Log("PauseMenu: Inventory is open, closing it and setting flag");
            uiStateManagement.ToggleInventory();
            justClosedInventory = true;
            return;
        }

        // If pause menu is open, close it
        if (isPaused)
        {
            DebugManager.Log("PauseMenu: Pause menu is open, closing it");
            SetPauseMenuActive(false);
            return;
        }

        // Nothing is open, open pause menu
        DebugManager.Log("PauseMenu: Nothing is open, opening pause menu");
        SetPauseMenuActive(true);
    }

    private void HandlePauseMenuVisibilityChanged(bool visible)
    {
        isPaused = visible;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(visible);
        }
    }

    public void TogglePause()
    {
        // Use the same logic as HandlePauseInput
        HandlePauseInput();
    }

    public void Resume()
    {
        SetPauseMenuActive(false);
    }

    private void SetPauseMenuActive(bool active)
    {
        isPaused = active;

        // Show/hide the panel
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(active);
        }

        // Handle cursor and time scale directly
        Time.timeScale = active ? 0f : 1f;
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = active;

        // Notify UIStateManager
        if (uiStateManagement != null)
        {
            DebugManager.Log($"PauseMenu: Notifying UIStateManager, pause={active}");
            uiStateManagement.SetPauseMenuVisible(active);
        }

        DebugManager.Log($"PauseMenu: Pause set to {active}, Cursor visible={Cursor.visible}");
    }

    public void QuitToMainMenu()
    {
        // Resume time before loading scene
        Time.timeScale = 1f;

        // Load main menu scene (adjust scene name as needed)
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        DebugManager.Log("Quitting game...");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
