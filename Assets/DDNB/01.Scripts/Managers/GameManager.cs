using System.Collections;
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

    [SerializeField] private int gameTime;      // 게임 시간
    [SerializeField] private int missionGold;   // 목표 금액
    [SerializeField] private int currentGold;   // 현재 금액

    [Header("Ghost Spawn Settings")]
    [SerializeField] private GameObject unitPrefab;     // 유닛UI 프리팹
    [SerializeField] private UnitData unitData;         // 적용할 SO 데이터
    [SerializeField] private Transform[] patrolPoints;  // 패트롤 포인트 지점들

    [Header("Spawn Timers")]
    [SerializeField] private float initialSpawnDelay = 30f; // 첫 진입 후 스폰까지 시간
    [SerializeField] private float respawnDelay = 20f;      // 디스폰 후 재스폰까지 시간


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (MICManager.Instance != null)
            MICManager.Instance.OnNoiseDetected += OnNoiseDetected;
    }

    private void OnDisable()
    {
        if (MICManager.Instance != null)
            MICManager.Instance.OnNoiseDetected -= OnNoiseDetected;
    }

    #region Init

    public void Init()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (Player == null) GetPlayer();
        if (NewSceneManager.Instance.IsCurrentSceneLobby()) return;

        SetMissionGold();
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

    #region ASMR
    public void StartASMR(InteractableObject target)
    {
        ASMRManager.Instance.StartASMR(target);
    }

    public void EndASMR()
    {
        ASMRManager.Instance.EndASMR();
    }

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

    #region Noise
    public void OnNoiseDetected()
    {
        Debug.LogError("소음이 감지되었습니다!!!!");
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
        }
    }
    #endregion
}