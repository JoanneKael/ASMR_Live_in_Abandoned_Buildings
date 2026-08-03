using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player")]
    public Player Player { get; private set; }

    public bool AreYouReady { get; private set; }
    public bool AllMissionCompleted { get; private set; }

    [Header("Current Stage Info")]
    public StageData CurrentStageData { get; private set; }
    public GameDifficulty CurrentDifficulty { get; private set; }

    [SerializeField] private int missionGold;   // 목표 금액
    [SerializeField] private int currentGold;   // 현재 금액

    [Header("In-Game Clock")]
    [Tooltip("실제 시간(초). 기본 5분 = 300초")]
    [SerializeField] private float realDurationSeconds = 300f;
    [Tooltip("대응 게임 시간(시간). 기본 6시간")]
    [SerializeField] private float gameDurationHours = 6f;
    [Tooltip("UI 갱신 간격(게임 분). 기본 10분")]
    [SerializeField] private int displayStepGameMinutes = 10;

    /// <summary>표시용 게임 분 (0 ~ 360, 20분 단위)</summary>
    public int DisplayedGameMinutes { get; private set; }

    /// <summary>현재 시계 문자열 (예: "02:40")</summary>
    public string IngameClockDisplay { get; private set; } = "00:00";

    /// <summary>시계 표시가 바뀔 때 (20분 단위)</summary>
    public event Action<string> OnIngameClockChanged;

    /// <summary>06:00 도달 시</summary>
    public event Action OnIngameTimeEnded;

    private float elapsedRealSeconds;
    private bool isClockRunning;
    private bool hasClockEnded;

    [Header("Ghost Spawn Settings")]
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private UnitData unitData;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Spawn Timers")]
    [SerializeField] private float initialSpawnDelay = 30f;
    [SerializeField] private float respawnDelay = 20f;

    private void Awake()
    {
        Instance = this;

        if (GetComponent<NoiseManager>() == null && NoiseManager.Instance == null)
            gameObject.AddComponent<NoiseManager>();
    }

    private void Update()
    {
        UpdateIngameClock();
    }

    #region Init

    public void Init()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (Player == null) GetPlayer();
        if (NewSceneManager.Instance.IsCurrentSceneLobby()) return;

        SetMissionGold();
        StartIngameClock();
    }

    public void GetPlayer()
    {
        Player = GameObject.FindWithTag("Player").GetComponent<Player>();
    }

    public void SetGameInfo(StageData stageData, GameDifficulty difficulty)
    {
        CurrentStageData = stageData;
        CurrentDifficulty = difficulty;

        missionGold = stageData.missionGold;

        AreYouReady = true;
    }

    public void SetMissionGold()
    {
        UIManager.Instance.ShowUI<UI_Mission>().SettingMissionGold(missionGold);
        UIManager.Instance.ShowUI<UI_Mission>().RefreshUI(0);
    }
    #endregion

    #region In-Game Clock
    /// <summary>
    /// 실제 5분 = 게임 6시간. UI는 00:00~06:00을 20분 간격으로 표시.
    /// </summary>
    public void StartIngameClock()
    {
        elapsedRealSeconds = 0f;
        hasClockEnded = false;
        isClockRunning = true;
        SetDisplayedMinutes(0);

        UI_IngameClock clockUI = UIManager.Instance.ShowUI<UI_IngameClock>();
        if (clockUI != null) clockUI.RefreshTime(IngameClockDisplay);
    }

    public void StopIngameClock()
    {
        isClockRunning = false;
    }

    private void UpdateIngameClock()
    {
        if (!isClockRunning || hasClockEnded) return;

        elapsedRealSeconds += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedRealSeconds / Mathf.Max(0.01f, realDurationSeconds));

        float totalGameMinutes = t * gameDurationHours * 60f; // 0 ~ 360
        int stepped = Mathf.FloorToInt(totalGameMinutes / displayStepGameMinutes) * displayStepGameMinutes;
        int maxMinutes = Mathf.RoundToInt(gameDurationHours * 60f);
        stepped = Mathf.Clamp(stepped, 0, maxMinutes);

        if (stepped != DisplayedGameMinutes)
            SetDisplayedMinutes(stepped);

        if (t >= 1f && !hasClockEnded)
        {
            hasClockEnded = true;
            isClockRunning = false;
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
        }
    }
    #endregion

    #region ChangeScene
    public void ChangeScene()
    {
        if (NewSceneManager.Instance.IsCurrentSceneLobby()) NewSceneManager.Instance.ChangeScene(CurrentStageData);
        else
        {
            NewSceneManager.Instance.GoToLobby();
            AreYouReady = false;
            StopIngameClock();
        }
    }
    #endregion
}
