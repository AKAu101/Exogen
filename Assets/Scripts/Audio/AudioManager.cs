using System.CodeDom;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using FMODUnity;
using FMOD.Studio;


public class AudioManager : MonoBehaviour
{
    [SerializeField] EventReference FootstepsSprint;
    [SerializeField] EventReference FootstepsWalk;
    [SerializeField] EventReference Wind;
    [SerializeField] GameObject player;
    [SerializeField] FirstPersonController controller;
    [SerializeField] AtmosphereTransition atmosphere;
    [SerializeField] EventReference playerDamageSound;
    [SerializeField] PlayerConsumptionManager eat;

    [Header("Volume Control")]
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;

    [SerializeField] EventReference luminiPickupSound;

    [SerializeField] EventReference itemAddedSound;
    [SerializeField] EventReference itemMovedSound;
    [SerializeField] EventReference eatSound;
    
    private InventorySystem inventorySystem;
    private LuminiPickup[] allLumini;

    private EventInstance footstepSprintInstance;
    private EventInstance footstepWalkInstance;
    private EventInstance windInstance;
    private bool wasPlayingLastFrame = false;
    private bool wasGroundedLastFrame = true;
    private bool wasEatingLastFrame = true;
    private bool wasWindyLastFrame = false;
    private bool wasWalkingLastFrame = false;

    // VCA for volume control
    private VCA masterVCA;
    void Awake()
{
    RuntimeManager.LoadBank("Master", true);
    RuntimeManager.LoadBank("Master.strings", true);
}
    void Start()
    {
        footstepSprintInstance = RuntimeManager.CreateInstance(FootstepsSprint);
        footstepWalkInstance = RuntimeManager.CreateInstance(FootstepsWalk);
        windInstance = RuntimeManager.CreateInstance(Wind);
        EventDescription jumpDesc = RuntimeManager.GetEventDescription("event:/Footsteps Stone Jump");
        EventDescription landDesc = RuntimeManager.GetEventDescription("event:/Footsteps Stone Land");
        EventDescription metalJumpDesc = RuntimeManager.GetEventDescription("event:/Footsteps Metal Jump");
        EventDescription metalLandDesc = RuntimeManager.GetEventDescription("event:/Footsteps Metal Land");
        jumpDesc.loadSampleData();
        landDesc.loadSampleData();
        metalJumpDesc.loadSampleData();
        metalLandDesc.loadSampleData();

        allLumini = FindObjectsOfType<LuminiPickup>();

        foreach (var lumini in allLumini)
        {
            var interactable = lumini.GetComponent<Interactable>();
            if (interactable != null)
            {
                interactable.OnInteract.AddListener(OnLuminiPickedUp);
            }
        }

        // Initialize VCA for volume control
        try
        {
            masterVCA = RuntimeManager.GetVCA("vca:/Master");
            SetMasterVolume(masterVolume);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"Could not find Master VCA. Make sure you have a VCA called 'Master' in your FMOD project. Error: {e.Message}");
        }

        inventorySystem = InventorySystem.Instance;
        
        if (inventorySystem != null)
        {
            inventorySystem.OnItemAdded += PlayItemAddedSound;
            inventorySystem.OnItemMoved += PlayItemMovedSound;
        }
        PlayerHealth.OnPlayerDamaged += PlayPlayerDamageSound;
    }
    void Update()
    {
        bool isGrounded = controller.IsGrounded;
        bool isSprinting = controller.IsSprinting;
        bool jumpPressed = controller.JumpPressed;
        bool isWalking = controller.IsWalking;
       
        //Volume control with U and I keys
        HandleVolumeInput();
        
        // Started sprinting
        bool shouldPlay = isSprinting && isGrounded;


        // Started meeting conditions
        if (shouldPlay && !wasPlayingLastFrame)
        {
            footstepSprintInstance.start();
        }
        // Stopped meeting conditions
        else if (!shouldPlay && wasPlayingLastFrame)
        {
            footstepSprintInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }

        wasPlayingLastFrame = shouldPlay;

        if(isWalking & !wasWalkingLastFrame)
        {
            footstepWalkInstance.start();
        }
        else if (!isWalking && wasWalkingLastFrame)
        {
            footstepWalkInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }

        wasWalkingLastFrame = isWalking;

        if (eat.IsConsuming&&!wasEatingLastFrame)
        {
            RuntimeManager.PlayOneShot(eatSound);
        }

        wasEatingLastFrame = eat.IsConsuming;

        JumpAudio();
        OutdoorSounds();
    }

    private void HandleVolumeInput()
    {
        // Increase volume with U key (only if not already at max)
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (masterVolume < 1f)
            {
                SetMasterVolume(masterVolume + 0.1f);
                UnityEngine.Debug.Log($"Master Volume: {masterVolume * 100}%");
            }
            else
            {
                UnityEngine.Debug.Log("Volume already at maximum (100%)");
            }
        }

        // Decrease volume with I key (only if not already at min)
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (masterVolume > 0f)
            {
                SetMasterVolume(masterVolume - 0.1f);
                UnityEngine.Debug.Log($"Master Volume: {masterVolume * 100}%");
            }
            else
            {
                UnityEngine.Debug.Log("Volume already at minimum (0%)");
            }
        }
    }

    private void PlayPlayerDamageSound()
    {
        RuntimeManager.PlayOneShot(playerDamageSound);
    }

    private void OnLuminiPickedUp(GameObject interactor)
    {
        RuntimeManager.PlayOneShot(luminiPickupSound);
    }

    private void PlayItemAddedSound(IInventoryData inventory, ItemData item, int slot)
    {
        RuntimeManager.PlayOneShot(itemAddedSound);
    }
    
    private void PlayItemMovedSound(IInventoryData fromInventory, int fromSlot, IInventoryData toInventory, int toSlot)
    {
        RuntimeManager.PlayOneShot(itemMovedSound);
    }

    void JumpAudio()
    {
        bool isGrounded = controller.IsGrounded;
        bool jumpPressed = controller.JumpPressed;

        if (jumpPressed && !atmosphere.IsInside)
        {
            RuntimeManager.PlayOneShotAttached("event:/Footsteps Stone Jump", player);
        }
        if (isGrounded && !wasGroundedLastFrame)
        {
            RuntimeManager.PlayOneShotAttached("event:/Footsteps Stone Land", player);
        }

        if (jumpPressed && atmosphere.IsInside)
        {
            RuntimeManager.PlayOneShotAttached("event:/Footsteps Metal Jump", player);
        }
        if (isGrounded && !wasGroundedLastFrame && atmosphere.IsInside)
        {
            RuntimeManager.PlayOneShotAttached("event:/Footsteps Metal Land", player);
        }

        wasGroundedLastFrame = isGrounded;
    }

    void OutdoorSounds()
    {
        if(!wasWindyLastFrame)
        {
            windInstance.start();
        }
        if (!atmosphere.IsInside)
        {
            wasWindyLastFrame = true;
            windInstance.setParameterByName("Volume", 1);
            footstepSprintInstance.setParameterByName("Surface", 0);
            footstepWalkInstance.setParameterByName("Surface", 0);
        }
        else if (atmosphere.IsInside)
        {
            wasWindyLastFrame = true;
            windInstance.setParameterByName("Volume", 0);
            footstepSprintInstance.setParameterByName("Surface", 1);
            footstepWalkInstance.setParameterByName("Surface", 1);
        }
    }
    
    /// <summary>
    /// Set the master volume (0 to 1)
    /// Can be called from UI sliders or code
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (masterVCA.isValid())
        {
            masterVCA.setVolume(masterVolume);
        }
    }

    /// <summary>
    /// Get the current master volume (0 to 1)
    /// </summary>
    public float GetMasterVolume()
    {
        return masterVolume;
    }

    void OnDestroy()
    {
        // Clean up the instance
        footstepSprintInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        footstepSprintInstance.release();
        footstepWalkInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        footstepWalkInstance.release();
        windInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        windInstance.release();
        PlayerHealth.OnPlayerDamaged -= PlayPlayerDamageSound;

        if (inventorySystem != null)
        {
            inventorySystem.OnItemAdded -= PlayItemAddedSound;
            inventorySystem.OnItemMoved -= PlayItemMovedSound;
        }
        
        if (allLumini != null)
        {
            foreach (var lumini in allLumini)
            {
                if (lumini != null)
                {
                    var interactable = lumini.GetComponent<Interactable>();
                    if (interactable != null)
                    {
                        interactable.OnInteract.RemoveListener(OnLuminiPickedUp);
                    }
                }
            }
        }
    }
}