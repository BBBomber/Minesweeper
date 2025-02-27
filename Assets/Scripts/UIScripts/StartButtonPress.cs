using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class StartButtonPress : MonoBehaviour
{
    public Outline targetOutline;
    public float dynamicTime = 2f;
    private bool isProcessing = false;

    public void StartProcess()
    {
        if (!isProcessing && targetOutline != null)
        {
            StartCoroutine(ReduceOutlineDistance());
        }
    }

    private IEnumerator ReduceOutlineDistance()
    {
        isProcessing = true;
        Vector2 startDistance = targetOutline.effectDistance;
        float elapsedTime = 0f;

        while (elapsedTime < dynamicTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / dynamicTime);

            // Quadratic Ease-Out: 1 - (1 - t)^2
            float easeOutT = 1f - Mathf.Pow(1f - t, 2f);

            float newX = Mathf.Lerp(startDistance.x, 520f, easeOutT);
            float newY = Mathf.Lerp(startDistance.y, -64f, easeOutT);

            targetOutline.effectDistance = new Vector2(newX, newY);

            yield return null;
        }


        //targetOutline.effectDistance = Vector2.zero;
        OnBothValuesZero();
        isProcessing = false;
    }

    private void OnBothValuesZero()
    {
        GameManager.Instance.LoadScene("GameScene");
    }
}
