using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player")]
    public Player Player { get; private set; }

    public bool AreYouReady { get; private set; }
    public bool AllMissionCompleted { get; private set; }

    [Header("Current Game Info")]
    public StageData CurrentStageData { get; private set; }
    public GameDifficulty CurrentDifficulty { get; private set; }

    [SerializeField] private int gameTime;      // 게임 시간
    [SerializeField] private int missionGold;   // 목표 금액
    [SerializeField] private int currentGold;   // 현재 금액


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

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

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

    public void OnNoiseDetected()
    {
        Debug.LogError("소음이 감지되었습니다!!!!");
    }

    public void ChangeScene()
    {
        if (NewSceneManager.Instance.IsCurrentSceneLobby()) NewSceneManager.Instance.ChangeScene(CurrentStageData);
        else
        {
            NewSceneManager.Instance.GoToLobby();
            AreYouReady = false;
        }
    }
}