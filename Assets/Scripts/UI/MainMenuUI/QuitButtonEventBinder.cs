using UnityEngine;

public class QuitButtonEventBinder : MonoBehaviour                                      
{
    public void InitQuitButton()
    {
        SceneChanger.instance.QuitGame();
    }
}