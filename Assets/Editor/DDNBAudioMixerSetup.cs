using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// DDNB AudioMixer 생성 (internal AudioMixerController는 리플렉션/YAML로만 다룸).
/// Master → Ambient / SFX + Exposed Volume.
/// </summary>
public static class DDNBAudioMixerSetup
{
    public const string MixerFolder = "Assets/Resources/Audio";
    public const string MixerPath = MixerFolder + "/DDNB_Mixer.mixer";

    public const string ParamMaster = "MasterVolume";
    public const string ParamAmbient = "AmbientVolume";
    public const string ParamSfx = "SFXVolume";

    private const string MenuCreate = "Tools/DDNB/Create Or Refresh DDNB Audio Mixer";

    [MenuItem(MenuCreate)]
    public static void CreateOrRefreshMixer()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(MixerFolder);

        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
        {
            if (!TryCreateMixerViaReflection(out mixer) || mixer == null)
            {
                WriteFallbackMixerYaml();
                AssetDatabase.ImportAsset(MixerPath);
                mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            }
        }

        if (mixer == null)
        {
            EditorUtility.DisplayDialog("DDNB Mixer", "Mixer 생성 실패.", "OK");
            return;
        }

        // Ambient / SFX 자식 그룹 + Expose (가능하면)
        TryEnsureChildGroupsAndExpose(mixer);

        EditorUtility.SetDirty(mixer);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = mixer;

        bool hasAmbient = HasGroup(mixer, "Ambient");
        bool hasSfx = HasGroup(mixer, "SFX");

        string extra = string.Empty;
        if (!hasAmbient || !hasSfx)
        {
            extra =
                "\n\n※ 그룹이 비어 있으면 Mixer 창에서 Master 우클릭 → Add child group으로\n" +
                "Ambient, SFX를 만들고, 각 Volume 우클릭 → Expose to script 후\n" +
                $"이름을 {ParamMaster} / {ParamAmbient} / {ParamSfx} 로 바꿔 주세요.";
        }

        EditorUtility.DisplayDialog(
            "DDNB Mixer",
            $"완료: {MixerPath}\n\n" +
            "구조:\n" +
            "  Master\n" +
            "  ├─ Ambient ← UI 환경음\n" +
            "  │   ├─ Ambient_Env    (희우웅·깜놀 2D)\n" +
            "  │   └─ Ambient_Threat (유닛 3D, 밸런스용)\n" +
            "  └─ SFX ← UI 효과음\n" +
            "      ├─ SFX_World  (일반 문 3D)\n" +
            "      ├─ SFX_Player\n" +
            "      ├─ SFX_ASMR\n" +
            "      └─ SFX_UI     (ExitDoor·UI 2D)\n" +
            $"Exposed: {ParamMaster}, {ParamAmbient}, {ParamSfx}" +
            extra +
            "\n\n다음: Setup AudioSources / Setup UI_Setting Sounds Rows",
            "OK");
    }

    private static bool TryCreateMixerViaReflection(out AudioMixer mixer)
    {
        mixer = null;
        try
        {
            Type controllerType = FindEditorType("UnityEditor.Audio.AudioMixerController");
            if (controllerType == null) return false;

            MethodInfo create = controllerType.GetMethod(
                "CreateMixerControllerAtPath",
                BindingFlags.Public | BindingFlags.Static);
            if (create == null) return false;

            object created = create.Invoke(null, new object[] { MixerPath });
            mixer = created as AudioMixer;
            return mixer != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DDNBAudioMixerSetup] Reflection create 실패: {e.Message}");
            return false;
        }
    }

    private static void TryEnsureChildGroupsAndExpose(AudioMixer mixer)
    {
        try
        {
            Type controllerType = FindEditorType("UnityEditor.Audio.AudioMixerController");
            Type groupType = FindEditorType("UnityEditor.Audio.AudioMixerGroupController");
            if (controllerType == null || groupType == null || !controllerType.IsInstanceOfType(mixer))
            {
                Debug.LogWarning("[DDNBAudioMixerSetup] Controller 타입 접근 불가 — Mixer 창에서 Ambient/SFX를 수동 추가하세요.");
                return;
            }

            object controller = mixer;
            PropertyInfo masterProp = controllerType.GetProperty("masterGroup", BindingFlags.Public | BindingFlags.Instance);
            object master = masterProp?.GetValue(controller);
            if (master == null) return;

            // Master 이름
            PropertyInfo nameProp = groupType.GetProperty("name");
            nameProp?.SetValue(master, "Master");

            object ambient = FindOrCreateChildReflect(controllerType, groupType, controller, master, "Ambient");
            object sfx = FindOrCreateChildReflect(controllerType, groupType, controller, master, "SFX");

            // Ambient 하위: 환경(2D) / 유닛 위협(3D) — UI는 Ambient 부모만 조절, 밸런스는 자식
            FindOrCreateChildReflect(controllerType, groupType, controller, ambient, "Ambient_Env");
            FindOrCreateChildReflect(controllerType, groupType, controller, ambient, "Ambient_Threat");

            // SFX 하위: 월드문(3D) / 플레이어 / ASMR / UI·ExitDoor(2D)
            FindOrCreateChildReflect(controllerType, groupType, controller, sfx, "SFX_World");
            FindOrCreateChildReflect(controllerType, groupType, controller, sfx, "SFX_Player");
            FindOrCreateChildReflect(controllerType, groupType, controller, sfx, "SFX_ASMR");
            FindOrCreateChildReflect(controllerType, groupType, controller, sfx, "SFX_UI");

            ExposeVolumeReflect(controllerType, groupType, controller, master, ParamMaster);
            ExposeVolumeReflect(controllerType, groupType, controller, ambient, ParamAmbient);
            ExposeVolumeReflect(controllerType, groupType, controller, sfx, ParamSfx);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DDNBAudioMixerSetup] 그룹/Expose 자동화 실패 (수동 가능): {e.Message}");
        }
    }

    private static object FindOrCreateChildReflect(
        Type controllerType,
        Type groupType,
        object controller,
        object parent,
        string childName)
    {
        PropertyInfo childrenProp = groupType.GetProperty("children", BindingFlags.Public | BindingFlags.Instance);
        Array children = childrenProp?.GetValue(parent) as Array;
        if (children != null)
        {
            foreach (object child in children)
            {
                if (child == null) continue;
                PropertyInfo nameProp = groupType.GetProperty("name");
                string n = nameProp?.GetValue(child) as string;
                if (n == childName) return child;
            }
        }

        MethodInfo createNew = controllerType.GetMethod("CreateNewGroup", BindingFlags.Public | BindingFlags.Instance);
        MethodInfo addChild = controllerType.GetMethod("AddChildToParent", BindingFlags.Public | BindingFlags.Instance);
        if (createNew == null || addChild == null) return null;

        object created = createNew.Invoke(controller, new object[] { childName, true });
        addChild.Invoke(controller, new[] { created, parent });
        return created;
    }

    private static void ExposeVolumeReflect(
        Type controllerType,
        Type groupType,
        object controller,
        object group,
        string paramName)
    {
        if (group == null) return;

        MethodInfo getVol = groupType.GetMethod("GetGUIDForVolume", BindingFlags.Public | BindingFlags.Instance);
        if (getVol == null) return;
        object volumeGuid = getVol.Invoke(group, null);

        MethodInfo contains = controllerType.GetMethod("ContainsExposedParameter", BindingFlags.Public | BindingFlags.Instance);
        if (contains != null && (bool)contains.Invoke(controller, new[] { volumeGuid }))
        {
            RenameExposedReflect(controllerType, controller, volumeGuid, paramName);
            return;
        }

        Type pathType = FindEditorType("UnityEditor.Audio.AudioGroupParameterPath");
        if (pathType == null) return;

        ConstructorInfo ctor = pathType.GetConstructor(new[] { groupType, volumeGuid.GetType() });
        if (ctor == null) return;

        object path = ctor.Invoke(new[] { group, volumeGuid });
        MethodInfo add = controllerType.GetMethod("AddExposedParameter", BindingFlags.Public | BindingFlags.Instance);
        add?.Invoke(controller, new[] { path });
        RenameExposedReflect(controllerType, controller, volumeGuid, paramName);
    }

    private static void RenameExposedReflect(Type controllerType, object controller, object volumeGuid, string name)
    {
        PropertyInfo exposedProp = controllerType.GetProperty("exposedParameters", BindingFlags.Public | BindingFlags.Instance);
        if (exposedProp == null) return;

        Array exposed = exposedProp.GetValue(controller) as Array;
        if (exposed == null || exposed.Length == 0) return;

        bool changed = false;
        for (int i = 0; i < exposed.Length; i++)
        {
            object item = exposed.GetValue(i);
            if (item == null) continue;

            FieldInfo guidField = item.GetType().GetField("guid");
            FieldInfo nameField = item.GetType().GetField("name");
            if (guidField == null || nameField == null) continue;

            object g = guidField.GetValue(item);
            if (g == null || !g.Equals(volumeGuid)) continue;

            string current = nameField.GetValue(item) as string;
            if (current == name) return;

            nameField.SetValue(item, name);
            exposed.SetValue(item, i);
            changed = true;
            break;
        }

        if (changed)
            exposedProp.SetValue(controller, exposed);
    }

    private static bool HasGroup(AudioMixer mixer, string groupName)
    {
        if (mixer == null) return false;
        AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
        if (groups == null) return false;
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null && groups[i].name == groupName)
                return true;
        }
        return false;
    }

    /// <summary>리플렉션 실패 시 Master만 있는 기본 mixer YAML 기록 (자식은 Mixer 창에서 추가)</summary>
    private static void WriteFallbackMixerYaml()
    {
        // Master only — Unity가 정상 import. Ambient/SFX는 메뉴 재실행 또는 수동 추가.
        const string yaml = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!241 &24100000
AudioMixerController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: DDNB_Mixer
  m_OutputGroup: {fileID: 0}
  m_MasterGroup: {fileID: 24300002}
  m_Snapshots:
  - {fileID: 24500006}
  m_StartSnapshot: {fileID: 24500006}
  m_SuspendThreshold: -80
  m_EnableSuspend: 1
  m_UpdateMode: 0
  m_ExposedParameters:
  - guid: 11111111111111111111111111111111
    name: MasterVolume
  m_AudioMixerGroupViews:
  - guids:
    - aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
    name: View
  m_CurrentViewIndex: 0
  m_TargetSnapshot: {fileID: 24500006}
--- !u!243 &24300002
AudioMixerGroupController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Master
  m_AudioMixer: {fileID: 24100000}
  m_GroupID: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
  m_Children: []
  m_Volume: 11111111111111111111111111111111
  m_Pitch: 22222222222222222222222222222222
  m_Send: 00000000000000000000000000000000
  m_Effects:
  - {fileID: 24400004}
  m_UserColorIndex: 0
  m_Mute: 0
  m_Solo: 0
  m_BypassEffects: 0
--- !u!244 &24400004
AudioMixerEffectController:
  m_ObjectHideFlags: 3
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: 
  m_EffectID: 33333333333333333333333333333333
  m_EffectName: Attenuation
  m_MixLevel: 44444444444444444444444444444444
  m_Parameters: []
  m_SendTarget: {fileID: 0}
  m_EnableWetMix: 0
  m_Bypass: 0
--- !u!245 &24500006
AudioMixerSnapshotController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Snapshot
  m_AudioMixer: {fileID: 24100000}
  m_SnapshotID: 55555555555555555555555555555555
  m_FloatValues: {}
  m_TransitionOverrides: {}
";
        File.WriteAllText(MixerPath, yaml.Replace("\r\n", "\n"));
    }

    private static Type FindEditorType(string fullName)
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = asm.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
