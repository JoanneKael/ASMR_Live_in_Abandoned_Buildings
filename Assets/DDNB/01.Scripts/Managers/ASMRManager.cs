using System.Collections;
using UnityEngine;

public class ASMRManager : MonoBehaviour
{
    public static ASMRManager Instance;

    [Header("ASMR")]
    private Coroutine asmrCoroutine;
    private InteractableASMR _target;
    [SerializeField] private float fillDuration = 30f;

    [Header("Minigame")]
    private bool isMinigameStarted = false;
    [SerializeField] private float minigameProbability = 0.05f;
    [SerializeField] private float minigameSuccessBonusMin = 4f;
    [SerializeField] private float minigameSuccessBonusMax = 5f;

    [Header("UI")]
    private UI_ASMR ui_asmr;
    private UI_Minigame ui_minigame;
    private System.Action minigameInputHandler;

    private void Awake()
    {
        Instance = this;
    }

    public void StartASMR(InteractableASMR target)
    {
        if (target.isASMRCompleted)
        {
            Debug.Log("이미 완료된 ASMR입니다.");
            return;
        }

        GameManager.Instance.Player.ChangePlayerState(PlayerState.ASMR);

        ui_asmr = UIManager.Instance.ShowUI<UI_ASMR>();

        StartAsmrSound(target);
        asmrCoroutine = StartCoroutine(IE_ASMR(target));
    }

    /// <summary>유닛 피격 등으로 ASMR 강제 중단</summary>
    public void InterruptByCombat()
    {
        if (asmrCoroutine == null && !isMinigameStarted && ui_asmr == null && ui_minigame == null)
        {
            ForceCloseAsmrUIs();
            return;
        }

        EndASMR();
    }

    public void EndASMR()
    {
        ClearMinigameInputHandler();

        if (asmrCoroutine != null)
        {
            StopCoroutine(asmrCoroutine);
            asmrCoroutine = null;
        }

        if (GameManager.Instance != null
            && GameManager.Instance.Player != null
            && GameManager.Instance.Player.CurrentState == PlayerState.ASMR)
        {
            GameManager.Instance.Player.ChangePlayerState(PlayerState.Idle);
        }

        ForceCloseAsmrUIs();
        isMinigameStarted = false;
        _target = null;
        StopAsmrSound();
    }

    private void ForceCloseAsmrUIs()
    {
        if (ui_asmr != null)
        {
            ui_asmr.CloseUI();
            ui_asmr = null;
        }

        if (ui_minigame != null)
        {
            ui_minigame.CloseUI();
            ui_minigame = null;
        }

        // 참조가 비어도 하이라키/캐시에 남아 있으면 HideUI로 정리
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.IsUIActive<UI_ASMR>())
                UIManager.Instance.HideUI<UI_ASMR>();
            if (UIManager.Instance.IsUIActive<UI_Minigame>())
                UIManager.Instance.HideUI<UI_Minigame>();
        }
    }

    private void ClearMinigameInputHandler()
    {
        if (minigameInputHandler == null) return;
        if (InputManager.Instance != null)
            InputManager.Instance.OnASMRPerformed -= minigameInputHandler;
        minigameInputHandler = null;
    }

    private void StartAsmrSound(InteractableASMR target)
    {
        if (SoundManager.Instance == null) return;

        string category = SoundManager.ResolveAsmrCategory(target);
        if (string.IsNullOrEmpty(category))
        {
            Debug.LogWarning($"[ASMRManager] ASMR SFX 카테고리를 찾지 못했습니다: {target?.name}");
            return;
        }

        SoundManager.Instance.PlayAsmrLoop(category);
    }

    private void StopAsmrSound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopAsmrLoop();
    }

    private IEnumerator IE_ASMR(InteractableASMR target)
    {
        _target = target;

        float currentSliderValue = target.currentASMRProgress;
        ui_asmr.SetFillAmount(currentSliderValue);

        float time = 0f;
        float checkInterval = 4f;
        int currentAttempts = 0;
        int maxAttempts = 5;

        while (GameManager.Instance.Player.CurrentState == PlayerState.ASMR)
        {
            if (currentAttempts < maxAttempts)
            {
                time += Time.deltaTime;

                if (time >= checkInterval)
                {
                    if (UnityEngine.Random.value < minigameProbability && !isMinigameStarted)
                    {
                        Debug.LogWarning("미니게임@@ 준비하세요!!!!");
                        currentAttempts++;
                        isMinigameStarted = true;
                        yield return new WaitForSeconds(0.15f);
                        yield return StartCoroutine(IE_Minigame());

                        // 미니게임 보상/패널티가 target에 반영됐을 수 있으므로 동기화
                        currentSliderValue = target.currentASMRProgress;
                        if (ui_asmr != null)
                            ui_asmr.SetFillAmount(currentSliderValue);
                    }

                    time = 0f;
                }
            }

            if (MICManager.Instance.CheckASMR())
            {
                currentSliderValue = Mathf.MoveTowards(currentSliderValue, 1f, Time.deltaTime / fillDuration);
                ui_asmr.SetFillAmount(currentSliderValue);

                target.currentASMRProgress = currentSliderValue;
            }

            if (currentSliderValue >= 1f)
            {
                target.isASMRCompleted = true;
                EndASMR();
                GameManager.Instance.SuccessASMR();
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator IE_Minigame()
    {
        ui_minigame = UIManager.Instance.ShowUI<UI_Minigame>();
        ui_minigame.SetBarRect();

        bool isInputReceived = false;
        bool isSuccess = false;

        minigameInputHandler = () =>
        {
            isInputReceived = true;
            isSuccess = ui_minigame != null && ui_minigame.IsWithinTargetZone();
        };

        InputManager.Instance.OnASMRPerformed += minigameInputHandler;

        ui_minigame.ResetLine();

        float timer = 0f;
        while (timer < 1.5f && !isInputReceived)
        {
            if (ui_minigame != null)
                ui_minigame.RotateLine();

            timer += Time.deltaTime;
            yield return null;
        }

        ClearMinigameInputHandler();

        if (ui_minigame != null)
        {
            ui_minigame.CloseUI();
            ui_minigame = null;
        }

        if (isInputReceived && isSuccess)
            ApplyMinigameSuccessBonus();
        else
        {
            Debug.Log("실패/시간초과");
            MinigameFail();
        }

        isMinigameStarted = false;
    }

    /// <summary>성공 시 fillDuration 기준 4~5초분 진행도를 보상</summary>
    private void ApplyMinigameSuccessBonus()
    {
        if (_target == null) return;

        float bonusSeconds = Random.Range(minigameSuccessBonusMin, minigameSuccessBonusMax);
        float bonusProgress = bonusSeconds / Mathf.Max(0.01f, fillDuration);

        _target.currentASMRProgress = Mathf.Clamp01(_target.currentASMRProgress + bonusProgress);

        if (ui_asmr != null)
            ui_asmr.SetFillAmount(_target.currentASMRProgress);

        Debug.Log($"미니게임 성공! +{bonusSeconds:F1}초 분량 진행 ({bonusProgress:P0})");
    }

    private void MinigameFail()
    {
        if (_target != null && ui_asmr != null)
        {
            _target.currentASMRProgress = Mathf.Clamp(_target.currentASMRProgress - 0.1f, 0f, 1f);
            ui_asmr.SetFillAmount(_target.currentASMRProgress);
        }

        EndASMR();

        if (NoiseManager.Instance != null && GameManager.Instance.Player != null)
        {
            NoiseManager.Instance.ReportNoise(
                NoiseSource.ASMRFail,
                GameManager.Instance.Player.transform.position,
                bypassCooldown: true);
        }
    }
}
