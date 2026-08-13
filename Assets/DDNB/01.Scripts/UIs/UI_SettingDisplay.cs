using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 및 그래픽 세부 설정 패널 (Display).
/// Back=Brief로, Reset=그래픽 기본값, Confirm=적용·저장 후 Brief.
/// </summary>
public class UI_SettingDisplay : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button button_Back;
    [SerializeField] private Button button_Reset;
    [SerializeField] private Button button_Confirm;

    [Header("Controls")]
    [SerializeField] private TMP_Dropdown dropdown_Display;
    [SerializeField] private TMP_Dropdown dropdown_Frame;
    [SerializeField] private TMP_Dropdown dropdown_Graphic;
    [SerializeField] private Toggle toggle_VSync;

    private UI_Setting _root;
    private GameSettingsData _draft;

    private void Awake()
    {
        AutoBindIfNeeded();
        _root = GetComponentInParent<UI_Setting>();
    }

    private void OnEnable()
    {
        AutoBindIfNeeded();
        EnsureDropdownOptions();
        BindListeners();
        LoadDraft();
        PushToUi();
    }

    private void OnDisable()
    {
        UnbindListeners();
    }

    private void AutoBindIfNeeded()
    {
        Transform buttons = transform.Find("Buttons");
        if (button_Back == null)
            button_Back = buttons != null ? buttons.Find("Button_Back")?.GetComponent<Button>() : null;
        if (button_Reset == null)
            button_Reset = buttons != null ? buttons.Find("Button_Reset")?.GetComponent<Button>() : null;
        if (button_Confirm == null)
            button_Confirm = buttons != null ? buttons.Find("Button_Confirm")?.GetComponent<Button>() : null;

        if (dropdown_Display == null)
            dropdown_Display = transform.Find("Display")?.GetComponentInChildren<TMP_Dropdown>(true);
        if (dropdown_Frame == null)
            dropdown_Frame = transform.Find("Frame")?.GetComponentInChildren<TMP_Dropdown>(true);
        if (dropdown_Graphic == null)
            dropdown_Graphic = transform.Find("Graphic")?.GetComponentInChildren<TMP_Dropdown>(true);
        if (toggle_VSync == null)
            toggle_VSync = transform.Find("VSync")?.GetComponentInChildren<Toggle>(true);
    }

    private void EnsureDropdownOptions()
    {
        SetOptions(dropdown_Display, SettingManager.DisplayModeLabels);
        StyleDropdown(dropdown_Display);

        if (dropdown_Frame != null)
        {
            var labels = new List<string>();
            foreach (int fps in SettingManager.FrameRateOptions)
                labels.Add($"{fps} FPS");
            SetOptions(dropdown_Frame, labels.ToArray());
            StyleDropdown(dropdown_Frame);
        }

        SetOptions(dropdown_Graphic, SettingManager.QualityLabels);
        StyleDropdown(dropdown_Graphic);
    }

    private static void SetOptions(TMP_Dropdown dropdown, string[] labels)
    {
        if (dropdown == null || labels == null) return;

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(labels));
        dropdown.RefreshShownValue();
    }

    /// <summary>드롭다운 Label 폰트 24, Content/Item 높이 40</summary>
    private static void StyleDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;

        if (dropdown.captionText != null)
            dropdown.captionText.fontSize = 24f;

        if (dropdown.itemText != null)
            dropdown.itemText.fontSize = 24f;

        if (dropdown.template == null) return;

        Transform item = dropdown.template.Find("Viewport/Content/Item");
        if (item is RectTransform itemRt)
        {
            Vector2 size = itemRt.sizeDelta;
            size.y = 40f;
            itemRt.sizeDelta = size;
            itemRt.anchoredPosition = new Vector2(itemRt.anchoredPosition.x, 0f);
        }
    }

    private void BindListeners()
    {
        if (button_Back != null)
        {
            button_Back.onClick.RemoveListener(OnClickBack);
            button_Back.onClick.AddListener(OnClickBack);
        }

        if (button_Reset != null)
        {
            button_Reset.onClick.RemoveListener(OnClickReset);
            button_Reset.onClick.AddListener(OnClickReset);
        }

        if (button_Confirm != null)
        {
            button_Confirm.onClick.RemoveListener(OnClickConfirm);
            button_Confirm.onClick.AddListener(OnClickConfirm);
        }
    }

    private void UnbindListeners()
    {
        if (button_Back != null) button_Back.onClick.RemoveListener(OnClickBack);
        if (button_Reset != null) button_Reset.onClick.RemoveListener(OnClickReset);
        if (button_Confirm != null) button_Confirm.onClick.RemoveListener(OnClickConfirm);
    }

    private void LoadDraft()
    {
        _draft = SettingManager.Instance != null
            ? SettingManager.Instance.Settings
            : GameSettingsData.CreateDefault();
    }

    private void PushToUi()
    {
        if (_draft == null) return;

        if (dropdown_Display != null)
            dropdown_Display.SetValueWithoutNotify(Mathf.Clamp(_draft.displayModeIndex, 0, Mathf.Max(0, dropdown_Display.options.Count - 1)));

        if (dropdown_Frame != null)
        {
            int frameIndex = 0;
            for (int i = 0; i < SettingManager.FrameRateOptions.Length; i++)
            {
                if (SettingManager.FrameRateOptions[i] == _draft.targetFrameRate)
                {
                    frameIndex = i;
                    break;
                }
            }
            dropdown_Frame.SetValueWithoutNotify(frameIndex);
        }

        if (dropdown_Graphic != null)
            dropdown_Graphic.SetValueWithoutNotify(Mathf.Clamp(_draft.qualityLevel, 0, 2));

        if (toggle_VSync != null)
            toggle_VSync.SetIsOnWithoutNotify(_draft.vSyncEnabled);

        dropdown_Display?.RefreshShownValue();
        dropdown_Frame?.RefreshShownValue();
        dropdown_Graphic?.RefreshShownValue();
    }

    private void PullFromUi()
    {
        if (_draft == null)
            _draft = GameSettingsData.CreateDefault();

        if (dropdown_Display != null)
            _draft.displayModeIndex = dropdown_Display.value;

        if (dropdown_Frame != null)
        {
            int idx = Mathf.Clamp(dropdown_Frame.value, 0, SettingManager.FrameRateOptions.Length - 1);
            _draft.targetFrameRate = SettingManager.FrameRateOptions[idx];
        }

        if (dropdown_Graphic != null)
            _draft.qualityLevel = Mathf.Clamp(dropdown_Graphic.value, 0, 2);

        if (toggle_VSync != null)
            _draft.vSyncEnabled = toggle_VSync.isOn;
    }

    private void OnClickBack()
    {
        if (_root != null) _root.ShowBrief();
        else gameObject.SetActive(false);
    }

    private void OnClickReset()
    {
        GameSettingsData current = SettingManager.Instance != null
            ? SettingManager.Instance.Settings
            : GameSettingsData.CreateDefault();
        _draft = GameSettingsData.CreateGraphicsDefault(current);
        PushToUi();
    }

    private void OnClickConfirm()
    {
        PullFromUi();
        if (SettingManager.Instance != null)
            SettingManager.Instance.ApplyGraphics(_draft, save: true);

        if (_root != null) _root.ShowBrief();
        else gameObject.SetActive(false);
    }
}
