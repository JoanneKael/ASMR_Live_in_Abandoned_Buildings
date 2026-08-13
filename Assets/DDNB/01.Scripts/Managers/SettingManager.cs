using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 화면/그래픽·사운드·마우스 감도 설정을 저장·불러오기·적용합니다.
/// Mixer: Master / Ambient / SFX (Resources/Audio/DDNB_Mixer)
/// </summary>
[DefaultExecutionOrder(-200)]
public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    private const string PrefsKey = "DDNB_GameSettings";
    private const string MixerResourcePath = "Audio/DDNB_Mixer";

    public const float DefaultMouseSensitivity = 1.5f;
    public const float MinMouseSensitivity = 0f;
    public const float MaxMouseSensitivity = 3f;

    public static readonly int[] FrameRateOptions = { 60, 120, 144 };

    public static readonly string[] DisplayModeLabels =
    {
        "테두리 없는 전체화면",
        "테두리 있는 전체화면",
        "창모드"
    };

    public static readonly string[] QualityLabels =
    {
        "낮음",
        "중간",
        "높음"
    };

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParam = DDNBAudioMixerSetupNames.Master;
    [SerializeField] private string sfxVolumeParam = DDNBAudioMixerSetupNames.Sfx;
    [SerializeField] private string ambientVolumeParam = DDNBAudioMixerSetupNames.Ambient;

    private GameSettingsData _settings = new GameSettingsData();

    public GameSettingsData Settings => _settings.Clone();

    public int DisplayModeIndex => _settings.displayModeIndex;
    public int TargetFrameRate => _settings.targetFrameRate;
    public int QualityLevel => _settings.qualityLevel;
    public bool VSyncEnabled => _settings.vSyncEnabled;
    public float MouseSensitivity => _settings.mouseSensitivity;

    public float MasterVolume => _settings.masterVolume;
    public float SfxVolume => _settings.sfxVolume;
    public float AmbientVolume => _settings.ambientVolume;
    public bool MasterMuted => _settings.masterMuted;
    public bool SfxMuted => _settings.sfxMuted;
    public bool AmbientMuted => _settings.ambientMuted;

    public AudioMixer AudioMixer => audioMixer;

    public event Action OnSettingsApplied;
    public event Action<float> OnMouseSensitivityChanged;
    public event Action OnVolumesChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        var go = new GameObject(nameof(SettingManager));
        DontDestroyOnLoad(go);
        go.AddComponent<SettingManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureMixerLoaded();
        Load();
        ApplyAll(_settings, raiseEvents: false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void EnsureMixerLoaded()
    {
        if (audioMixer != null) return;
        audioMixer = Resources.Load<AudioMixer>(MixerResourcePath);
        if (audioMixer == null)
            Debug.LogWarning($"[SettingManager] Mixer 없음: Resources/{MixerResourcePath}. Tools/DDNB/Create Or Refresh DDNB Audio Mixer 실행 필요.");
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(PrefsKey))
        {
            _settings = GameSettingsData.CreateDefault();
            _settings.qualityLevel = QualitySettingsToUiLevel(QualitySettings.GetQualityLevel());
            ClampSettings(ref _settings);
            return;
        }

        string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            _settings = GameSettingsData.CreateDefault();
            _settings.qualityLevel = QualitySettingsToUiLevel(QualitySettings.GetQualityLevel());
            ClampSettings(ref _settings);
            return;
        }

        try
        {
            _settings = JsonUtility.FromJson<GameSettingsData>(json);
            if (_settings == null)
            {
                _settings = GameSettingsData.CreateDefault();
                _settings.qualityLevel = QualitySettingsToUiLevel(QualitySettings.GetQualityLevel());
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SettingManager] 설정 로드 실패, 기본값 사용: {e.Message}");
            _settings = GameSettingsData.CreateDefault();
            _settings.qualityLevel = QualitySettingsToUiLevel(QualitySettings.GetQualityLevel());
        }

        ClampSettings(ref _settings);
    }

    public void Save()
    {
        ClampSettings(ref _settings);
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(_settings));
        PlayerPrefs.Save();
    }

    public void Apply(GameSettingsData data, bool save = true)
    {
        if (data == null) return;

        _settings = data.Clone();
        ClampSettings(ref _settings);
        ApplyAll(_settings, raiseEvents: true);

        if (save)
            Save();
    }

    public void ApplyGraphics(GameSettingsData data, bool save = true)
    {
        if (data == null) return;

        _settings.displayModeIndex = data.displayModeIndex;
        _settings.targetFrameRate = data.targetFrameRate;
        _settings.qualityLevel = data.qualityLevel;
        _settings.vSyncEnabled = data.vSyncEnabled;
        ClampSettings(ref _settings);

        ApplyDisplay(_settings);
        ApplyFrameRate(_settings);
        ApplyQuality(_settings);
        ApplyVSync(_settings);

        OnSettingsApplied?.Invoke();
        if (save) Save();
    }

    public void ApplyInput(GameSettingsData data, bool save = true)
    {
        if (data == null) return;

        _settings.mouseSensitivity = data.mouseSensitivity;
        ClampSettings(ref _settings);

        OnMouseSensitivityChanged?.Invoke(_settings.mouseSensitivity);
        OnSettingsApplied?.Invoke();
        if (save) Save();
    }

    public void ApplySounds(GameSettingsData data, bool save = true)
    {
        if (data == null) return;

        _settings.masterVolume = data.masterVolume;
        _settings.sfxVolume = data.sfxVolume;
        _settings.ambientVolume = data.ambientVolume;
        _settings.masterMuted = data.masterMuted;
        _settings.sfxMuted = data.sfxMuted;
        _settings.ambientMuted = data.ambientMuted;
        ClampSettings(ref _settings);

        ApplyVolumes(_settings);
        OnVolumesChanged?.Invoke();
        OnSettingsApplied?.Invoke();
        if (save) Save();
    }

    public void ResetToDefault(bool save = true)
    {
        Apply(GameSettingsData.CreateDefault(), save);
    }

    public void SetMouseSensitivity(float value, bool save = true)
    {
        _settings.mouseSensitivity = Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
        OnMouseSensitivityChanged?.Invoke(_settings.mouseSensitivity);
        OnSettingsApplied?.Invoke();
        if (save) Save();
    }

    public static int QualityUiToSettingsLevel(int uiIndex)
    {
        int max = Mathf.Max(0, QualitySettings.names.Length - 1);
        float t = Mathf.Clamp(uiIndex, 0, 2) / 2f;
        return Mathf.RoundToInt(t * max);
    }

    public static int QualitySettingsToUiLevel(int settingsLevel)
    {
        int max = Mathf.Max(0, QualitySettings.names.Length - 1);
        if (max <= 0) return 0;

        float t = Mathf.Clamp01(settingsLevel / (float)max);
        return Mathf.RoundToInt(t * 2f);
    }

    public static FullScreenMode IndexToFullScreenMode(int index)
    {
        switch (Mathf.Clamp(index, 0, 2))
        {
            case 0: return FullScreenMode.FullScreenWindow;
            case 1: return FullScreenMode.ExclusiveFullScreen;
            default: return FullScreenMode.Windowed;
        }
    }

    public static int FullScreenModeToIndex(FullScreenMode mode)
    {
        switch (mode)
        {
            case FullScreenMode.FullScreenWindow: return 0;
            case FullScreenMode.ExclusiveFullScreen: return 1;
            default: return 2;
        }
    }

    private void ApplyAll(GameSettingsData data, bool raiseEvents)
    {
        ApplyDisplay(data);
        ApplyFrameRate(data);
        ApplyQuality(data);
        ApplyVSync(data);
        ApplyVolumes(data);

        if (!raiseEvents) return;

        OnMouseSensitivityChanged?.Invoke(data.mouseSensitivity);
        OnVolumesChanged?.Invoke();
        OnSettingsApplied?.Invoke();
    }

    private static void ApplyDisplay(GameSettingsData data)
    {
        Screen.fullScreenMode = IndexToFullScreenMode(data.displayModeIndex);
    }

    private static void ApplyFrameRate(GameSettingsData data)
    {
        Application.targetFrameRate = data.targetFrameRate;
    }

    private static void ApplyQuality(GameSettingsData data)
    {
        int level = QualityUiToSettingsLevel(data.qualityLevel);
        QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);
    }

    private static void ApplyVSync(GameSettingsData data)
    {
        QualitySettings.vSyncCount = data.vSyncEnabled ? 1 : 0;
    }

    private void ApplyVolumes(GameSettingsData data)
    {
        EnsureMixerLoaded();

        if (audioMixer == null)
        {
            // Mixer 없으면 마스터만 AudioListener로 근사
            AudioListener.volume = data.masterMuted ? 0f : Mathf.Clamp01(data.masterVolume);
            return;
        }

        SetMixerVolume(masterVolumeParam, data.masterMuted ? 0f : data.masterVolume);
        SetMixerVolume(ambientVolumeParam, data.ambientMuted ? 0f : data.ambientVolume);
        SetMixerVolume(sfxVolumeParam, data.sfxMuted ? 0f : data.sfxVolume);
    }

    private void SetMixerVolume(string param, float linear01)
    {
        if (string.IsNullOrEmpty(param) || audioMixer == null) return;
        float dB = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
        audioMixer.SetFloat(param, dB);
    }

    private static void ClampSettings(ref GameSettingsData data)
    {
        data.displayModeIndex = Mathf.Clamp(data.displayModeIndex, 0, 2);

        int nearest = FrameRateOptions[0];
        int bestDist = int.MaxValue;
        foreach (int fps in FrameRateOptions)
        {
            int dist = Mathf.Abs(fps - data.targetFrameRate);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = fps;
            }
        }
        data.targetFrameRate = nearest;

        data.qualityLevel = Mathf.Clamp(data.qualityLevel, 0, 2);

        if (data.mouseSensitivity > MaxMouseSensitivity)
            data.mouseSensitivity = DefaultMouseSensitivity;
        else
            data.mouseSensitivity = Mathf.Clamp(data.mouseSensitivity, MinMouseSensitivity, MaxMouseSensitivity);

        data.masterVolume = Mathf.Clamp01(data.masterVolume);
        data.sfxVolume = Mathf.Clamp01(data.sfxVolume);
        data.ambientVolume = Mathf.Clamp01(data.ambientVolume);
    }
}

/// <summary>런타임에서 Editor 상수 이름을 쓰기 위한 미러 (에디터 클래스 의존 방지)</summary>
public static class DDNBAudioMixerSetupNames
{
    public const string Master = "MasterVolume";
    public const string Ambient = "AmbientVolume";
    public const string Sfx = "SFXVolume";
}

[Serializable]
public class GameSettingsData
{
    public int displayModeIndex = 0;
    public int targetFrameRate = 60;
    public int qualityLevel = 1;
    public bool vSyncEnabled = true;

    public float masterVolume = 1f;
    public float sfxVolume = 1f;
    public float ambientVolume = 1f;
    public bool masterMuted;
    public bool sfxMuted;
    public bool ambientMuted;

    public float mouseSensitivity = SettingManager.DefaultMouseSensitivity;

    public static GameSettingsData CreateDefault()
    {
        return new GameSettingsData
        {
            displayModeIndex = SettingManager.FullScreenModeToIndex(FullScreenMode.FullScreenWindow),
            targetFrameRate = 60,
            qualityLevel = 1,
            vSyncEnabled = true,
            masterVolume = 1f,
            sfxVolume = 1f,
            ambientVolume = 1f,
            masterMuted = false,
            sfxMuted = false,
            ambientMuted = false,
            mouseSensitivity = SettingManager.DefaultMouseSensitivity
        };
    }

    public static GameSettingsData CreateGraphicsDefault(GameSettingsData keepOthers = null)
    {
        var data = keepOthers != null ? keepOthers.Clone() : CreateDefault();
        data.displayModeIndex = SettingManager.FullScreenModeToIndex(FullScreenMode.FullScreenWindow);
        data.targetFrameRate = 60;
        data.qualityLevel = 1;
        data.vSyncEnabled = true;
        return data;
    }

    public static GameSettingsData CreateInputDefault(GameSettingsData keepOthers = null)
    {
        var data = keepOthers != null ? keepOthers.Clone() : CreateDefault();
        data.mouseSensitivity = SettingManager.DefaultMouseSensitivity;
        return data;
    }

    public static GameSettingsData CreateSoundsDefault(GameSettingsData keepOthers = null)
    {
        var data = keepOthers != null ? keepOthers.Clone() : CreateDefault();
        data.masterVolume = 1f;
        data.sfxVolume = 1f;
        data.ambientVolume = 1f;
        data.masterMuted = false;
        data.sfxMuted = false;
        data.ambientMuted = false;
        return data;
    }

    public GameSettingsData Clone()
    {
        return new GameSettingsData
        {
            displayModeIndex = displayModeIndex,
            targetFrameRate = targetFrameRate,
            qualityLevel = qualityLevel,
            vSyncEnabled = vSyncEnabled,
            masterVolume = masterVolume,
            sfxVolume = sfxVolume,
            ambientVolume = ambientVolume,
            masterMuted = masterMuted,
            sfxMuted = sfxMuted,
            ambientMuted = ambientMuted,
            mouseSensitivity = mouseSensitivity
        };
    }
}
