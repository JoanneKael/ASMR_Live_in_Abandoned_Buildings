using System;
using System.Collections;
using UnityEngine;

public class ASMRManager : MonoBehaviour
{
    public static ASMRManager Instance;

    [Header("ASMR")]
    private Coroutine asmrCoroutine;
    InteractableASMR _target;
    [SerializeField] private float fillDuration = 30f;

    [Header("Minigame")]
    private bool isMinigameStarted = false;
    [SerializeField] private float minigameProbability = 0.05f;

    [Header("UI")]
    private UI_ASMR ui_asmr;
    private UI_Minigame ui_minigame;

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

        asmrCoroutine = StartCoroutine(IE_ASMR(target));
    }

    public void EndASMR()
    {
        if (asmrCoroutine != null) StopCoroutine(asmrCoroutine);

        GameManager.Instance.Player.ChangePlayerState(PlayerState.Idle);

        if (ui_asmr != null) ui_asmr.CloseUI();
        if (ui_minigame != null) ui_minigame.CloseUI();

        _target = null;
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
                    }

                    time = 0f;
                }
            }

            if (MICManager.Instance.CheckASMR())
            {
                //Debug.Log("asmr 중");
                currentSliderValue = Mathf.MoveTowards(currentSliderValue, 1f, Time.deltaTime / fillDuration);
                ui_asmr.SetFillAmount(currentSliderValue);

                target.currentASMRProgress = currentSliderValue;
            }

            if (currentSliderValue >= 1f)
            {
                Debug.Log("ASMR 목표 달성!");
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

        // 입력이 들어왔을 때 실행할 로직
        System.Action handleInput = () =>
        {
            isInputReceived = true;
            // 타이밍 체크 로직: 현재 바늘이 정답 구역인지 확인
            isSuccess = ui_minigame.IsWithinTargetZone();
        };

        InputManager.Instance.OnASMRPerformed += handleInput;

        ui_minigame.ResetLine();

        float timer = 0f;
        while (timer < 1.5f && !isInputReceived)
        {
            ui_minigame.RotateLine();

            timer += Time.deltaTime;
            yield return null;
        }

        InputManager.Instance.OnASMRPerformed -= handleInput;

        ui_minigame.CloseUI();

        if (isInputReceived && isSuccess)
            Debug.Log("성공!");
        else
        {
            Debug.Log("실패/시간초과");

            MinigameFail();
        }

        isMinigameStarted = false;
    }

    private void MinigameFail()
    {
        // ASMR 게이지 패널티
        _target.currentASMRProgress = Mathf.Clamp(_target.currentASMRProgress - 0.1f, 0f, 1f);
        ui_asmr.SetFillAmount(_target.currentASMRProgress);

        // 사운드 이펙트


        // ASMR 중단
        EndASMR();

        // 소음 이벤트 (전역 쿨다운 무시)
        if (NoiseManager.Instance != null && GameManager.Instance.Player != null)
        {
            NoiseManager.Instance.ReportNoise(
                NoiseSource.ASMRFail,
                GameManager.Instance.Player.transform.position,
                bypassCooldown: true);
        }
    }
}