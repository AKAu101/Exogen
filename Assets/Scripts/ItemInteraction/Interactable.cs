using UnityEngine;
using UnityEngine.Events;

/// <summary>
///     Component that marks a GameObject as interactable.
///     Uses Outline component for visual feedback when hovering.
/// </summary>
public class Interactable : MonoBehaviour
{
    public string message;
    public GameObjectEvent OnInteract;
    private Outline outline;

    private void Start()
    {
        outline = GetComponent<Outline>();
        DisableOutline();
    }

    public void Interact(GameObject interactor)
    {
        OnInteract.Invoke(interactor);
    }

    public void DisableOutline()
    {
        if (outline != null) outline.enabled = false;
    }

    public void EnableOutline()
    {
        if (outline != null) outline.enabled = true;
    }
}