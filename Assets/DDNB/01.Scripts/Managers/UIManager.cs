using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    #region UI Refs
    private Transform _hud;
    private Transform HUD => _hud ? _hud : (_hud = GameObject.Find("HUD")?.transform);

    private UI_Status ui_Status;
    public UI_Status UI_Status => ui_Status ? ui_Status : (ui_Status = HUD.GetComponentInChildren<UI_Status>(true));

    private UI_Decibel ui_Decibel;
    public UI_Decibel UI_Decibel => ui_Decibel ? ui_Decibel : (ui_Decibel = HUD.GetComponentInChildren<UI_Decibel>(true));

    private UI_ASMR ui_ASMR;
    public UI_ASMR UI_ASMR => ui_ASMR ? ui_ASMR : (ui_ASMR = HUD.GetComponentInChildren<UI_ASMR>(true));

    private UI_Minigame ui_Minigame;
    public UI_Minigame UI_Minigame => ui_Minigame ? ui_Minigame : (ui_Minigame = HUD.GetComponentInChildren<UI_Minigame>(true));
    #endregion

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// 제네릭 타입의 UI를 찾아서 활성화/비활성화합니다.
    /// </summary>
    public void ShowUI<T>(bool isActive) where T : Component
    {
        if (HUD == null)
        {
            Debug.LogError("HUD를 찾을 수 없습니다.");
            return;
        }

        // 1. 자식 중에서 T 타입의 컴포넌트를 찾음 (비활성화 상태여도 찾음)
        T uiComponent = HUD.GetComponentInChildren<T>(true);

        if (uiComponent != null)
        {
            // 2. 해당 컴포넌트의 게임오브젝트 활성화
            uiComponent.gameObject.SetActive(isActive);
        }
        else
        {
            Debug.LogWarning($"{typeof(T).Name}을(를) 찾을 수 없습니다.");
        }
    }
}