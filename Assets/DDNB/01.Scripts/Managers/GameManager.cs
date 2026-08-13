using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player")]
    public Player Player { get; private set; }
    [SerializeField] private Transform playerSpawn;

    public bool AreYouReady { get; private set; }
    public bool AllMissionCompleted { get; private set; }
    public bool IsGameOver { get; private set; }

    [Header("Day System")]
    [SerializeField] private int maxDays = 3;
    public int RemainingDays { get; private set; }

    [Header("Current Stage Info")]
    public StageData CurrentStageData { get; private set; }
    public GameDifficulty CurrentDifficulty { get; private set; }

    [SerializeField] private int missionGold;
    [SerializeField] private int currentGold;
    [SerializeField] private int maxStunCount;
    [SerializeField] private int currentStunCount;

    [Header("In-Game Clock")]
    [Tooltip("실제 시간(초). 기본 5분 = 300초")]
    [SerializeField] private float realDurationSeconds = 300f;
    [Tooltip("대응 게임 시간(시간). 기본 6시간")]
    [SerializeField] private float gameDurationHours = 6f;
    [Tooltip("UI 갱신 간격(게임 분)")]
    [SerializeField] private int displayStepGameMinutes = 10;

    public int DisplayedGameMinutes { get; private set; }
    public string IngameClockDisplay { get; private set; } = "00:00";

    public event Action<string> OnIngameClockChanged;
    public event Action OnIngameTimeEnded;
    public event Action<int> OnRemainingDaysChanged;

    private float elapsedRealSeconds;
    private bool isClockRunning;
    private bool hasClockEnded;
    private bool isResolvingDay; // 하루 종료 처리 중 중복 방지

    private void Awake()
    {
        Instance = this;

        if (GetComponent<NoiseManager>() == null && NoiseManager.Instance == null)
            gameObject.AddComponent<NoiseManager>();
    }

    private void OnEnable()
    {
        OnIngameTimeEnded += HandleDayTimeEnded;
        GameEvents.OnUnitAttackSucceeded += HandleUnitAttackSucceeded;
    }

    private void OnDisable()
    {
        OnIngameTimeEnded -= HandleDayTimeEnded;
        GameEvents.OnUnitAttackSucceeded -= HandleUnitAttackSucceeded;
    }

    private void Update()
    {
        UpdateIngameClock();
    }

    #region Init

    public void Init()
    {
        // 타이틀은 UI 클릭용 커서 유지. 그 외(로비/인게임)는 FPS용 잠금
        if (IsTitleScene())
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Player == null) GetPlayer();
        if (IsTitleScene()) return;

        // 로비: 맵 선택 전 상태로 리셋 후 퀘스트 갱신
        if (NewSceneManager.Instance.IsCurrentSceneLobby())
        {
            AreYouReady = false;
            AllMissionCompleted = false;
            GameEvents.RaiseQuestObjectiveChanged();
            return;
        }

        IsGameOver = false;
        isResolvingDay = false;
        RemainingDays = maxDays;
        currentGold = 0;
        currentStunCount = 0;
        AreYouReady = false;
        AllMissionCompleted = false;

        SetMissionGold();
        OnRemainingDaysChanged?.Invoke(RemainingDays);
        StartNewDay();
        GameEvents.RaiseQuestObjectiveChanged();
    }

    private static bool IsTitleScene()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Title";
    }

    public void GetPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("[GameManager] Player 태그를 찾을 수 없습니다.");
            Player = null;
            return;
        }

        Player = playerObj.GetComponent<Player>();
        if (Player == null)
        {
            Debug.LogWarning("[GameManager] Player 컴포넌트가 없습니다.");
            return;
        }

        // Inspector에 playerSpawn이 없으면 런타임 앵커 생성 후, 현재 플레이어 위치로 갱신
        EnsurePlayerSpawnMatchesPlayer();
    }

    /// <summary>
    /// playerSpawn 참조를 확보하고, 트랜스폼을 현재 Player 위치/회전으로 맞춥니다.
    /// (플레이어를 스폰으로 옮기지 않음)
    /// </summary>
    private void EnsurePlayerSpawnMatchesPlayer()
    {
        if (Player == null) return;

        if (playerSpawn == null)
        {
            GameObject existing = GameObject.Find("PlayerSpawn");
            if (existing != null)
            {
                playerSpawn = existing.transform;
            }
            else
            {
                GameObject spawnObj = new GameObject("PlayerSpawn");
                playerSpawn = spawnObj.transform;
            }
        }

        playerSpawn.SetPositionAndRotation(Player.transform.position, Player.transform.rotation);
    }

    public void SetGameInfo(StageData stageData, GameDifficulty difficulty)
    {
        CurrentStageData = stageData;
        CurrentDifficulty = difficulty;

        missionGold = stageData.GetMissionGold(difficulty);
        maxStunCount = stageData.GetMaxStunCount(difficulty);
        currentStunCount = 0;

        AreYouReady = true;
        GameEvents.RaiseQuestObjectiveChanged();
    }

    public void SetMissionGold()
    {
        UIManager.Instance.ShowUI<UI_Mission>().SettingMissionGold(missionGold);
        UIManager.Instance.ShowUI<UI_Mission>().RefreshUI(0);
    }
    #endregion

    #region Day System
    /// <summary>하루/밤 시작. 첫 진입·넉아웃 이후 스폰 배치용.</summary>
    public void StartNewDay()
    {
        if (IsGameOver) return;

        isResolvingDay = false;
        MovePlayerToSpawn();
        ResetPlayerCamera();
        currentStunCount = 0;
        RestorePlayerStatus();
        StartIngameClock();
        ShowDayCheckerToast();

        Debug.Log($"하루 시작 — 남은 일수: {RemainingDays}");
    }

    private void MovePlayerToSpawn()
    {
        if (Player == null) GetPlayer();
        if (Player == null || playerSpawn == null)
        {
            Debug.LogWarning("[GameManager] Player 또는 PlayerSpawn이 없습니다.");
            return;
        }

        // 은신 연출 코루틴이 위치를 덮어쓰지 않도록 즉시 해제
        if (Player.ActiveHideSpot != null)
            Player.ActiveHideSpot.ForceReleaseForRespawn();

        Rigidbody rb = Player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = playerSpawn.position;
            rb.rotation = playerSpawn.rotation;
        }

        Player.transform.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);
        Physics.SyncTransforms();

        Player.SetHideTransitioning(false);

        // Stunned는 HitPresenter 연출이 관리 — 여기서 Idle로 바꾸면 카메라가 덮어씌워짐
        if (Player.CurrentState == PlayerState.Hidden)
        {
            bool presenting = InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked;
            Player.ChangePlayerState(presenting ? PlayerState.Stunned : PlayerState.Idle);
        }
        else if (Player.CurrentState == PlayerState.ASMR || Player.CurrentState == PlayerState.Lobby)
        {
            Player.ChangePlayerState(PlayerState.Idle);
        }
    }

    private void ResetPlayerCamera()
    {
        if (Player == null) return;
        PlayerHitPresenter presenter = Player.GetComponent<PlayerHitPresenter>();
        if (presenter != null)
            presenter.ResetPresentationCamera();
    }

    private void RestorePlayerStatus()
    {
        if (Player == null) return;
        PlayerStatus status = Player.GetComponent<PlayerStatus>();
        if (status != null) status.ResetStatus();
    }

    private void ShowDayCheckerToast()
    {
        if (UIManager.Instance == null) return;
        UI_DayChecker dayUI = UIManager.Instance.ShowUI<UI_DayChecker>();
        if (dayUI != null)
            dayUI.ShowNightToast(RemainingDays, maxDays);
    }

    private PlayerHitPresenter GetHitPresenter()
    {
        if (Player == null) GetPlayer();
        if (Player == null) return null;

        PlayerHitPresenter presenter = Player.GetComponent<PlayerHitPresenter>();
        if (presenter == null)
            presenter = Player.gameObject.AddComponent<PlayerHitPresenter>();
        return presenter;
    }

    /// <summary>타임오버 → 넉아웃 연출 후 다음날</summary>
    private void HandleDayTimeEnded()
    {
        BeginKnockOutDayTransition();
    }

    /// <summary>유닛 공격 성공 — 기절 카운트 후 기절/넉아웃 분기</summary>
    private void HandleUnitAttackSucceeded()
    {
        if (IsGameOver || isResolvingDay) return;
        if (NewSceneManager.Instance != null && NewSceneManager.Instance.IsCurrentSceneLobby()) return;

        // ASMR 중 피격 시 강제 종료 + UI 닫기
        if (ASMRManager.Instance != null)
            ASMRManager.Instance.InterruptByCombat();

        currentStunCount++;
        Debug.Log($"피격 — stun {currentStunCount}/{maxStunCount}");

        PlayerHitPresenter presenter = GetHitPresenter();

        if (currentStunCount > maxStunCount)
        {
            BeginKnockOutDayTransition();
        }
        else
        {
            // 기절: 자리 유지, 카운트 유지, 연출 중 입력 차단은 Presenter가 처리
            if (presenter != null)
                presenter.PlayStun();
        }
    }

    /// <summary>넉아웃(타임오버/기절 초과): 눈 감은 뒤 스폰 이동·카운트 초기화 → 눈 뜨기 → 토스트</summary>
    private void BeginKnockOutDayTransition()
    {
        if (IsGameOver || isResolvingDay) return;
        if (NewSceneManager.Instance != null && NewSceneManager.Instance.IsCurrentSceneLobby()) return;

        if (ASMRManager.Instance != null)
            ASMRManager.Instance.InterruptByCombat();

        isResolvingDay = true;
        StopIngameClock();

        // 타임오버 넉아웃 등: 유닛이 남아 있으면 즉시 제거 (공격 자폭과 별개)
        if (StageManager.Instance != null)
            StageManager.Instance.DespawnForDayEnd();

        PlayerHitPresenter presenter = GetHitPresenter();
        if (presenter == null)
        {
            ApplyDayFailureWhileEyesClosed();
            FinishDayAfterKnockOutWake();
            return;
        }

        presenter.PlayKnockOut(
            onEyesClosed: ApplyDayFailureWhileEyesClosed,
            onComplete: FinishDayAfterKnockOutWake);
    }

    /// <summary>눈이 감긴 직후: 일수 감소. 게임오버면 플래그만 세우고, 결과 UI는 넘어지기 연출 종료 후 연결.</summary>
    private void ApplyDayFailureWhileEyesClosed()
    {
        RemainingDays--;
        OnRemainingDaysChanged?.Invoke(RemainingDays);
        Debug.Log($"하루 종료(눈 감김) — 남은 일수: {RemainingDays}");

        if (RemainingDays <= 0)
        {
            // Result는 HitPresenter가 넉아웃(넘어지기) 연출을 끝낸 뒤 PresentGameOverResult 호출
            IsGameOver = true;
            StopIngameClock();
            if (StageManager.Instance != null)
                StageManager.Instance.DespawnForDayEnd();
            return;
        }

        currentStunCount = 0;
        MovePlayerToSpawn();
        ResetPlayerCamera();
        RestorePlayerStatus();
    }

    /// <summary>넉아웃 넘어지기 연출이 끝난 뒤 실패 결과 UI를 엽니다.</summary>
    public void PresentGameOverResult()
    {
        if (!IsGameOver) return;
        ShowGameResult(false);
    }

    /// <summary>눈 뜬 뒤: 스폰 재확인 + 시계 재시작 + 밤 토스트 (게임오버면 스킵)</summary>
    private void FinishDayAfterKnockOutWake()
    {
        if (IsGameOver) return;

        // 연출 중 위치가 밀렸을 수 있어 스폰을 한 번 더 고정
        MovePlayerToSpawn();
        ResetPlayerCamera();

        isResolvingDay = false;
        StartIngameClock();
        ShowDayCheckerToast();

        // 다음날 유닛 재스폰 (공격 리스폰과 동일하게 잠시 뒤)
        if (StageManager.Instance != null)
            StageManager.Instance.ScheduleRespawn(10f);

        Debug.Log($"다음날 시작 — 남은 일수: {RemainingDays}");
    }

    /// <summary>미션 완료 후 탈출 문 홀드 성공 시</summary>
    public void OnEscapeSuccess()
    {
        if (IsGameOver || isResolvingDay) return;
        if (!AllMissionCompleted)
        {
            Debug.Log("미션 금액 미달 — 탈출할 수 없습니다.");
            return;
        }

        isResolvingDay = true;
        StopIngameClock();
        ShowGameResult(true);
    }

    private void ShowGameResult(bool success)
    {
        IsGameOver = true;
        StopIngameClock();

        if (StageManager.Instance != null)
            StageManager.Instance.DespawnForDayEnd();

        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputBlocked(false);

        Time.timeScale = 0f;

        UI_Result resultUI = UIManager.Instance.ShowUI<UI_Result>();
        if (resultUI != null) resultUI.SettingResultUI(success);
        else Debug.LogError("[GameManager] UI_Result를 찾을 수 없습니다.");
    }
    #endregion

    #region In-Game Clock
    public void StartIngameClock()
    {
        elapsedRealSeconds = 0f;
        hasClockEnded = false;
        isClockRunning = true;
        SetDisplayedMinutes(0);

        UI_IngameClock clockUI = UIManager.Instance.ShowUI<UI_IngameClock>();
        if (clockUI != null) clockUI.RefreshTime(IngameClockDisplay);

        if (AmbientSoundDirector.Instance != null)
            AmbientSoundDirector.Instance.StartDayAmbient(realDurationSeconds);
    }

    public void StopIngameClock()
    {
        isClockRunning = false;
        if (AmbientSoundDirector.Instance != null)
            AmbientSoundDirector.Instance.StopDayAmbient();
    }

    /// <summary>일시정지용 — 경과 시간은 유지한 채 시계만 멈춤</summary>
    public void PauseIngameClock()
    {
        isClockRunning = false;
        if (AmbientSoundDirector.Instance != null)
            AmbientSoundDirector.Instance.SetPaused(true);
    }

    /// <summary>일시정지 해제 — 리셋 없이 시계 재개</summary>
    public void ResumeIngameClock()
    {
        if (IsGameOver || hasClockEnded) return;
        if (NewSceneManager.Instance != null && NewSceneManager.Instance.IsCurrentSceneLobby()) return;
        isClockRunning = true;
        if (AmbientSoundDirector.Instance != null)
            AmbientSoundDirector.Instance.SetPaused(false);
    }

    private void UpdateIngameClock()
    {
        if (!isClockRunning || hasClockEnded || IsGameOver) return;
        // timeScale=0 이면 deltaTime도 0
        if (Time.timeScale <= 0f) return;

        elapsedRealSeconds += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedRealSeconds / Mathf.Max(0.01f, realDurationSeconds));

        float totalGameMinutes = t * gameDurationHours * 60f;
        int stepped = Mathf.FloorToInt(totalGameMinutes / displayStepGameMinutes) * displayStepGameMinutes;
        int maxMinutes = Mathf.RoundToInt(gameDurationHours * 60f);
        stepped = Mathf.Clamp(stepped, 0, maxMinutes);

        if (stepped != DisplayedGameMinutes)
            SetDisplayedMinutes(stepped);

        if (t >= 1f && !hasClockEnded)
        {
            hasClockEnded = true;
            isClockRunning = false;
            if (AmbientSoundDirector.Instance != null)
                AmbientSoundDirector.Instance.StopDayAmbient();
            SetDisplayedMinutes(maxMinutes);
            OnIngameTimeEnded?.Invoke();
            Debug.Log("인게임 시간 종료 (06:00)");
        }
    }

    private void SetDisplayedMinutes(int gameMinutes)
    {
        DisplayedGameMinutes = gameMinutes;
        IngameClockDisplay = FormatGameTime(gameMinutes);
        OnIngameClockChanged?.Invoke(IngameClockDisplay);
    }

    private static string FormatGameTime(int totalMinutes)
    {
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        return $"{hours:00}:{minutes:00}";
    }
    #endregion

    #region ASMR Reward
    public void SuccessASMR()
    {
        currentGold += 1000;

        UI_Mission ui = UIManager.Instance.ShowUI<UI_Mission>();
        if (ui != null) ui.RefreshUI(currentGold);

        CheckMissionGold();
    }

    private void CheckMissionGold()
    {
        if (currentGold >= missionGold)
        {
            Debug.Log("미션 금액 달성!! 탈출하세요");
            AllMissionCompleted = true;
            GameEvents.RaiseQuestObjectiveChanged();
        }
    }
    #endregion

    #region ChangeScene
    public void ChangeScene()
    {
        // 로비 → 스테이지 진입
        if (NewSceneManager.Instance.IsCurrentSceneLobby())
        {
            NewSceneManager.Instance.ChangeScene(CurrentStageData);
            return;
        }

        // 스테이지에서 탈출 문: 성공 처리
        OnEscapeSuccess();
    }
    #endregion
}
