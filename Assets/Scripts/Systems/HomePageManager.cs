using UnityEngine;

public class HomePageManager : MonoBehaviour
{
    public RotateImageEaseOut rotatorScript;


    public void OnResumeButtonClicked()
    {
        GameManager.Instance.ShouldResumeGame();
        rotatorScript.StartRotation();
    }
}
