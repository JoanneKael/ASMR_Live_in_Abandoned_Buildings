using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 입력 세부 설정 패널 (Input).
/// 마우스 감도 슬라이더(0~3, 기본 1.5).
/// </summary>
public class UI_SettingInput : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button button_Back;
    [SerializeField] private Button button_Reset;
    [SerializeField] private Button button_Confirm;

    [Header("Controls")]
    [SerializeField] private Slider slider_MouseSensitivity;
    [SerializeField] private TextMeshProUGUI text_MouseSensitivityValue;

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
        ConfigureSlider();
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

        Transform row = transform.Find("MouseSensitivity");
        if (row == null) row = transform.Find("Sensitivity");

        if (slider_MouseSensitivity == null && row != null)
            slider_MouseSensitivity = row.GetComponentInChildren<Slider>(true);

        if (text_MouseSensitivityValue == null && row != null)
        {
            Transform value = row.Find("Value");
            if (value != null)
                text_MouseSensitivityValue = value.GetComponent<TextMeshProUGUI>();
            if (text_MouseSensitivityValue == null)
                text_MouseSensitivityValue = row.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void ConfigureSlider()
    {
        if (slider_MouseSensitivity == null) return;
        slider_MouseSensitivity.minValue = SettingManager.MinMouseSensitivity;
        slider_MouseSensitivity.maxValue = SettingManager.MaxMouseSensitivity;
        slider_MouseSensitivity.wholeNumbers = false;
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

        if (slider_MouseSensitivity != null)
        {
            slider_MouseSensitivity.onValueChanged.RemoveListener(OnSensitivityChanged);
            slider_MouseSensitivity.onValueChanged.AddListener(OnSensitivityChanged);
        }
    }

    private void UnbindListeners()
    {
        if (button_Back != null) button_Back.onClick.RemoveListener(OnClickBack);
        if (button_Reset != null) button_Reset.onClick.RemoveListener(OnClickReset);
        if (button_Confirm != null) button_Confirm.onClick.RemoveListener(OnClickConfirm);
        if (slider_MouseSensitivity != null)
            slider_MouseSensitivity.onValueChanged.RemoveListener(OnSensitivityChanged);
    }

    private void LoadDraft()
    {
        _draft = SettingManager.Instance != null
            ? SettingManager.Instance.Settings
            : GameSettingsData.CreateDefault();
    }

    private void PushToUi()
    {
        if (_draft == null || slider_MouseSensitivity == null) return;

        float value = Mathf.Clamp(_draft.mouseSensitivity,
            SettingManager.MinMouseSensitivity,
            SettingManager.MaxMouseSensitivity);
        slider_MouseSensitivity.SetValueWithoutNotify(value);
        RefreshValueLabel(value);
    }

    private void PullFromUi()
    {
        if (_draft == null)
            _draft = GameSettingsData.CreateDefault();

        if (slider_MouseSensitivity != null)
            _draft.mouseSensitivity = slider_MouseSensitivity.value;
    }

    private void OnSensitivityChanged(float value)
    {
        RefreshValueLabel(value);
    }

    private void RefreshValueLabel(float value)
    {
        if (text_MouseSensitivityValue != null)
            text_MouseSensitivityValue.text = value.ToString("0.00");
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
        _draft = GameSettingsData.CreateInputDefault(current);
        PushToUi();
    }

    private void OnClickConfirm()
    {
        PullFromUi();
        if (SettingManager.Instance != null)
            SettingManager.Instance.ApplyInput(_draft, save: true);

        if (_root != null) _root.ShowBrief();
        else gameObject.SetActive(false);
    }
}
