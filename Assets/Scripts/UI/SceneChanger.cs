using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple scene manager for loading and transitioning between scenes.
/// Can be used directly in the Inspector by attaching to UI buttons.
/// </summary>
public class SceneChanger : MonoBehaviour
{
    [Header("Loading Settings")]
    [SerializeField] private bool useAsyncLoading = true;
    [SerializeField] private float minimumLoadTime = 0.5f; // Minimum time to show loading (prevents flashing)

    [Header("Optional References")]
    [SerializeField] private GameObject loadingScreen; // Optional loading screen UI
    [SerializeField] private UnityEngine.UI.Slider progressBar; // Optional progress bar

    private static SceneChanger instance;

    private void Awake()
    {
        // Simple singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Public Methods - Can be called from UI buttons

    /// <summary>
    /// Load a scene by its name
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        if (useAsyncLoading)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Load a scene by its build index
    /// </summary>
    public void LoadSceneByIndex(int sceneIndex)
    {
        if (useAsyncLoading)
        {
            StartCoroutine(LoadSceneAsync(sceneIndex));
        }
        else
        {
            SceneManager.LoadScene(sceneIndex);
        }
    }

    /// <summary>
    /// Reload the current scene
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        LoadSceneByName(currentSceneName);
    }

    /// <summary>
    /// Load the next scene in the build settings
    /// </summary>
    public void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            LoadSceneByIndex(nextSceneIndex);
        }
        else
        {
            DebugManager.LogWarning("No next scene available. Current scene is the last one in build settings.");
        }
    }

    /// <summary>
    /// Load the previous scene in the build settings
    /// </summary>
    public void LoadPreviousScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int previousSceneIndex = currentSceneIndex - 1;

        if (previousSceneIndex >= 0)
        {
            LoadSceneByIndex(previousSceneIndex);
        }
        else
        {
            DebugManager.LogWarning("No previous scene available. Current scene is the first one in build settings.");
        }
    }

    /// <summary>
    /// Quit the application
    /// </summary>
    public void QuitGame()
    {
        DebugManager.Log("Quitting game...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    #endregion

    #region Static Methods - Can be called from anywhere

    /// <summary>
    /// Static method to load a scene from anywhere in code
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        if (instance != null)
        {
            instance.LoadSceneByName(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Static method to load a scene by index from anywhere in code
    /// </summary>
    public static void LoadScene(int sceneIndex)
    {
        if (instance != null)
        {
            instance.LoadSceneByIndex(sceneIndex);
        }
        else
        {
            SceneManager.LoadScene(sceneIndex);
        }
    }

    #endregion

    #region Async Loading

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        float startTime = Time.time;

        // Show loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // Start loading the scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // Wait until the scene is almost loaded
        while (!operation.isDone)
        {
            // Calculate progress (0 to 0.9 is loading, 0.9 to 1.0 is activation)
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            // Update progress bar if available
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // Check if loading is complete and minimum load time has passed
            if (operation.progress >= 0.9f)
            {
                float elapsedTime = Time.time - startTime;
                if (elapsedTime >= minimumLoadTime)
                {
                    operation.allowSceneActivation = true;
                }
            }

            yield return null;
        }

        // Hide loading screen (will be destroyed when scene changes anyway)
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }

    private IEnumerator LoadSceneAsync(int sceneIndex)
    {
        float startTime = Time.time;

        // Show loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // Start loading the scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;

        // Wait until the scene is almost loaded
        while (!operation.isDone)
        {
            // Calculate progress (0 to 0.9 is loading, 0.9 to 1.0 is activation)
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            // Update progress bar if available
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // Check if loading is complete and minimum load time has passed
            if (operation.progress >= 0.9f)
            {
                float elapsedTime = Time.time - startTime;
                if (elapsedTime >= minimumLoadTime)
                {
                    operation.allowSceneActivation = true;
                }
            }

            yield return null;
        }

        // Hide loading screen (will be destroyed when scene changes anyway)
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }

    #endregion
}
