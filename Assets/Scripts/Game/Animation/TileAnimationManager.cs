using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileAnimationManager : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float scaleDownDuration = 0.15f;
    [SerializeField] private float scaleUpDuration = 0.15f;
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float cascadeDelay = 0.02f;
    [SerializeField] private float maxCascadeDelay = 0.1f;
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

        // Perform action when scale down is complete (change sprite)
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

        // Animation complete
        onAnimationComplete?.Invoke();
    }

    private IEnumerator TileSequenceAnimation(List<Transform> tileTransforms, List<System.Action> onScaleDownComplete, System.Action onAllComplete)
    {
        List<Coroutine> runningAnimations = new List<Coroutine>();

        // Calculate dynamic delay based on distance from first tile
        if (tileTransforms.Count > 0)
        {
            Vector3 firstTilePos = tileTransforms[0].position;

            // Start all animations with calculated delays
            for (int i = 0; i < tileTransforms.Count; i++)
            {
                Transform tile = tileTransforms[i];
                float distance = Vector3.Distance(firstTilePos, tile.position);

                // Dynamic delay based on distance (clamped to maxCascadeDelay)
                float delay = Mathf.Min(distance * cascadeDelay, maxCascadeDelay);

                int index = i; // Capture the correct index for the lambda
                yield return new WaitForSeconds(delay);

                runningAnimations.Add(StartCoroutine(TileRevealAnimation(
                    tile,
                    () => { if (index < onScaleDownComplete.Count) onScaleDownComplete[index]?.Invoke(); },
                    null
                )));
            }
        }

        // Wait for all animations to complete
        foreach (var anim in runningAnimations)
        {
            yield return anim;
        }

        onAllComplete?.Invoke();
    }
}