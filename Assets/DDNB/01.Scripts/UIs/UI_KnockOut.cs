using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 넉아웃(눈 감기) UI — top/bottom 이미지 Height를 늘려 눈을 감는 연출.
/// Prefab: top pivot (0.5, 1), bottom pivot (0.5, 0) 기준.
/// Resources/PopupUI/UI_KnockOut Prefab에 topLid / bottomLid 할당.
/// </summary>
public class UI_KnockOut : UI_Base
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    [Header("Eyelids")]
    [SerializeField] private RectTransform topLid;
    [SerializeField] private RectTransform bottomLid;

    [Tooltip("감긴 상태 Height (열린 상태 = 0)")]
    [SerializeField] private float closedHeight = 600f;

    private void Awake()
    {
        ApplyRootLayout();
        SetLidsOpenImmediate();
    }

    private void ApplyRootLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root == null) return;

        bool isStretch = Mathf.Approximately(root.anchorMin.x, 0f)
            && Mathf.Approximately(root.anchorMin.y, 0f)
            && Mathf.Approximately(root.anchorMax.x, 1f)
            && Mathf.Approximately(root.anchorMax.y, 1f);

        if (!isStretch)
        {
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
            root.anchoredPosition = Vector2.zero;
        }
        else
        {
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }
    }

    public void SetLidsOpenImmediate()
    {
        SetLidHeight(topLid, 0f);
        SetLidHeight(bottomLid, 0f);
    }

    public void SetLidsClosedImmediate()
    {
        SetLidHeight(topLid, closedHeight);
        SetLidHeight(bottomLid, closedHeight);
    }

    /// <summary>눈꺼풀 Height 0 → closedHeight</summary>
    public async UniTask PlayCloseEyesAsync(float duration, CancellationToken token)
    {
        gameObject.SetActive(true);
        ApplyRootLayout();
        SetLidsOpenImmediate();
        await AnimateLidsAsync(open: false, duration, token);
    }

    /// <summary>눈꺼풀 Height closedHeight → 0</summary>
    public async UniTask PlayOpenEyesAsync(float duration, CancellationToken token)
    {
        gameObject.SetActive(true);
        ApplyRootLayout();
        SetLidsClosedImmediate();
        await AnimateLidsAsync(open: true, duration, token);
        CloseUI();
    }

    private async UniTask AnimateLidsAsync(bool open, float duration, CancellationToken token)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        float start = open ? closedHeight : 0f;
        float end = open ? 0f : closedHeight;

        SetLidHeight(topLid, start);
        SetLidHeight(bottomLid, start);

        while (elapsed < duration)
        {
            token.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            float height = Mathf.Lerp(start, end, t);

            SetLidHeight(topLid, height);
            SetLidHeight(bottomLid, height);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        SetLidHeight(topLid, end);
        SetLidHeight(bottomLid, end);
    }

    private static void SetLidHeight(RectTransform lid, float height)
    {
        if (lid == null) return;

        Vector2 size = lid.sizeDelta;
        size.y = height;
        lid.sizeDelta = size;
    }
}
