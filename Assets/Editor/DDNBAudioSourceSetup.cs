using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Player / Unit / 일반문(3D) AudioSource 배치.
/// ExitDoor는 2D UI라 SoundManager.SfxUiSource를 쓰므로 씬 소스 불필요.
/// </summary>
public static class DDNBAudioSourceSetup
{
    private const string PlayerPrefabPath = "Assets/DDNB/Prefabs/Player/Player.prefab";
    private const string UnitPrefabPath = "Assets/DDNB/Prefabs/Units/Unit.prefab";
    private const string MixerPath = DDNBAudioMixerSetup.MixerPath;

    private const string MenuAll = "Tools/DDNB/Setup AudioSources (Player/Unit/Doors)";
    private const string MenuDoors = "Tools/DDNB/Setup Door AudioSources In Open Scene";

    [MenuItem(MenuAll)]
    public static void SetupAll()
    {
        AudioMixerGroup sfxPlayer = FindGroup("SFX_Player") ?? FindGroup("SFX");
        AudioMixerGroup sfxUi = FindGroup("SFX_UI") ?? FindGroup("SFX");
        AudioMixerGroup sfxWorld = FindGroup("SFX_World") ?? FindGroup("SFX");
        AudioMixerGroup threat = FindGroup("Ambient_Threat") ?? FindGroup("Ambient");

        int player = SetupPlayerPrefab(sfxPlayer, sfxUi);
        int unit = SetupUnitPrefab(threat);
        int doors = SetupDoorsInOpenScene(sfxWorld);

        EditorUtility.DisplayDialog(
            "DDNB AudioSources",
            $"Player: {player} (SFX_Player / SFX_UI)\n" +
            $"Unit: {unit} (Ambient_Threat 3D)\n" +
            $"일반 Door: {doors} (SFX_World 3D)\n" +
            "ExitDoor: SoundManager 2D UI 소스 사용 (씬 소스 없음)\n\n" +
            (sfxWorld == null
                ? "경고: Mixer 그룹을 못 찾았습니다. Create DDNB Audio Mixer 먼저."
                : "Mixer 자식 그룹 연결 완료."),
            "OK");
    }

    [MenuItem(MenuDoors)]
    public static void SetupDoorsOnly()
    {
        AudioMixerGroup sfxWorld = FindGroup("SFX_World") ?? FindGroup("SFX");
        int doors = SetupDoorsInOpenScene(sfxWorld);
        EditorUtility.DisplayDialog("DDNB Door Audio", $"InteractableDoor(일반 문) {doors}개 → SFX_World 3D", "OK");
    }

    private static AudioMixerGroup FindGroup(string groupName)
    {
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null) return null;

        AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
        if (groups == null || groups.Length == 0) return null;

        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null && groups[i].name == groupName)
                return groups[i];
        }

        return groups[0];
    }

    private static int SetupPlayerPrefab(AudioMixerGroup sfxPlayer, AudioMixerGroup sfxUi)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        if (root == null)
        {
            Debug.LogWarning($"[DDNBAudioSourceSetup] Player 프리팹 없음: {PlayerPrefabPath}");
            return 0;
        }

        int count = 0;
        try
        {
            Undo.RegisterFullObjectHierarchyUndo(root, "Setup Player AudioSources");

            AudioSource playerSfx = EnsureNamedSource(root.transform, "Audio_SFX", out bool createdSfx);
            ConfigureSource(playerSfx, 0f, sfxPlayer, 1f, 25f);
            if (createdSfx) count++;

            AudioSource playerUi = EnsureNamedSource(root.transform, "Audio_UI", out bool createdUi);
            ConfigureSource(playerUi, 0f, sfxUi, 1f, 25f);
            if (createdUi) count++;

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return count;
    }

    private static int SetupUnitPrefab(AudioMixerGroup threatGroup)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(UnitPrefabPath);
        if (root == null)
        {
            Debug.LogWarning($"[DDNBAudioSourceSetup] Unit 프리팹 없음: {UnitPrefabPath}");
            return 0;
        }

        int count = 0;
        try
        {
            Undo.RegisterFullObjectHierarchyUndo(root, "Setup Unit AudioSources");

            AudioSource vocal = EnsureNamedSource(root.transform, "Audio_Threat", out bool created);
            ConfigureSource(vocal, 1f, threatGroup, 2f, 28f);
            if (created) count++;

            if (root.GetComponent<UnitAudio>() == null)
                Undo.AddComponent<UnitAudio>(root);

            PrefabUtility.SaveAsPrefabAsset(root, UnitPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return count;
    }

    private static int SetupDoorsInOpenScene(AudioMixerGroup sfxWorld)
    {
        InteractableDoor[] doors = Object.FindObjectsByType<InteractableDoor>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int count = 0;
        for (int i = 0; i < doors.Length; i++)
        {
            InteractableDoor door = doors[i];
            if (door == null) continue;

            Undo.RegisterCompleteObjectUndo(door.gameObject, "Setup Door AudioSource");

            AudioSource source = door.GetComponent<AudioSource>();
            if (source == null)
            {
                source = Undo.AddComponent<AudioSource>(door.gameObject);
                count++;
            }

            ConfigureSource(source, 1f, sfxWorld, 1.5f, 22f);
            EditorUtility.SetDirty(door.gameObject);
        }

        return doors.Length;
    }

    private static AudioSource EnsureNamedSource(Transform root, string childName, out bool created)
    {
        created = false;
        Transform existing = root.Find(childName);
        if (existing != null)
        {
            AudioSource src = existing.GetComponent<AudioSource>();
            if (src == null)
            {
                src = Undo.AddComponent<AudioSource>(existing.gameObject);
                created = true;
            }
            return src;
        }

        GameObject go = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(go, "Create Audio child");
        go.transform.SetParent(root, false);
        go.transform.localPosition = Vector3.zero;
        AudioSource audio = Undo.AddComponent<AudioSource>(go);
        created = true;
        return audio;
    }

    private static void ConfigureSource(
        AudioSource source,
        float spatialBlend,
        AudioMixerGroup group,
        float minDist,
        float maxDist)
    {
        if (source == null) return;

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDist;
        source.maxDistance = maxDist;
        source.dopplerLevel = 0f;
        source.priority = spatialBlend > 0.5f ? 128 : 64;
        if (group != null)
            source.outputAudioMixerGroup = group;
    }
}
