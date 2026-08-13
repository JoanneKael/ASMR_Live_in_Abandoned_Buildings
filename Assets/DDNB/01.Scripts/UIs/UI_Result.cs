using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 성공/실패 결과 UI.
/// 타이틀·서브텍스트·로비 버튼을 공유하며, 활성화 후 순차 등장 연출을 재생합니다.
/// </summary>
public class UI_Result : UI_Base
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI[] subtitleLines; // 3줄

    [Header("Lobby Button")]
    [SerializeField] private Button lobbyButton;
    [SerializeField] private CanvasGroup lobbyButtonCanvasGroup;

    [Header("Animation")]
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float lineInterval = 0.5f;
    [SerializeField] private float buttonDelayAfterLastLine = 1f;
    [SerializeField] private float fadeDuration = 0.45f;
    [SerializeField] private float slideOffsetY = 24f;

    private static readonly Color SuccessTitleColor = new Color32(0x78, 0x78, 0x78, 0xFF);
    private static readonly Color FailTitleColor = new Color32(0xB4, 0x00, 0x00, 0xFF);

    private static readonly string[] SuccessSubtitles =
    {
        "당신의 라이브는 성공적으로 끝났습니다.",
        "시청자들은 당신이 더 많은 폐건물을 방문하기를 원합니다.",
        "로비로 이동하여 다음 방송을 준비해주세요."
    };

    private static readonly string[] FailSubtitles =
    {
        "당신의 방송은 신고 누적으로 종료되었습니다.",
        "당신의 계정과 그날의 영상 모두 삭제되었습니다.",
        "당신은 실종되었으나, 아무도 당신을 기억하지 못합니다."
    };

    private Coroutine sequenceRoutine;
    private Coroutine goLobbyRoutine;
    private Vector2 titleBasePos;
    private Vector2[] subtitleBasePositions;
    private bool basesCached;

    private void Awake()
    {
        CacheBasePositions();
        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(OnClickLobby);
    }

    private void OnDestroy()
    {
        if (lobbyButton != null)
            lobbyButton.onClick.RemoveListener(OnClickLobby);
    }

    /// <summary>
    /// 결과 화면 설정. true = 성공, false = 실패.
    /// 엔딩 연출을 처음부터 재생합니다. (맨 앞 정렬은 ShowUI에서 처리)
    /// </summary>
    public void SettingResultUI(bool success)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ApplyContent(success);
        HideAllImmediate();

        if (sequenceRoutine != null) StopCoroutine(sequenceRoutine);
        sequenceRoutine = StartCoroutine(IE_PlayIntroSequence());
    }

    private void ApplyContent(bool success)
    {
        if (titleText != null)
        {
            titleText.text = success ? "당신은 살아남았습니다." : "방송이 종료되었습니다.";
            titleText.color = success ? SuccessTitleColor : FailTitleColor;
        }

        string[] lines = success ? SuccessSubtitles : FailSubtitles;
        if (subtitleLines == null) return;

        for (int i = 0; i < subtitleLines.Length; i++)
        {
            if (subtitleLines[i] == null) continue;
            subtitleLines[i].text = i < lines.Length ? lines[i] : string.Empty;
            // 서브 텍스트는 흰색 유지하되 알파만 애니메이션
            Color c = subtitleLines[i].color;
            c.a = 1f;
            subtitleLines[i].color = c;
        }
    }

    private void HideAllImmediate()
    {
        CacheBasePositions();

        SetTextHidden(titleText, titleBasePos);

        if (subtitleLines != null)
        {
            for (int i = 0; i < subtitleLines.Length; i++)
            {
                Vector2 basePos = subtitleBasePositions != null && i < subtitleBasePositions.Length
                    ? subtitleBasePositions[i]
                    : Vector2.zero;
                SetTextHidden(subtitleLines[i], basePos);
            }
        }

        if (lobbyButtonCanvasGroup != null)
        {
            lobbyButtonCanvasGroup.alpha = 0f;
            lobbyButtonCanvasGroup.interactable = false;
            lobbyButtonCanvasGroup.blocksRaycasts = false;
        }

        if (lobbyButton != null)
            lobbyButton.gameObject.SetActive(true);
    }

    private void CacheBasePositions()
    {
        if (basesCached) return;

        if (titleText != null)
            titleBasePos = titleText.rectTransform.anchoredPosition;

        if (subtitleLines != null)
        {
            subtitleBasePositions = new Vector2[subtitleLines.Length];
            for (int i = 0; i < subtitleLines.Length; i++)
            {
                if (subtitleLines[i] != null)
                    subtitleBasePositions[i] = subtitleLines[i].rectTransform.anchoredPosition;
            }
        }

        basesCached = true;
    }

    private static void SetTextHidden(TextMeshProUGUI text, Vector2 basePos)
    {
        if (text == null) return;

        Color c = text.color;
        c.a = 0f;
        text.color = c;

        text.rectTransform.anchoredPosition = basePos + Vector2.down * 24f;
    }

    private IEnumerator IE_PlayIntroSequence()
    {
        // Time.timeScale = 0 대비 unscaled 대기
        yield return new WaitForSecondsRealtime(initialDelay);

        // 타이틀
        yield return IE_FadeSlideIn(titleText, titleBasePos, fadeDuration);

        // 서브 텍스트 한 줄씩
        if (subtitleLines != null)
        {
            for (int i = 0; i < subtitleLines.Length; i++)
            {
                if (subtitleLines[i] == null) continue;
                if (string.IsNullOrEmpty(subtitleLines[i].text)) continue;

                yield return new WaitForSecondsRealtime(lineInterval);

                Vector2 basePos = subtitleBasePositions[i];
                yield return IE_FadeSlideIn(subtitleLines[i], basePos, fadeDuration);
            }
        }

        // 마지막 줄 이후 1초 → 버튼 페이드인
        yield return new WaitForSecondsRealtime(buttonDelayAfterLastLine);
        yield return IE_FadeCanvasGroup(lobbyButtonCanvasGroup, 0f, 1f, fadeDuration);

        if (lobbyButtonCanvasGroup != null)
        {
            lobbyButtonCanvasGroup.interactable = true;
            lobbyButtonCanvasGroup.blocksRaycasts = true;
        }

        sequenceRoutine = null;
    }

    private IEnumerator IE_FadeSlideIn(TextMeshProUGUI text, Vector2 basePos, float duration)
    {
        if (text == null) yield break;

        RectTransform rt = text.rectTransform;
        Vector2 startPos = basePos + Vector2.down * slideOffsetY;
        rt.anchoredPosition = startPos;

        Color c = text.color;
        c.a = 0f;
        text.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t); // smoothstep

            c.a = t;
            text.color = c;
            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, basePos, t);
            yield return null;
        }

        c.a = 1f;
        text.color = c;
        rt.anchoredPosition = basePos;
    }

    private IEnumerator IE_FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        group.alpha = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            group.alpha = Mathf.LerpUnclamped(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    private void OnClickLobby()
    {
        if (goLobbyRoutine != null) return;

        // 연출 중이면 버튼이 아직 비활성인 게 정상 — 그래도 대기 후 이동
        if (lobbyButton != null)
            lobbyButton.interactable = false;
        if (lobbyButtonCanvasGroup != null)
            lobbyButtonCanvasGroup.interactable = false;

        goLobbyRoutine = StartCoroutine(IE_GoToLobbyAfterPresentation());
    }

    /// <summary>
    /// 결과 연출이 끝난 뒤 Result·KnockOut을 닫고 로비로 이동합니다.
    /// </summary>
    private IEnumerator IE_GoToLobbyAfterPresentation()
    {
        // 인트로 연출이 끝날 때까지 대기 (버튼은 보통 연출 후에만 눌리지만 안전장치)
        while (sequenceRoutine != null)
            yield return null;

        Time.timeScale = 1f;

        // 게임오버 넉아웃에서 눈 감은 채 남은 UI 정리
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.IsUIActive<UI_KnockOut>())
                UIManager.Instance.HideUI<UI_KnockOut>();
            if (UIManager.Instance.IsUIActive<UI_Stunned>())
                UIManager.Instance.HideUI<UI_Stunned>();
        }

        CloseUI();
        yield return null;

        goLobbyRoutine = null;
        if (NewSceneManager.Instance != null)
            NewSceneManager.Instance.GoToLobby();
    }
}
