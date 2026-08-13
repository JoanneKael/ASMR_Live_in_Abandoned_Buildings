using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사운드 세부 설정: 마스터 / 환경음 / 효과음 + 뮤트 + 숫자.
/// 행 이름: Master, Ambient, SFX (하위 Slider / Mute / Value)
/// </summary>
public class UI_SettingSounds : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button button_Back;
    [SerializeField] private Button button_Reset;
    [SerializeField] private Button button_Confirm;

    [Header("Master")]
    [SerializeField] private Slider slider_Master;
    [SerializeField] private Toggle toggle_MasterMute;
    [SerializeField] private TextMeshProUGUI text_MasterValue;

    [Header("Ambient")]
    [SerializeField] private Slider slider_Ambient;
    [SerializeField] private Toggle toggle_AmbientMute;
    [SerializeField] private TextMeshProUGUI text_AmbientValue;

    [Header("SFX")]
    [SerializeField] private Slider slider_Sfx;
    [SerializeField] private Toggle toggle_SfxMute;
    [SerializeField] private TextMeshProUGUI text_SfxValue;

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
        ConfigureSliders();
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

        BindRow("Master", ref slider_Master, ref toggle_MasterMute, ref text_MasterValue);
        BindRow("Ambient", ref slider_Ambient, ref toggle_AmbientMute, ref text_AmbientValue);
        BindRow("SFX", ref slider_Sfx, ref toggle_SfxMute, ref text_SfxValue);
    }

    private void BindRow(string rowName, ref Slider slider, ref Toggle mute, ref TextMeshProUGUI valueText)
    {
        Transform row = transform.Find(rowName);
        if (row == null) return;

        if (slider == null)
            slider = row.Find("Slider")?.GetComponent<Slider>() ?? row.GetComponentInChildren<Slider>(true);
        if (mute == null)
            mute = row.Find("Mute")?.GetComponent<Toggle>() ?? row.GetComponentInChildren<Toggle>(true);
        if (valueText == null)
        {
            Transform value = row.Find("Value");
            if (value != null)
                valueText = value.GetComponent<TextMeshProUGUI>();
            if (valueText == null)
                valueText = row.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void ConfigureSliders()
    {
        ConfigureSlider(slider_Master);
        ConfigureSlider(slider_Ambient);
        ConfigureSlider(slider_Sfx);
    }

    private static void ConfigureSlider(Slider slider)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
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

        BindSlider(slider_Master, OnMasterChanged);
        BindSlider(slider_Ambient, OnAmbientChanged);
        BindSlider(slider_Sfx, OnSfxChanged);
        BindToggle(toggle_MasterMute, OnMasterMuteChanged);
        BindToggle(toggle_AmbientMute, OnAmbientMuteChanged);
        BindToggle(toggle_SfxMute, OnSfxMuteChanged);
    }

    private void UnbindListeners()
    {
        if (button_Back != null) button_Back.onClick.RemoveListener(OnClickBack);
        if (button_Reset != null) button_Reset.onClick.RemoveListener(OnClickReset);
        if (button_Confirm != null) button_Confirm.onClick.RemoveListener(OnClickConfirm);

        UnbindSlider(slider_Master, OnMasterChanged);
        UnbindSlider(slider_Ambient, OnAmbientChanged);
        UnbindSlider(slider_Sfx, OnSfxChanged);
        UnbindToggle(toggle_MasterMute, OnMasterMuteChanged);
        UnbindToggle(toggle_AmbientMute, OnAmbientMuteChanged);
        UnbindToggle(toggle_SfxMute, OnSfxMuteChanged);
    }

    private static void BindSlider(Slider s, UnityEngine.Events.UnityAction<float> cb)
    {
        if (s == null) return;
        s.onValueChanged.RemoveListener(cb);
        s.onValueChanged.AddListener(cb);
    }

    private static void UnbindSlider(Slider s, UnityEngine.Events.UnityAction<float> cb)
    {
        if (s != null) s.onValueChanged.RemoveListener(cb);
    }

    private static void BindToggle(Toggle t, UnityEngine.Events.UnityAction<bool> cb)
    {
        if (t == null) return;
        t.onValueChanged.RemoveListener(cb);
        t.onValueChanged.AddListener(cb);
    }

    private static void UnbindToggle(Toggle t, UnityEngine.Events.UnityAction<bool> cb)
    {
        if (t != null) t.onValueChanged.RemoveListener(cb);
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

        SetSlider(slider_Master, _draft.masterVolume);
        SetSlider(slider_Ambient, _draft.ambientVolume);
        SetSlider(slider_Sfx, _draft.sfxVolume);
        SetToggle(toggle_MasterMute, _draft.masterMuted);
        SetToggle(toggle_AmbientMute, _draft.ambientMuted);
        SetToggle(toggle_SfxMute, _draft.sfxMuted);

        RefreshValueTexts();
    }

    private void PullFromUi()
    {
        if (_draft == null) _draft = GameSettingsData.CreateDefault();

        if (slider_Master != null) _draft.masterVolume = slider_Master.value;
        if (slider_Ambient != null) _draft.ambientVolume = slider_Ambient.value;
        if (slider_Sfx != null) _draft.sfxVolume = slider_Sfx.value;
        if (toggle_MasterMute != null) _draft.masterMuted = toggle_MasterMute.isOn;
        if (toggle_AmbientMute != null) _draft.ambientMuted = toggle_AmbientMute.isOn;
        if (toggle_SfxMute != null) _draft.sfxMuted = toggle_SfxMute.isOn;
    }

    private void RefreshValueTexts()
    {
        SetValueText(text_MasterValue, _draft != null ? _draft.masterVolume : 1f);
        SetValueText(text_AmbientValue, _draft != null ? _draft.ambientVolume : 1f);
        SetValueText(text_SfxValue, _draft != null ? _draft.sfxVolume : 1f);
    }

    private static void SetSlider(Slider s, float v)
    {
        if (s != null) s.SetValueWithoutNotify(v);
    }

    private static void SetToggle(Toggle t, bool on)
    {
        if (t != null) t.SetIsOnWithoutNotify(on);
    }

    private static void SetValueText(TextMeshProUGUI tmp, float v)
    {
        if (tmp != null) tmp.text = v.ToString("0.00");
    }

    private void OnMasterChanged(float v)
    {
        if (_draft != null) _draft.masterVolume = v;
        SetValueText(text_MasterValue, v);
    }

    private void OnAmbientChanged(float v)
    {
        if (_draft != null) _draft.ambientVolume = v;
        SetValueText(text_AmbientValue, v);
    }

    private void OnSfxChanged(float v)
    {
        if (_draft != null) _draft.sfxVolume = v;
        SetValueText(text_SfxValue, v);
    }

    private void OnMasterMuteChanged(bool on)
    {
        if (_draft != null) _draft.masterMuted = on;
    }

    private void OnAmbientMuteChanged(bool on)
    {
        if (_draft != null) _draft.ambientMuted = on;
    }

    private void OnSfxMuteChanged(bool on)
    {
        if (_draft != null) _draft.sfxMuted = on;
    }

    private void OnClickBack()
    {
        if (_root != null) _root.ShowBrief();
        else gameObject.SetActive(false);
    }

    private void OnClickReset()
    {
        _draft = GameSettingsData.CreateSoundsDefault(
            SettingManager.Instance != null ? SettingManager.Instance.Settings : null);
        PushToUi();
    }

    private void OnClickConfirm()
    {
        PullFromUi();
        if (SettingManager.Instance != null)
            SettingManager.Instance.ApplySounds(_draft, save: true);

        if (_root != null) _root.ShowBrief();
        else gameObject.SetActive(false);
    }
}
