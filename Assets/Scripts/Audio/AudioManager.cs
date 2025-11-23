using System.CodeDom;
using System.Diagnostics;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;


public class AudioManager : MonoBehaviour
{
    [SerializeField] EventReference FootstepsSprint;
    [SerializeField] EventReference Wind;
    [SerializeField] GameObject player;
    [SerializeField] FirstPersonController controller;
    [SerializeField] AtmosphereTransition atmosphere;
    
    private EventInstance footstepInstance;
    private EventInstance windInstance;
    private bool wasPlayingLastFrame = false;
    private bool wasGroundedLastFrame = true;
    private bool wasWindyLastFrame = false;
    void Start()
    {
        RuntimeManager.LoadBank("Master", true);
        RuntimeManager.LoadBank("Master.strings", true);
        footstepInstance = RuntimeManager.CreateInstance(FootstepsSprint);
        windInstance = RuntimeManager.CreateInstance(Wind);
        EventDescription jumpDesc = RuntimeManager.GetEventDescription("event:/Footsteps Stone Jump");
        EventDescription landDesc = RuntimeManager.GetEventDescription("event:/Footsteps Stone Land");
        EventDescription metalJumpDesc = RuntimeManager.GetEventDescription("event:/Footsteps Metal Jump");
        EventDescription metalLandDesc = RuntimeManager.GetEventDescription("event:/Footsteps Metal Land");
        jumpDesc.loadSampleData();
        landDesc.loadSampleData();
        metalJumpDesc.loadSampleData();
        metalLandDesc.loadSampleData();
    }
    void Update()
    {
        bool isGrounded = controller.IsGrounded;
        bool isSprinting = controller.IsSprinting;
        bool jumpPressed = controller.JumpPressed;
        // Started sprinting
        bool shouldPlay = isSprinting && isGrounded;
        
        // Started meeting conditions
        if (shouldPlay && !wasPlayingLastFrame)
        {
            footstepInstance.start();
        }
        // Stopped meeting conditions
        else if (!shouldPlay && wasPlayingLastFrame)
        {
            footstepInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
        
        wasPlayingLastFrame = shouldPlay;
        JumpAudio();
        OutdoorSounds();
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
            footstepInstance.setParameterByName("Surface", 0);
        }
        else if (atmosphere.IsInside)
        {
            wasWindyLastFrame = true;
            windInstance.setParameterByName("Volume", 0);
            footstepInstance.setParameterByName("Surface", 1);
        }
    }
    
    void OnDestroy()
    {
        // Clean up the instance
        footstepInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        footstepInstance.release();
        windInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        windInstance.release();
    }
}