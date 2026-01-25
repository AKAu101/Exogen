using UnityEngine;

public class PlayButtonEventBinder : MonoBehaviour                                      
{
    public void InitPlayButton()
    {
        SceneChanger.instance.LoadSceneByName("Game");
    }
}