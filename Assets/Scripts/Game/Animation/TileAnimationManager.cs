using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileAnimationManager : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float scaleDownDuration = 0.09f;
    [SerializeField] private float scaleUpDuration = 0.09f;
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float cascadeDelay = 0.008f;
    [SerializeField] private float maxCascadeDelay = 0.05f;
    [SerializeField] private AnimationCurve scaleDownCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private AnimationCurve scaleUpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private static TileAnimationManager _instance;
    public static TileAnimationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<TileAnimationManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("TileAnimationManager");
                    _instance = obj.AddComponent<TileAnimationManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Animate a single tile
    public void AnimateTileReveal(Transform tileTransform, System.Action onScaleDownComplete, System.Action onAnimationComplete = null)
    {
        StartCoroutine(TileRevealAnimation(tileTransform, onScaleDownComplete, onAnimationComplete));
    }

    // Animates a sequence of tiles with cascading delay
    public void AnimateTileSequence(List<Transform> tileTransforms, List<System.Action> onScaleDownComplete, System.Action onAllComplete = null)
    {
        StartCoroutine(TileSequenceAnimation(tileTransforms, onScaleDownComplete, onAllComplete));
    }

    private IEnumerator TileRevealAnimation(Transform tileTransform, System.Action onScaleDownComplete, System.Action onAnimationComplete)
    {
        Vector3 originalScale = tileTransform.localScale;
        Vector3 minScaleVec = originalScale * minScale;

        // Scale down
        float elapsed = 0f;
        while (elapsed < scaleDownDuration)
        {
            float t = elapsed / scaleDownDuration;
            float curveValue = scaleDownCurve.Evaluate(t);
            tileTransform.localScale = Vector3.Lerp(originalScale, minScaleVec, curveValue);
            elapsed += Time.deltaTime;
            yield return null;
        }
        tileTransform.localScale = minScaleVec;

        onScaleDownComplete?.Invoke();

        // Scale up
        elapsed = 0f;
        while (elapsed < scaleUpDuration)
        {
            float t = elapsed / scaleUpDuration;
            float curveValue = scaleUpCurve.Evaluate(t);
            tileTransform.localScale = Vector3.Lerp(minScaleVec, originalScale, curveValue);
            elapsed += Time.deltaTime;
            yield return null;
        }
        tileTransform.localScale = originalScale;

        onAnimationComplete?.Invoke();
    }

    private IEnumerator TileSequenceAnimation(List<Transform> tileTransforms, List<System.Action> onScaleDownComplete, System.Action onAllComplete)
    {
        if (tileTransforms.Count == 0)
        {
            onAllComplete?.Invoke();
            yield break;
        }

        Vector3 firstTilePos = tileTransforms[0].position;
        float maxDelay = 0f;

        // Launch every tile immediately as its own delayed coroutine
        for (int i = 0; i < tileTransforms.Count; i++)
        {
            float distance = Vector3.Distance(firstTilePos, tileTransforms[i].position);
            float delay = Mathf.Min(distance * cascadeDelay, maxCascadeDelay);
            if (delay > maxDelay) maxDelay = delay;

            int index = i;
            StartCoroutine(DelayedTileReveal(
                delay,
                tileTransforms[index],
                () => { if (index < onScaleDownComplete.Count) onScaleDownComplete[index]?.Invoke(); }
            ));
        }

        // Wait for all tiles (furthest delay + full animation duration)
        yield return new WaitForSeconds(maxDelay + scaleDownDuration + scaleUpDuration);

        onAllComplete?.Invoke();
    }

    private IEnumerator DelayedTileReveal(float delay, Transform tileTransform, System.Action onScaleDownComplete)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        yield return StartCoroutine(TileRevealAnimation(tileTransform, onScaleDownComplete, null));
    }
}