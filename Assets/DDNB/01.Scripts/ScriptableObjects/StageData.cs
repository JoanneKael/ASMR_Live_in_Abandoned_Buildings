using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "ScriptableObjects/StageData", order = 1)]
public class StageData : ScriptableObject
{
    [Header("Info")]
    public string stageID;          // 스테이지 고유 아이디 (예: "Stage_01")
    public string mapName;          // UI에 표시될 맵 이름 (예: "Abandoned House")
    public string sceneName;        // 로드할 씬 이름

    [Header("Game Play")]
    public int missionGold;         // 클리어에 필요한 미션 금액

    [Header("Description")]
    [TextArea]
    public string description;      // 맵 설명
}
