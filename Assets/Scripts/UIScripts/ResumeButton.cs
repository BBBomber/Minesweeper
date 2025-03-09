using UnityEngine;
using UnityEngine.UI;

public class ResumeButton : MonoBehaviour
{
    public Button resumeButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(GameManager.Instance.HasSavedGame(GameManager.Instance.currentDifficulty))
        {
            resumeButton.gameObject.SetActive(true);
        }
        else { resumeButton.gameObject.SetActive(false);}
    }


    public void OnDifficultyChanged()
    {
        if (GameManager.Instance.HasSavedGame(GameManager.Instance.currentDifficulty))
        {
            resumeButton.gameObject.SetActive(true);
        }
        else { resumeButton.gameObject.SetActive(false); }
    }

}
