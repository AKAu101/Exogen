using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AudioManagerMenu : MonoBehaviour
{
        [Header("Volume Control")]
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
    private VCA masterVCA;
    void Awake()
    {
        RuntimeManager.LoadBank("Master", true);
        RuntimeManager.LoadBank("Master.strings", true);
    }
    void Start()
    {
        try
        {
            masterVCA = RuntimeManager.GetVCA("vca:/Master");
            SetMasterVolume(masterVolume);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"Could not find Master VCA. Make sure you have a VCA called 'Master' in your FMOD project. Error: {e.Message}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (masterVCA.isValid())
        {
            masterVCA.setVolume(masterVolume);
        }
    }
    public float GetMasterVolume()
    {
        return masterVolume;
    }
}

