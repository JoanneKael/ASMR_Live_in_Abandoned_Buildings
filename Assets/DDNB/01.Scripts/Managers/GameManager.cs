using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player")]
    public Player Player { get; private set; }

    [Header("Refs")]
    [SerializeField] private int stage;         // 게임 스테이지
    [SerializeField] private int gameTime;      // 게임 시간
    [SerializeField] private int missionGold;   // 목표 금액
    [SerializeField] private int currentGold;   // 현재 금액

    [Header("ASMR")]
    private Coroutine asmrCoroutine;
    InteractableObject _target;
    [SerializeField] private float fillDuration = 30f;

    [Header("Minigame")]
    private bool isMinigameStarted = false;
    [SerializeField] private float minigameProbability = 0.05f;

    private void Awake()
    {
        Instance = this;
        Player = GameObject.FindWithTag("Player").GetComponent<Player>();
    }

    private void Start()
    {
        if (MICManager.Instance != null)
            MICManager.Instance.OnNoiseDetected += OnNoiseDetected;
    }

    private void OnNoiseDetected()
    {
        Debug.LogError("소음이 감지되었습니다!!!!");
    }

    private void OnDisable()
    {
        if (MICManager.Instance != null)
            MICManager.Instance.OnNoiseDetected -= OnNoiseDetected;
    }

    public void StartASMR(InteractableObject target)
    {
        if (target.isASMRCompleted)
        {
            Debug.Log("이미 완료된 ASMR입니다.");
            return;
        }

        Player.ChangePlayerState(PlayerState.ASMR);
        UIManager.Instance.ShowUI<UI_ASMR>(true);

        asmrCoroutine = StartCoroutine(IE_ASMR(target));
    }

    public void EndASMR()
    {
        if (asmrCoroutine != null) StopCoroutine(asmrCoroutine);

        Player.ChangePlayerState(PlayerState.Idle);
        UIManager.Instance.ShowUI<UI_ASMR>(false);
        UIManager.Instance.ShowUI<UI_Minigame>(false);
        _target = null;
    }

    public void SuccessASMR()
    {
        currentGold++;
        CheckMissionGold();
    }

    private void CheckMissionGold()
    {
        if (currentGold >= missionGold) Debug.Log("미션 금액 달성!! 탈출하세요");
    }

    private IEnumerator IE_ASMR(InteractableObject target)
    {
        _target = target;

        float currentSliderValue = target.currentASMRProgress;
        UIManager.Instance.UI_ASMR.SetFillAmount(currentSliderValue);

        float time = 0f;
        float checkInterval = 5f;
        int currentAttempts = 0;
        int maxAttempts = 5;

        while (Player.CurrentState == PlayerState.ASMR)
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
                UIManager.Instance.UI_ASMR.SetFillAmount(currentSliderValue);

                target.currentASMRProgress = currentSliderValue;
            }

            if (currentSliderValue >= 1f)
            {
                Debug.Log("ASMR 목표 달성!");
                target.isASMRCompleted = true;
                EndASMR();
                SuccessASMR();
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator IE_Minigame()
    {
        UIManager.Instance.ShowUI<UI_Minigame>(true);
        UIManager.Instance.UI_Minigame.SetBarRect();

        bool isInputReceived = false;
        bool isSuccess = false;

        // 입력이 들어왔을 때 실행할 로직
        System.Action handleInput = () =>
        {
            isInputReceived = true;
            // 타이밍 체크 로직: 현재 바늘이 정답 구역인지 확인
            isSuccess = UIManager.Instance.UI_Minigame.IsWithinTargetZone();
        };

        // 1. 이벤트 구독
        InputManager.Instance.OnASMRPerformed += handleInput;

        // 2. 입력 대기
        UIManager.Instance.UI_Minigame.ResetLine();

        float timer = 0f;
        while (timer < 1.5f && !isInputReceived)
        {
            UIManager.Instance.UI_Minigame.RotateLine();

            timer += Time.deltaTime;
            yield return null;
        }

        // 3. 이벤트 구독 해제 (필수!)
        InputManager.Instance.OnASMRPerformed -= handleInput;

        // 4. 결과 처리
        UIManager.Instance.ShowUI<UI_Minigame>(false);

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
        _target.currentASMRProgress = Mathf.Clamp(_target.currentASMRProgress - 0.1f, 0f, 1f);
        UIManager.Instance.UI_ASMR.SetFillAmount(_target.currentASMRProgress);
        
        EndASMR();
        OnNoiseDetected();
        MICManager.Instance.RefreshDecibelUI(0f);
    }
}