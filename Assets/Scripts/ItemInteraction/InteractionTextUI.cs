using Generals;
using TMPro;
using UnityEngine;

/// <summary>
///     Displays interaction prompts to the player when looking at interactable objects.
///     Shows and hides text with the appropriate interaction key.
/// </summary>
public class InteractionTextUI : Singleton<InteractionTextUI>
{
    [SerializeField] private TMP_Text interactionText;

    public void EnableInteractionText(string text)
    {
        interactionText.text = text + " (E)";
        interactionText.gameObject.SetActive(true);
    }

    public void DisableInteractionText()
    {
        interactionText.gameObject.SetActive(false);
    }
}