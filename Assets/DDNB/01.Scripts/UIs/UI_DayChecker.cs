using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 밤 시작 토스트 (~ 첫 번째 밤 ~ 등). Resources/PopupUI/UI_DayChecker
/// 표시 직후 인게임 시계·입력을 멈추고, unlockDelay 초 뒤 해제합니다.
/// </summary>
public class UI_DayChecker : UI_Base
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Toast Timing")]
    [SerializeField] private float fadeInDuration = 0.45f;
    [SerializeField] private float holdDuration = 1.6f;
    [SerializeField] private float fadeOutDuration = 0.45f;

    [Header("Gameplay Freeze")]
    [Tooltip("토스트 등장 후 시계·입력을 다시 푸는 대기 시간(실시간 초)")]
    [SerializeField] private float unlockDelay = 1f;

    private Coroutine toastRoutine;
    private Coroutine unlockRoutine;
    private bool isGameplayFrozen;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void OnDisable()
    {
        if (toastRoutine != null)
        {
            StopCoroutine(toastRoutine);
            toastRoutine = null;
        }

        if (unlockRoutine != null)
        {
            StopCoroutine(unlockRoutine);
            unlockRoutine = null;
        }

        // 토스트가 중간에 꺼져도 입력·시계가 잠긴 채로 남지 않도록
        EndGameplayFreeze();
    }

    /// <summary>
    /// remainingDays / maxDays 기준으로 밤 문구 표시 후 자동 비활성.
    /// </summary>
    public void ShowNightToast(int remainingDays, int maxDays)
    {
        int nightIndex = Mathf.Clamp(maxDays - remainingDays + 1, 1, maxDays);
        string message = GetNightMessage(nightIndex, maxDays);

        if (messageText != null)
            messageText.text = message;

        gameObject.SetActive(true);

        BeginGameplayFreeze();

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        if (unlockRoutine != null) StopCoroutine(unlockRoutine);

        toastRoutine = StartCoroutine(IE_Toast());
        unlockRoutine = StartCoroutine(IE_UnlockAfterDelay());
    }

    private static string GetNightMessage(int nightIndex, int maxDays)
    {
        if (nightIndex <= 1) return "~ 첫 번째 밤 ~";
        if (nightIndex >= maxDays) return "~ 마지막 밤 ~";
        if (nightIndex == 2) return "~ 두 번째 밤 ~";
        return $"~ {nightIndex}번째 밤 ~";
    }

    private void BeginGameplayFreeze()
    {
        isGameplayFrozen = true;

        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputBlocked(true);

        if (GameManager.Instance != null)
            GameManager.Instance.PauseIngameClock();
    }

    private void EndGameplayFreeze()
    {
        if (!isGameplayFrozen) return;
        isGameplayFrozen = false;

        if (GameManager.Instance != null)
            GameManager.Instance.ResumeIngameClock();

        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputBlocked(false);
    }

    private IEnumerator IE_UnlockAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, unlockDelay));
        EndGameplayFreeze();
        unlockRoutine = null;
    }

    private IEnumerator IE_Toast()
    {
        yield return FadeCanvas(0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return FadeCanvas(1f, 0f, fadeOutDuration);
        toastRoutine = null;
        CloseUI();
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
