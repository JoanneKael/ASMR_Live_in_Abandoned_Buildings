using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 루트 — Brief에서 Display / Input / Sounds 패널만 열고 닫습니다.
/// 각 세부 설정의 적용·초기화는 패널 스크립트가 담당합니다.
/// </summary>
public class UI_Setting : UI_Base
{
    [Header("Brief")]
    [SerializeField] private GameObject brief;
    [SerializeField] private Button button_Back;
    [SerializeField] private Button button_Graphics;
    [SerializeField] private Button button_Input;
    [SerializeField] private Button button_Sounds;

    [Header("Panels")]
    [SerializeField] private GameObject panel_Display;
    [SerializeField] private GameObject panel_Input;
    [SerializeField] private GameObject panel_Sounds;

    private void Awake()
    {
        AutoBindIfNeeded();
    }

    private void OnEnable()
    {
        AutoBindIfNeeded();
        BindListeners();
        ShowBrief();
    }

    private void OnDisable()
    {
        UnbindListeners();
    }

    private void AutoBindIfNeeded()
    {
        if (brief == null)
        {
            Transform t = transform.Find("Brief");
            if (t == null) t = transform.Find("brief");
            if (t != null) brief = t.gameObject;
        }

        if (panel_Display == null)
        {
            Transform t = transform.Find("Display");
            if (t != null) panel_Display = t.gameObject;
        }

        if (panel_Input == null)
        {
            Transform t = transform.Find("Input");
            if (t != null) panel_Input = t.gameObject;
        }

        if (panel_Sounds == null)
        {
            Transform t = transform.Find("Sounds");
            if (t != null) panel_Sounds = t.gameObject;
        }

        if (brief != null)
        {
            if (button_Back == null)
                button_Back = brief.transform.Find("Button_Back")?.GetComponent<Button>();
            if (button_Graphics == null)
                button_Graphics = brief.transform.Find("Button_Graphics")?.GetComponent<Button>();
            if (button_Input == null)
                button_Input = brief.transform.Find("Button_Input")?.GetComponent<Button>();
            if (button_Sounds == null)
                button_Sounds = brief.transform.Find("Button_Sounds")?.GetComponent<Button>();
        }
    }

    private void BindListeners()
    {
        if (button_Back != null)
        {
            button_Back.onClick.RemoveListener(OnClickBriefBack);
            button_Back.onClick.AddListener(OnClickBriefBack);
        }

        if (button_Graphics != null)
        {
            button_Graphics.onClick.RemoveListener(OnClickGraphics);
            button_Graphics.onClick.AddListener(OnClickGraphics);
        }

        if (button_Input != null)
        {
            button_Input.onClick.RemoveListener(OnClickInput);
            button_Input.onClick.AddListener(OnClickInput);
        }

        if (button_Sounds != null)
        {
            button_Sounds.onClick.RemoveListener(OnClickSounds);
            button_Sounds.onClick.AddListener(OnClickSounds);
        }
    }

    private void UnbindListeners()
    {
        if (button_Back != null) button_Back.onClick.RemoveListener(OnClickBriefBack);
        if (button_Graphics != null) button_Graphics.onClick.RemoveListener(OnClickGraphics);
        if (button_Input != null) button_Input.onClick.RemoveListener(OnClickInput);
        if (button_Sounds != null) button_Sounds.onClick.RemoveListener(OnClickSounds);
    }

    /// <summary>Brief만 켜고 세부 패널은 모두 끕니다.</summary>
    public void ShowBrief()
    {
        SetActiveSafe(brief, true);
        SetActiveSafe(panel_Display, false);
        SetActiveSafe(panel_Input, false);
        SetActiveSafe(panel_Sounds, false);
    }

    public void OpenDisplay() => OpenPanel(panel_Display);
    public void OpenInput() => OpenPanel(panel_Input);
    public void OpenSounds() => OpenPanel(panel_Sounds);

    private void OpenPanel(GameObject panel)
    {
        if (panel == null) return;

        SetActiveSafe(brief, false);
        SetActiveSafe(panel_Display, false);
        SetActiveSafe(panel_Input, false);
        SetActiveSafe(panel_Sounds, false);
        SetActiveSafe(panel, true);
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }

    private void OnClickBriefBack()
    {
        CloseUI();
    }

    private void OnClickGraphics() => OpenDisplay();
    private void OnClickInput() => OpenInput();
    private void OnClickSounds() => OpenSounds();
}
