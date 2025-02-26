using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RotateImageEaseOut : MonoBehaviour
{
    public RectTransform targetImage;
    public float endAngle = 360f;
    public float dynamicTime = 2f;

    private bool isProcessing = false;

    public void StartRotation()
    {
        if (!isProcessing && targetImage != null)
        {
            StartCoroutine(RotateCoroutine());
        }
    }

    private IEnumerator RotateCoroutine()
    {
        isProcessing = true;

        float startAngle = targetImage.eulerAngles.z;
        float elapsedTime = 0f;

        while (elapsedTime < dynamicTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / dynamicTime);

            // Quadratic Ease-Out
            float easeOutT = 1f - Mathf.Pow(1f - t, 2f);

            float newAngle = Mathf.Lerp(startAngle, endAngle, easeOutT);
            Vector3 currentEulerAngles = targetImage.eulerAngles;
            currentEulerAngles.z = newAngle;
            targetImage.eulerAngles = currentEulerAngles;

            yield return null;
        }

        Vector3 finalEulerAngles = targetImage.eulerAngles;
        finalEulerAngles.z = endAngle;
        targetImage.eulerAngles = finalEulerAngles;

        isProcessing = false;
    }
}
