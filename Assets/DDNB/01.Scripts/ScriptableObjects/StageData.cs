using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "ScriptableObjects/StageData", order = 1)]
public class StageData : ScriptableObject
{
    [Header("Info")]
    public string stageID;          // 스테이지 고유 아이디 (예: "Stage_01")
    public string mapName;          // UI에 표시될 맵 이름 (예: "Abandoned House")
    public string sceneName;        // 로드할 씬 이름

    [Header("Mission Gold by Difficulty")]
    public int missionGoldEasy = 4000;
    public int missionGoldNormal = 7000;
    public int missionGoldHard = 10000;

    [Header("Max Stun Count by Difficulty")]
    [Tooltip("허용 기절 횟수. 초과 시 넉아웃(하루 소비). Hard 0 = 첫 피격에 넉아웃")]
    public int maxStunnedEasy = 2;
    public int maxStunnedNormal = 1;
    public int maxStunnedHard = 0;

    [Header("Description")]
    public string description;      // 맵 설명

    [Header("Unit Settings")]
    public GameObject unitPrefab;   // 해당 스테이지에 등장할 유닛 프리팹
    public UnitData unitData;       // 해당 유닛의 속성 데이터

    /// <summary>난이도에 맞는 미션 골드 반환</summary>
    public int GetMissionGold(GameDifficulty difficulty)
    {
        return difficulty switch
        {
            GameDifficulty.Easy => missionGoldEasy,
            GameDifficulty.Normal => missionGoldNormal,
            GameDifficulty.Hard => missionGoldHard,
            _ => missionGoldNormal
        };
    }

    /// <summary>난이도에 맞는 최대 기절 허용 횟수 반환</summary>
    public int GetMaxStunCount(GameDifficulty difficulty)
    {
        return difficulty switch
        {
            GameDifficulty.Easy => maxStunnedEasy,
            GameDifficulty.Normal => maxStunnedNormal,
            GameDifficulty.Hard => maxStunnedHard,
            _ => maxStunnedNormal
        };
    }
}
