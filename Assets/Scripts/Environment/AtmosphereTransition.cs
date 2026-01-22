using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AtmosphereTransition : MonoBehaviour
{
    [Header("References")]
    public Volume postProcessVolume;
    
    [Header("Transition")]
    public float transitionSpeed = 2f; // Increased for better speed control
    
    [Header("Outside Values")]
    public float outsideFogDensity = 0.06f;
    public float outsideBloom = 0.5f;
    public float outsideFilmGrain = 200f;
    public float outsideChromatic = 0.4f;
    public float outsideMotionBlur = 0f;
    
    [Header("Inside Values")]
    public float insideFogDensity = 0.06f;
    public float insideBloom = 0f;
    public float insideFilmGrain = 0f;
    public float insideChromatic = 0f;
    public float insideMotionBlur = 0f;
    
    private bool isInside = false;

    //Getter
    public bool IsInside => isInside;
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
        
        // Initialize to outside values
        ApplyValues(currentLerp);
    }
    
    void Update()
    {
        // Determine direction based on isInside
        float direction = isInside ? -1f : 1f;
        
        // Update currentLerp with smooth transition
        currentLerp = Mathf.Clamp01(currentLerp + (direction * transitionSpeed * Time.deltaTime));
        
        // Apply all values based on current lerp
        ApplyValues(currentLerp);
    }
    
    private void ApplyValues(float lerpValue)
    {
        // Apply fog
        RenderSettings.fogDensity = Mathf.Lerp(insideFogDensity, outsideFogDensity, lerpValue);
        
        // Apply post processing
        if (bloom != null)
            bloom.intensity.value = Mathf.Lerp(insideBloom, outsideBloom, lerpValue);
        
        if (filmGrain != null)
            filmGrain.intensity.value = Mathf.Lerp(insideFilmGrain, outsideFilmGrain, lerpValue);
        
        if (chromatic != null)
            chromatic.intensity.value = Mathf.Lerp(insideChromatic, outsideChromatic, lerpValue);
        
        if (motionBlur != null)
            motionBlur.intensity.value = Mathf.Lerp(insideMotionBlur, outsideMotionBlur, lerpValue);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Don't disable fog immediately - let it transition
            // RenderSettings.fog = false;
            isInside = true;
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Don't enable fog immediately - let it transition
            // RenderSettings.fog = true;
            isInside = false;
        }
    }
}