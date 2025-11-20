using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AtmosphereTransition : MonoBehaviour
{
    [Header("References")]
    public Volume postProcessVolume;
    
    [Header("Transition")]
    public float transitionSpeed = 1f;
    
    [Header("Outside Values")]
    public float outsideFogDensity = 1f;
    public float outsideBloom = 490f;
    public float outsideFilmGrain = 0.7f;
    public float outsideChromatic = 0.2f;
    public float outsideMotionBlur = 0.4f;
    
    [Header("Inside Values")]
    public float insideFogDensity = 0f;
    public float insideBloom = 100f;
    public float insideFilmGrain = 0.3f;
    public float insideChromatic = 0.05f;
    public float insideMotionBlur = 0.1f;
    
    private bool isInside = false;
    private float currentLerp = 1f; // 1 = outside, 0 = inside
    
    // Post processing components
    private Vignette vignette;
    private Bloom bloom;
    private FilmGrain filmGrain;
    private ChromaticAberration chromatic;
    private MotionBlur motionBlur;
    
    void Start()
    {
        // Get all post processing effects
        postProcessVolume.profile.TryGet(out bloom);
        postProcessVolume.profile.TryGet(out filmGrain);
        postProcessVolume.profile.TryGet(out chromatic);
        postProcessVolume.profile.TryGet(out motionBlur);
    }
    
    void Update()
    {
        // Smoothly transition
        float target;
        if (isInside)
        {
            target = 0f;
        }
        else
        {
            target = 1f;
        }
        
        currentLerp = Mathf.Lerp(currentLerp, target, Time.deltaTime * transitionSpeed);
        
        // Apply fog
        RenderSettings.fogDensity = Mathf.Lerp(insideFogDensity, outsideFogDensity, currentLerp);
        
        // Apply post processing
        if (bloom != null)
            bloom.intensity.value = Mathf.Lerp(insideBloom, outsideBloom, currentLerp);
        
        if (filmGrain != null)
            filmGrain.intensity.value = Mathf.Lerp(insideFilmGrain, outsideFilmGrain, currentLerp);
        
        if (chromatic != null)
            chromatic.intensity.value = Mathf.Lerp(insideChromatic, outsideChromatic, currentLerp);
        
        if (motionBlur != null)
            motionBlur.intensity.value = Mathf.Lerp(insideMotionBlur, outsideMotionBlur, currentLerp);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RenderSettings.fog = false;
            isInside = true;
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RenderSettings.fog = true;
            isInside = false;
        }
    }
}