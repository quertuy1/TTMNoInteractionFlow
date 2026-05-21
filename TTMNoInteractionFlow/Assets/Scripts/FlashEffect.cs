using UnityEngine;

public class FlashEffect : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup canvasGroup;

    [Header("Timing")]
    public float flashInTime = 0.05f;
    public float flashHoldTime = 0.03f;
    public float flashOutTime = 0.12f;

    private Coroutine flashRoutine;

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public void TriggerFlash()
    {
        if (canvasGroup == null) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashSequence());
    }

    private System.Collections.IEnumerator FlashSequence()
    {
        yield return Fade(0f, 1f, flashInTime);
        yield return new WaitForSeconds(flashHoldTime);
        yield return Fade(1f, 0f, flashOutTime);
        flashRoutine = null;
    }

    private System.Collections.IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
