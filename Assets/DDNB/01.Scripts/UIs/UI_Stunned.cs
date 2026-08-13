using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기절 연출용 비네팅 UI (Popup / Resources/PopupUI/UI_Stunned).
/// vignetteImage는 Stretch 또는 1920x1080 기준 RectTransform.
/// </summary>
public class UI_Stunned : UI_Base
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    [SerializeField] private Image vignetteImage;
    [SerializeField] private RectTransform vignetteRect;

    private Texture2D vignetteTexture;
    private Sprite vignetteSprite;

    private void Awake()
    {
        if (vignetteImage == null)
            vignetteImage = GetComponentInChildren<Image>();
        if (vignetteRect == null && vignetteImage != null)
            vignetteRect = vignetteImage.rectTransform;

        ApplyFullScreenRect();
        EnsureVignetteSprite();
    }

    private void OnDestroy()
    {
        if (vignetteSprite != null) Destroy(vignetteSprite);
        if (vignetteTexture != null) Destroy(vignetteTexture);
    }

    private void ApplyFullScreenRect()
    {
        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            // 루트가 stretch가 아니면 1920x1080 앵커 중앙 기준으로 맞춤
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

        if (vignetteRect != null)
        {
            vignetteRect.anchorMin = Vector2.zero;
            vignetteRect.anchorMax = Vector2.one;
            vignetteRect.offsetMin = Vector2.zero;
            vignetteRect.offsetMax = Vector2.zero;
            vignetteRect.localScale = Vector3.one;
        }
    }

    private void EnsureVignetteSprite()
    {
        if (vignetteImage == null) return;
        if (vignetteSprite != null)
        {
            vignetteImage.sprite = vignetteSprite;
            return;
        }

        const int size = 256;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        vignetteTexture.wrapMode = TextureWrapMode.Clamp;
        vignetteTexture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDist = center.magnitude;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float a = Mathf.SmoothStep(0.15f, 1f, dist);
                vignetteTexture.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
        }

        vignetteTexture.Apply(false, true);
        vignetteSprite = Sprite.Create(
            vignetteTexture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f);

        vignetteImage.sprite = vignetteSprite;
        vignetteImage.type = Image.Type.Simple;
        vignetteImage.preserveAspect = false;
        vignetteImage.raycastTarget = false;
        vignetteImage.color = new Color(1f, 1f, 1f, 0f);
    }

    /// <summary>가장자리 → 중심으로 암전</summary>
    public async UniTask PlayCloseInAsync(float duration, CancellationToken token)
    {
        gameObject.SetActive(true);
        ApplyFullScreenRect();
        EnsureVignetteSprite();
        await AnimateIntensityAsync(0f, 1f, duration, token);
    }

    /// <summary>중심 → 바깥으로 시야 회복</summary>
    public async UniTask PlayOpenOutAsync(float duration, CancellationToken token)
    {
        gameObject.SetActive(true);
        EnsureVignetteSprite();
        if (vignetteImage != null && vignetteSprite != null)
            vignetteImage.sprite = vignetteSprite;
        await AnimateIntensityAsync(1f, 0f, duration, token);
        CloseUI();
    }

    private async UniTask AnimateIntensityAsync(float from, float to, float duration, CancellationToken token)
    {
        if (vignetteImage == null) return;

        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            token.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetIntensity(Mathf.Lerp(from, to, t));
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        SetIntensity(to);
    }

    private void SetIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);

        if (intensity >= 0.999f)
        {
            vignetteImage.sprite = null;
            vignetteImage.color = Color.black;
            return;
        }

        if (vignetteSprite != null)
            vignetteImage.sprite = vignetteSprite;

        Color c = Color.Lerp(Color.white, new Color(0.15f, 0.15f, 0.15f, 1f), intensity);
        c.a = intensity;
        vignetteImage.color = c;
    }
}
