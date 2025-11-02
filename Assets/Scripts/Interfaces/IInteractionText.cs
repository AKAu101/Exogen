using System;

public interface IInteractionText
{
    // Events
    event Action<string> OnInteractionTextRequested;
    event Action OnInteractionTextDisableRequested;

    // Methods
    void EnableInteractionText(string message);
    void DisableInteractionText();
}