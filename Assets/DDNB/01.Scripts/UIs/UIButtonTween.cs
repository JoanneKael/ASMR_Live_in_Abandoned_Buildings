using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모든 UI 버튼에 붙일 수 있는 공통 스케일 트윈.
/// 호버: 살짝 커짐 / 아웃: 원래 크기 / 클릭: 작아졌다가 복귀
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIButtonTween : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float clickScale = 0.92f;

    [Header("Timing")]
    [SerializeField] private float tweenDuration = 0.1f;
    [SerializeField] private float clickPunchDuration = 0.08f;

    private Vector3 originalScale;
    private Coroutine tweenRoutine;
    private bool isHovering;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnDisable()
    {
        if (tweenRoutine != null)
        {
            StopCoroutine(tweenRoutine);
            tweenRoutine = null;
        }

        // gameObject나 transform이 씬에서 완전 삭제되는 중이 아닐 때만 Scale 복구
        if (gameObject.activeSelf == false)
        {
            transform.localScale = originalScale;
        }

        isHovering = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        PlayScale(originalScale * hoverScale, tweenDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        PlayScale(originalScale, tweenDuration);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!gameObject.activeInHierarchy || !enabled) return;
        if (tweenRoutine != null) StopCoroutine(tweenRoutine);
        tweenRoutine = StartCoroutine(IE_ClickPunch());
    }

    private IEnumerator IE_ClickPunch()
    {
        yield return IE_ScaleTo(originalScale * clickScale, clickPunchDuration);
        Vector3 endScale = isHovering ? originalScale * hoverScale : originalScale;
        yield return IE_ScaleTo(endScale, tweenDuration);
        tweenRoutine = null;
    }

    private void PlayScale(Vector3 target, float duration)
    {
        if (tweenRoutine != null) StopCoroutine(tweenRoutine);
        tweenRoutine = StartCoroutine(IE_ScaleToAndClear(target, duration));
    }

    private IEnumerator IE_ScaleToAndClear(Vector3 target, float duration)
    {
        yield return IE_ScaleTo(target, duration);
        tweenRoutine = null;
    }

    private IEnumerator IE_ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = transform.localScale;
        if (duration <= 0f)
        {
            transform.localScale = target;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // smoothstep
            t = t * t * (3f - 2f * t);
            transform.localScale = Vector3.LerpUnclamped(start, target, t);
            yield return null;
        }

        transform.localScale = target;
    }
}
