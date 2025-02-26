using UnityEngine;
using UnityEngine.UI;           // For Button
using TMPro;                    // For TextMeshProUGUI
using DG.Tweening;
using System.Collections.Generic;

public class DifficultySelectorTMP : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI currentText;  // Drag "CurrentText" TMP object here
    public TextMeshProUGUI nextText;     // Drag "NextText" TMP object here
    public Button leftButton;            // Drag "LeftButton" here
    public Button rightButton;           // Drag "RightButton" here

    [Header("Positions")]
    public RectTransform leftPos;        // RectTransform marking left off-screen position
    public RectTransform centerPos;      // RectTransform marking center position
    public RectTransform rightPos;       // RectTransform marking right off-screen position

    [Header("Difficulties")]
    public List<string> difficulties;    // e.g. ["Easy", "Medium", "Hard", "Expert"]

    public int currentIndex = 0;

    private void Start()
    {
        // Provide default difficulties if none set
        if (difficulties == null || difficulties.Count == 0)
        {
            difficulties = new List<string> { "Easy", "Medium", "Hard", "Expert" };
        }

        // Initialize the text and positions
        currentIndex = 0;
        currentText.text = difficulties[currentIndex];
        currentText.rectTransform.position = centerPos.position;

        // Ensure nextText is off-screen (e.g., on the right by default)
        nextText.text = "";
        nextText.rectTransform.position = rightPos.position;

        UpdateButtonInteractivity();
    }

    public void OnClickNext()
    {
        if (currentIndex < difficulties.Count - 1)
        {
            currentIndex++;
            SlideTransition(isToRight: true);
            GameManager.Instance.currentDifficulty = currentIndex;
        }
    }

    public void OnClickPrevious()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            SlideTransition(isToRight: false);
            GameManager.Instance.currentDifficulty = currentIndex;
        }
    }

    private void SlideTransition(bool isToRight)
    {
        // Kill any ongoing tweens to avoid overlap
        DOTween.Kill(currentText.rectTransform);
        DOTween.Kill(nextText.rectTransform);

        // Prepare new text
        nextText.text = difficulties[currentIndex];

        if (isToRight)
        {
            // currentText slides left
            currentText.rectTransform
                .DOMoveX(leftPos.position.x, 0.5f)
                .SetEase(Ease.OutCubic);

            // nextText slides in from the right
            nextText.rectTransform.position = rightPos.position;
            nextText.rectTransform
                .DOMoveX(centerPos.position.x, 0.5f)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    // Swap roles
                    currentText.text = nextText.text;
                    currentText.rectTransform.position = centerPos.position;
                    nextText.rectTransform.position = rightPos.position;
                    nextText.text = "";
                });
        }
        else
        {
            // currentText slides right
            currentText.rectTransform
                .DOMoveX(rightPos.position.x, 0.5f)
                .SetEase(Ease.OutCubic);

            // nextText slides in from the left
            nextText.rectTransform.position = leftPos.position;
            nextText.rectTransform
                .DOMoveX(centerPos.position.x, 0.5f)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    // Swap roles
                    currentText.text = nextText.text;
                    currentText.rectTransform.position = centerPos.position;
                    nextText.rectTransform.position = leftPos.position;
                    nextText.text = "";
                });
        }

        UpdateButtonInteractivity();
    }

    private void UpdateButtonInteractivity()
    {
        // Disable the left button if we're at the first difficulty
        leftButton.gameObject.SetActive(currentIndex > 0);

        // Disable the right button if we're at the last difficulty
        rightButton.gameObject.SetActive(currentIndex < difficulties.Count - 1); 
    }
}
