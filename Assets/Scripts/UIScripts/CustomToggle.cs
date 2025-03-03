using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CustomToggle : MonoBehaviour
{
    [SerializeField] private Image revealIcon;  // The reveal icon (shown in Reveal Mode)
    [SerializeField] private Image flagIcon;    // The flag icon (shown in Flag Mode)
    [SerializeField] private Image background;  // The background that changes color
    [SerializeField] private Color revealColor = Color.white;   // Background color for Reveal Mode
    [SerializeField] private Color flagColor = new Color(1f, 0.5f, 0.5f); // Background color for Flag Mode
    [SerializeField] private float animDuration = 0.3f;  // Animation duration

    private bool isFlagMode = false;  // Tracks the current mode

    public System.Action<bool> OnToggleChanged; // Event for notifying GameManager

    void Start()
    {
        // Ensure the correct initial state
        revealIcon.gameObject.SetActive(true);
        flagIcon.gameObject.SetActive(false);
        background.color = revealColor;
    }

    public void ToggleMode()
    {
        isFlagMode = !isFlagMode;
        AnimateFlip();
        OnToggleChanged?.Invoke(isFlagMode); // Notify MinesweeperGameManager
    }

    private void AnimateFlip()
    {
        // Step 1: Shrink the toggle (Y scale to 0)
        transform.DOScaleY(0, animDuration / 2).SetEase(Ease.InQuad).OnComplete(() =>
        {
            // Step 2: Toggle icons
            revealIcon.gameObject.SetActive(!isFlagMode);
            flagIcon.gameObject.SetActive(isFlagMode);

            // Step 3: Change background color smoothly
            background.DOColor(isFlagMode ? flagColor : revealColor, animDuration / 2);

            // Step 4: Expand the toggle (Y scale back to 1)
            transform.DOScaleY(1, animDuration / 2).SetEase(Ease.OutQuad);
        });
    }
}
