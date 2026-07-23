using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Lobby : UI_Base
{
    [Header("Setting Confirm")]
    [SerializeField] private Button btn_Confirm;
    [SerializeField] private Button btn_Cancel;

    [Header("Map Info")]
    [SerializeField] private Button btn_Map_Left;
    [SerializeField] private Button btn_Map_Right;

    [Header("Difficulty Info")]
    [SerializeField] private Button btn_Diff_Left;
    [SerializeField] private Button btn_Diff_Right;

    [Header("UI Text Displays")]
    [SerializeField] private TextMeshProUGUI txt_MapName;
    [SerializeField] private TextMeshProUGUI txt_Difficulty;

    [Header("Data Sources")]
    [SerializeField] private List<StageData> stageList;
    private GameDifficulty[] difficultyList = { GameDifficulty.Easy, GameDifficulty.Normal, GameDifficulty.Hard };

    private int currentMapIndex = 0;
    private int currentDiffIndex = 0;

    void Start()
    {
        Bind();
        UpdateUI();
    }

    private void Bind()
    {
        btn_Confirm.onClick.AddListener(ConfirmSettings);
        btn_Cancel.onClick.AddListener(CloseUI);

        btn_Map_Left.onClick.AddListener(() => ShowMapInfos(false));
        btn_Map_Right.onClick.AddListener(() => ShowMapInfos(true));
        btn_Diff_Left.onClick.AddListener(() => ShowDiffInfos(false));
        btn_Diff_Right.onClick.AddListener(() => ShowDiffInfos(true));
    }

    private void ShowMapInfos(bool isRight)
    {
        if (stageList == null || stageList.Count == 0) return;

        if (isRight)
        {
            currentMapIndex++;
            if (currentMapIndex >= stageList.Count) currentMapIndex = 0;
        }
        else
        {
            currentMapIndex--;
            if (currentMapIndex < 0) currentMapIndex = stageList.Count - 1;
        }

        UpdateUI();

    }

    private void ShowDiffInfos(bool isRight)
    {
        if (isRight)
        {
            currentDiffIndex++;
            if (currentDiffIndex >= difficultyList.Length) currentDiffIndex = 0;
        }
        else
        {
            currentDiffIndex--;
            if (currentDiffIndex < 0) currentDiffIndex = difficultyList.Length - 1;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (stageList != null && stageList.Count > 0)
        {
            StageData currentStage = stageList[currentMapIndex];

            if (txt_MapName != null)
                txt_MapName.text = currentStage.mapName;
        }

        if (txt_Difficulty != null)
        {
            txt_Difficulty.text = difficultyList[currentDiffIndex].ToString();
        }
    }

    private void ConfirmSettings()
    {
        if (stageList == null || stageList.Count == 0)
        {
            Debug.LogWarning("등록된 스테이지 데이터가 없습니다!");
            return;
        }

        // 현재 선택된 맵 데이터와 난이도를 GameManager로 전달
        StageData selectedStageData = stageList[currentMapIndex];
        GameDifficulty selectedDifficulty = difficultyList[currentDiffIndex];

        Debug.Log($"StageData : {selectedStageData}, GameDifficulty : {selectedDifficulty}");

        GameManager.Instance.SetGameInfo(selectedStageData, selectedDifficulty);

        CloseUI();
    }

    public override void CloseUI()
    {
        base.CloseUI();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        GameManager.Instance.Player.ChangePlayerState(PlayerState.Idle);
    }
}