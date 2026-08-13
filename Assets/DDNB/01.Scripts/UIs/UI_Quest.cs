using TMPro;
using UnityEngine;

/// <summary>
/// HUD 퀘스트 안내. Text_Quest에 상황별 목표 문구를 표시합니다.
/// </summary>
public class UI_Quest : MonoBehaviour
{
    private const string LobbySelectMap = "앞의 컴퓨터에서 맵과 난이도를 선택하세요";
    private const string LobbyExitDoor = "뒤의 문으로 나가세요";
    private const string InGameDoAsmr = "건물을 돌아다니며 ASMR을 진행하세요";
    private const string InGameEscape = "현관문을 통해 탈출하세요";

    [SerializeField] private TextMeshProUGUI text_Quest;

    private void Awake()
    {
        AutoBindIfNeeded();
    }

    private void OnEnable()
    {
        GameEvents.OnQuestObjectiveChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnQuestObjectiveChanged -= Refresh;
    }

    private void AutoBindIfNeeded()
    {
        if (text_Quest != null) return;

        Transform t = transform.Find("Text_Quest");
        if (t != null)
            text_Quest = t.GetComponent<TextMeshProUGUI>();

        if (text_Quest == null)
            text_Quest = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    /// <summary>로비/인게임·준비·미션 상태에 맞춰 문구 갱신</summary>
    public void Refresh()
    {
        AutoBindIfNeeded();
        if (text_Quest == null) return;

        text_Quest.text = ResolveQuestText();
    }

    private static string ResolveQuestText()
    {
        bool inLobby = NewSceneManager.Instance != null
            && NewSceneManager.Instance.IsCurrentSceneLobby();

        if (inLobby)
        {
            bool ready = GameManager.Instance != null && GameManager.Instance.AreYouReady;
            return ready ? LobbyExitDoor : LobbySelectMap;
        }

        // 인게임
        bool missionDone = GameManager.Instance != null && GameManager.Instance.AllMissionCompleted;
        return missionDone ? InGameEscape : InGameDoAsmr;
    }
}
