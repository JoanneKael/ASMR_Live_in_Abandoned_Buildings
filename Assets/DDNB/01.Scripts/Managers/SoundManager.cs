using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 사운드 재생 API.
/// Mixer:
///   Master
///   ├─ Ambient          ← UI 환경음 슬라이더
///   │   ├─ Ambient_Env     (희우웅·깜놀 2D)
///   │   └─ Ambient_Threat  (유닛 3D)
///   └─ SFX              ← UI 효과음 슬라이더
///       ├─ SFX_World       (일반 문 3D)
///       ├─ SFX_Player      (플레이어 2D)
///       ├─ SFX_ASMR        (ASMR 2D)
///       └─ SFX_UI          (ExitDoor·UI 2D)
/// </summary>
[DefaultExecutionOrder(-190)]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public const string AsmrResourcesRoot = "Sounds/ASMRSFX";
    public const string SfxResourcesRoot = "Sounds/SFX";
    public const string PlayerResourcesRoot = "Sounds/Player";
    public const string UnitResourcesRoot = "Sounds/Unit";
    private const string MixerResourcePath = "Audio/DDNB_Mixer";

    // 자주 쓰는 폴더 (Resources 기준)
    public const string PathPlayerFootsteps = "Sounds/Player/Footsteps_Player";
    public const string PathUnitFootsteps = "Sounds/Unit/Footsteps_Unit";
    public const string PathUnitGhost = "Sounds/Unit/Ghost";
    public const string PathSfxDoor = "Sounds/SFX/door";
    public const string PathSfxExitDoor = "Sounds/SFX/exitdoor";
    public const string PathSfxAmbient = "Sounds/SFX/Ambient";

    public const string GroupAmbient = "Ambient";
    public const string GroupAmbientEnv = "Ambient_Env";
    public const string GroupAmbientThreat = "Ambient_Threat";
    public const string GroupSfx = "SFX";
    public const string GroupSfxWorld = "SFX_World";
    public const string GroupSfxPlayer = "SFX_Player";
    public const string GroupSfxAsmr = "SFX_ASMR";
    public const string GroupSfxUi = "SFX_UI";

    /// <summary>라우팅용 버스 (부모 Ambient/SFX 아래 자식 그룹)</summary>
    public enum SoundBus
    {
        AmbientEnv,     // 2D 환경/스팅어
        AmbientThreat,  // 3D 유닛
        SfxWorld,       // 3D 일반 문
        SfxPlayer,      // 2D 플레이어
        SfxAsmr,        // 2D ASMR
        SfxUi           // 2D ExitDoor·UI
    }

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Parent Groups (UI 볼륨)")]
    [SerializeField] private AudioMixerGroup ambientGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Child Groups (밸런스)")]
    [SerializeField] private AudioMixerGroup ambientEnvGroup;
    [SerializeField] private AudioMixerGroup ambientThreatGroup;
    [SerializeField] private AudioMixerGroup sfxWorldGroup;
    [SerializeField] private AudioMixerGroup sfxPlayerGroup;
    [SerializeField] private AudioMixerGroup sfxAsmrGroup;
    [SerializeField] private AudioMixerGroup sfxUiGroup;

    [Header("Built-in 2D Sources (SoundManager DDOL)")]
    [SerializeField] private AudioSource asmrSource;
    [SerializeField] private AudioSource sfxUiSource;
    [SerializeField] private AudioSource sfxPlayerSource;
    [SerializeField] private AudioSource ambientEnvSource;

    private readonly Dictionary<string, AudioClip[]> _asmrClipCache = new Dictionary<string, AudioClip[]>();
    private readonly Dictionary<string, AudioClip[]> _sfxClipCache = new Dictionary<string, AudioClip[]>();
    private readonly Dictionary<string, AudioClip[]> _pathClipCache = new Dictionary<string, AudioClip[]>();
    private readonly HashSet<AudioSource> _loopingSources = new HashSet<AudioSource>();

    public AudioMixerGroup AmbientGroup => ambientGroup;
    public AudioMixerGroup SfxGroup => sfxGroup;
    public AudioMixerGroup AmbientEnvGroup => ResolveGroup(ambientEnvGroup, ambientGroup, GroupAmbientEnv, GroupAmbient);
    public AudioMixerGroup AmbientThreatGroup => ResolveGroup(ambientThreatGroup, ambientGroup, GroupAmbientThreat, GroupAmbient);
    public AudioMixerGroup SfxWorldGroup => ResolveGroup(sfxWorldGroup, sfxGroup, GroupSfxWorld, GroupSfx);
    public AudioMixerGroup SfxPlayerGroup => ResolveGroup(sfxPlayerGroup, sfxGroup, GroupSfxPlayer, GroupSfx);
    public AudioMixerGroup SfxAsmrGroup => ResolveGroup(sfxAsmrGroup, sfxGroup, GroupSfxAsmr, GroupSfx);
    public AudioMixerGroup SfxUiGroup => ResolveGroup(sfxUiGroup, sfxGroup, GroupSfxUi, GroupSfx);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject(nameof(SoundManager));
        DontDestroyOnLoad(go);
        go.AddComponent<SoundManager>();
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
        EnsureMixerGroups();
        EnsureAsmrSource();
        EnsureSfxUiSource();
        EnsureSfxPlayerSource();
        EnsureAmbientEnvSource();

        if (GetComponent<AmbientSoundDirector>() == null)
            gameObject.AddComponent<AmbientSoundDirector>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public AudioSource AsmrSource
    {
        get { EnsureAsmrSource(); return asmrSource; }
    }

    public AudioSource SfxUiSource
    {
        get { EnsureSfxUiSource(); return sfxUiSource; }
    }

    public AudioSource SfxPlayerSource
    {
        get { EnsureSfxPlayerSource(); return sfxPlayerSource; }
    }

    /// <summary>희우웅 등 2D 환경 루프/원샷용 (맵에 둘 필요 없음)</summary>
    public AudioSource AmbientEnvSource
    {
        get { EnsureAmbientEnvSource(); return ambientEnvSource; }
    }

    private void EnsureMixerGroups()
    {
        if (audioMixer == null)
            audioMixer = Resources.Load<AudioMixer>(MixerResourcePath);
        if (audioMixer == null) return;

        if (ambientGroup == null) ambientGroup = FindGroupExact(GroupAmbient);
        if (sfxGroup == null) sfxGroup = FindGroupExact(GroupSfx);

        if (ambientEnvGroup == null) ambientEnvGroup = FindGroupExact(GroupAmbientEnv);
        if (ambientThreatGroup == null) ambientThreatGroup = FindGroupExact(GroupAmbientThreat);
        if (sfxWorldGroup == null) sfxWorldGroup = FindGroupExact(GroupSfxWorld);
        if (sfxPlayerGroup == null) sfxPlayerGroup = FindGroupExact(GroupSfxPlayer);
        if (sfxAsmrGroup == null) sfxAsmrGroup = FindGroupExact(GroupSfxAsmr);
        if (sfxUiGroup == null) sfxUiGroup = FindGroupExact(GroupSfxUi);
    }

    private AudioMixerGroup FindGroupExact(string name)
    {
        if (audioMixer == null || string.IsNullOrEmpty(name)) return null;
        AudioMixerGroup[] groups = audioMixer.FindMatchingGroups(name);
        if (groups == null) return null;
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null && groups[i].name == name)
                return groups[i];
        }
        return null;
    }

    private AudioMixerGroup ResolveGroup(
        AudioMixerGroup preferred,
        AudioMixerGroup parentFallback,
        string preferredName,
        string parentName)
    {
        if (preferred != null) return preferred;
        EnsureMixerGroups();
        AudioMixerGroup found = FindGroupExact(preferredName);
        if (found != null) return found;
        if (parentFallback != null) return parentFallback;
        return FindGroupExact(parentName);
    }

    private AudioMixerGroup GetBusGroup(SoundBus bus)
    {
        switch (bus)
        {
            case SoundBus.AmbientEnv: return AmbientEnvGroup;
            case SoundBus.AmbientThreat: return AmbientThreatGroup;
            case SoundBus.SfxWorld: return SfxWorldGroup;
            case SoundBus.SfxPlayer: return SfxPlayerGroup;
            case SoundBus.SfxAsmr: return SfxAsmrGroup;
            case SoundBus.SfxUi: return SfxUiGroup;
            default: return SfxGroup;
        }
    }

    private void EnsureAsmrSource()
    {
        if (asmrSource != null) return;
        asmrSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(asmrSource, 0f, SoundBus.SfxAsmr, 1f, 25f);
    }

    private void EnsureSfxUiSource()
    {
        if (sfxUiSource != null) return;
        sfxUiSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(sfxUiSource, 0f, SoundBus.SfxUi, 1f, 25f);
    }

    private void EnsureSfxPlayerSource()
    {
        if (sfxPlayerSource != null) return;
        sfxPlayerSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(sfxPlayerSource, 0f, SoundBus.SfxPlayer, 1f, 25f);
    }

    private void EnsureAmbientEnvSource()
    {
        if (ambientEnvSource != null) return;
        ambientEnvSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(ambientEnvSource, 0f, SoundBus.AmbientEnv, 1f, 50f);
    }

    /// <summary>일반 문 3D → SFX_World</summary>
    public void ConfigureWorldSfxSource(AudioSource source, float minDist = 1.5f, float maxDist = 12f)
    {
        ConfigureSource(source, 1f, SoundBus.SfxWorld, minDist, maxDist);
    }

    /// <summary>유닛 위협 3D → Ambient_Threat</summary>
    public void ConfigureThreatSource(AudioSource source, float minDist = 2f, float maxDist = 12f)
    {
        ConfigureSource(source, 1f, SoundBus.AmbientThreat, minDist, maxDist);
    }

    /// <summary>구 API 호환 → Threat</summary>
    public void ConfigureAmbientSource(AudioSource source, float minDist = 2f, float maxDist = 12f)
    {
        ConfigureThreatSource(source, minDist, maxDist);
    }

    /// <summary>플레이어 2D → SFX_Player</summary>
    public void ConfigurePlayerSfxSource(AudioSource source)
    {
        ConfigureSource(source, 0f, SoundBus.SfxPlayer, 1f, 25f);
    }

    /// <summary>UI/ExitDoor 2D → SFX_UI</summary>
    public void ConfigureUiSfxSource(AudioSource source)
    {
        ConfigureSource(source, 0f, SoundBus.SfxUi, 1f, 25f);
    }

    public void ConfigureSource(AudioSource source, float spatialBlend, SoundBus bus, float minDist, float maxDist)
    {
        if (source == null) return;
        EnsureMixerGroups();

        source.playOnAwake = false;
        source.spatialBlend = Mathf.Clamp01(spatialBlend);
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDist;
        source.maxDistance = maxDist;
        source.dopplerLevel = 0f;

        AudioMixerGroup group = GetBusGroup(bus);
        if (group != null)
            source.outputAudioMixerGroup = group;
    }

    #region Core Play API

    public void PlayOneShot(AudioSource source, AudioClip clip, float volumeScale = 1f)
    {
        if (source == null || clip == null) return;
        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayLoop(AudioSource source, AudioClip clip, float volume = 1f)
    {
        if (source == null || clip == null) return;
        source.Stop();
        source.clip = clip;
        source.loop = true;
        source.volume = Mathf.Clamp01(volume);
        source.Play();
        _loopingSources.Add(source);
    }

    public void Stop(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.loop = false;
        source.clip = null;
        _loopingSources.Remove(source);
    }

    public void StopAllLoops()
    {
        if (_loopingSources.Count == 0) return;
        List<AudioSource> snapshot = new List<AudioSource>(_loopingSources);
        for (int i = 0; i < snapshot.Count; i++)
            Stop(snapshot[i]);
    }

    public void PlayUiOneShot(AudioClip clip, float volumeScale = 1f)
    {
        PlayOneShot(SfxUiSource, clip, volumeScale);
    }

    public bool PlayUiFromCategory(string category, float volumeScale = 1f)
    {
        AudioClip clip = GetRandomSfxClip(category);
        if (clip == null) return false;
        PlayUiOneShot(clip, volumeScale);
        return true;
    }

    public void PlayPlayerOneShot(AudioClip clip, float volumeScale = 1f)
    {
        PlayOneShot(SfxPlayerSource, clip, volumeScale);
    }

    public bool PlayPlayerFromFolder(string resourcesFolder, float volumeScale = 1f)
    {
        AudioClip clip = GetRandomClipAt(resourcesFolder);
        if (clip == null) return false;
        PlayPlayerOneShot(clip, volumeScale);
        return true;
    }

    public void PlayAmbientEnvLoop(AudioClip clip, float volume = 1f)
    {
        PlayLoop(AmbientEnvSource, clip, volume);
    }

    public void PlayAmbientEnvOneShot(AudioClip clip, float volumeScale = 1f)
    {
        PlayOneShot(AmbientEnvSource, clip, volumeScale);
    }

    public void StopAmbientEnvLoop()
    {
        Stop(AmbientEnvSource);
    }

    #endregion

    #region ASMR

    public bool PlayAsmrLoop(string category, AudioSource source = null, float volume = 1f)
    {
        AudioClip clip = GetRandomAsmrClip(category);
        if (clip == null) return false;
        PlayLoop(source != null ? source : AsmrSource, clip, volume);
        return true;
    }

    public void StopAsmrLoop(AudioSource source = null)
    {
        Stop(source != null ? source : AsmrSource);
    }

    public bool PlayAsmrOneShot(string category, AudioSource source = null, float volumeScale = 1f)
    {
        AudioClip clip = GetRandomAsmrClip(category);
        if (clip == null) return false;
        PlayOneShot(source != null ? source : AsmrSource, clip, volumeScale);
        return true;
    }

    public AudioClip[] LoadAsmrClips(string category) =>
        LoadClipsCached(_asmrClipCache, AsmrResourcesRoot, category);

    public AudioClip GetRandomAsmrClip(string category)
    {
        AudioClip[] clips = LoadAsmrClips(category);
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    public static string ResolveAsmrCategory(InteractableASMR asmr)
    {
        if (asmr == null) return null;
        if (!string.IsNullOrWhiteSpace(asmr.AsmrSfxCategory))
            return asmr.AsmrSfxCategory.Trim();
        if (asmr.Data != null && !string.IsNullOrWhiteSpace(asmr.Data.interactionSoundName))
            return asmr.Data.interactionSoundName.Trim();

        string name = asmr.Data != null ? asmr.Data.itemName : asmr.name;
        if (string.IsNullOrWhiteSpace(name)) return null;
        string lower = name.ToLowerInvariant();
        if (lower.Contains("keyboard")) return "keyboard";
        if (lower.Contains("box")) return "box";
        if (lower.Contains("notebook") || lower.Contains("book")) return "book";
        if (lower.Contains("toy")) return "toyblock";
        if (lower.Contains("fabric") || lower.Contains("cloth")) return "fabric";
        if (lower.Contains("mug") || lower.Contains("cup")) return "mugcup";
        if (lower.Contains("bottle")) return "bottlewithliquid";
        if (lower.Contains("typewriter") || lower.Contains("rifle")) return "typewriter";
        return null;
    }

    #endregion

    #region SFX Resources

    public AudioClip[] LoadSfxClips(string category) =>
        LoadClipsCached(_sfxClipCache, SfxResourcesRoot, category);

    public AudioClip GetRandomSfxClip(string category)
    {
        AudioClip[] clips = LoadSfxClips(category);
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    /// <summary>Resources/{folder} 전체 클립</summary>
    public AudioClip[] LoadClipsAt(string resourcesFolder) =>
        LoadClipsAtPathCached(resourcesFolder);

    public AudioClip GetRandomClipAt(string resourcesFolder)
    {
        AudioClip[] clips = LoadClipsAt(resourcesFolder);
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    /// <summary>폴더에서 이름이 정확히 일치하는 클립 (예: door_open)</summary>
    public AudioClip GetNamedClip(string resourcesFolder, string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName)) return null;
        AudioClip[] clips = LoadClipsAt(resourcesFolder);
        if (clips == null || clips.Length == 0) return null;

        string want = clipName.Trim();
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == want)
                return clips[i];
        }

        // 부분 일치 폴백
        string wantLower = want.ToLowerInvariant();
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name.ToLowerInvariant().Contains(wantLower))
                return clips[i];
        }

        return null;
    }

    /// <summary>이름 prefix로 필터한 뒤 랜덤 (예: cinematic_, door_A_)</summary>
    public AudioClip GetRandomClipWithPrefix(string resourcesFolder, string namePrefix)
    {
        AudioClip[] clips = LoadClipsAt(resourcesFolder);
        if (clips == null || clips.Length == 0) return null;
        if (string.IsNullOrWhiteSpace(namePrefix))
            return clips[Random.Range(0, clips.Length)];

        string prefix = namePrefix.Trim().ToLowerInvariant();
        List<AudioClip> matched = null;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null) continue;
            if (!clips[i].name.ToLowerInvariant().StartsWith(prefix)) continue;
            if (matched == null) matched = new List<AudioClip>();
            matched.Add(clips[i]);
        }

        if (matched == null || matched.Count == 0) return null;
        return matched[Random.Range(0, matched.Count)];
    }

    private static AudioClip[] LoadClipsCached(
        Dictionary<string, AudioClip[]> cache,
        string root,
        string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return System.Array.Empty<AudioClip>();

        string key = category.Trim().ToLowerInvariant();
        if (cache.TryGetValue(key, out AudioClip[] cached))
            return cached;

        string path = $"{root}/{key}";
        AudioClip[] clips = Resources.LoadAll<AudioClip>(path);
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[SoundManager] 클립 없음: Resources/{path}");
            clips = System.Array.Empty<AudioClip>();
        }

        cache[key] = clips;
        return clips;
    }

    private AudioClip[] LoadClipsAtPathCached(string resourcesFolder)
    {
        if (string.IsNullOrWhiteSpace(resourcesFolder))
            return System.Array.Empty<AudioClip>();

        string key = resourcesFolder.Trim().Replace('\\', '/').Trim('/');
        string cacheKey = key.ToLowerInvariant();
        if (_pathClipCache.TryGetValue(cacheKey, out AudioClip[] cached))
            return cached;

        AudioClip[] clips = Resources.LoadAll<AudioClip>(key);
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[SoundManager] 클립 없음: Resources/{key}");
            clips = System.Array.Empty<AudioClip>();
        }

        _pathClipCache[cacheKey] = clips;
        return clips;
    }

    #endregion
}
