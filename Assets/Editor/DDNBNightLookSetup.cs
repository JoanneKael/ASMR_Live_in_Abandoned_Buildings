using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 완전 깜깜한 폐가용 URP Volume 프로필 생성 + 열린 씬에 암전 기본 세팅 적용.
/// Lightmap 베이크는 수동으로 진행하세요.
/// </summary>
public static class DDNBNightLookSetup
{
    private const string SettingsFolder = "Assets/DDNB/Settings";
    private const string ProfilePath = SettingsFolder + "/DDNB_Post Profile Night.asset";
    private const string MenuCreate = "Tools/DDNB/Create Or Refresh DDNB Night Post Profile";
    private const string MenuApply = "Tools/DDNB/Apply Pitch-Black Night Look To Open Scene";

    [MenuItem(MenuCreate)]
    public static void CreateOrRefreshProfile()
    {
        VolumeProfile profile = EnsureDarkNightProfile();
        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);
        EditorUtility.DisplayDialog(
            "DDNB Night Profile",
            $"프로필을 생성/갱신했습니다.\n\n{ProfilePath}\n\n" +
            "Exposure 음수, Vignette ON, 파란기 완화 설정입니다.",
            "OK");
    }

    [MenuItem(MenuApply)]
    public static void ApplyToOpenScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        VolumeProfile profile = EnsureDarkNightProfile();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Pitch-Black Night Look");

        int directionalOff = DisableDirectionalLights();
        ApplyRenderSettings();
        int volumeCount = AssignProfileToVolumes(profile);

        Undo.CollapseUndoOperations(undoGroup);

        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid())
            EditorSceneManager.MarkSceneDirty(active);

        EditorUtility.DisplayDialog(
            "Pitch-Black Night Look",
            "열린 씬에 암전 기본 세팅을 적용했습니다.\n\n" +
            $"• Directional Light 비활성: {directionalOff}\n" +
            $"• Volume 프로필 연결: {volumeCount}\n" +
            "• Ambient Intensity = 0 / Ambient = Black\n" +
            "• Fog = 거의 검정\n" +
            $"• Profile: {ProfilePath}\n\n" +
            "다음 단계:\n" +
            "1) 씬 저장 (Ctrl+S)\n" +
            "2) Window → Rendering → Lighting → Generate Lighting\n" +
            "3) 플레이어 플래시로 시야 밸런스 확인\n\n" +
            "Undo로 되돌릴 수 있습니다 (RenderSettings 일부는 씬 저장 전 확인).",
            "OK");
    }

    /// <summary>프로필이 없으면 만들고, 있으면 암전 값으로 덮어씁니다.</summary>
    public static VolumeProfile EnsureDarkNightProfile()
    {
        EnsureSettingsFolder();

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        ApplyDarkNightOverrides(profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return profile;
    }

    private static void EnsureSettingsFolder()
    {
        if (AssetDatabase.IsValidFolder(SettingsFolder))
            return;

        if (!AssetDatabase.IsValidFolder("Assets/DDNB"))
            AssetDatabase.CreateFolder("Assets", "DDNB");

        AssetDatabase.CreateFolder("Assets/DDNB", "Settings");
    }

    private static void ApplyDarkNightOverrides(VolumeProfile profile)
    {
        // ACES 톤매핑
        Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        // 파란기 완화 (쿨톤 최소화)
        WhiteBalance whiteBalance = GetOrAdd<WhiteBalance>(profile);
        whiteBalance.active = true;
        whiteBalance.temperature.Override(0f);
        whiteBalance.tint.Override(0f);

        // 핵심: 밝히지 않고 오히려 살짝 내림 + 대비/채도
        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.active = true;
        color.postExposure.Override(-0.45f);
        color.contrast.Override(30f);
        color.saturation.Override(-18f);
        color.colorFilter.overrideState = false;
        color.hueShift.Override(0f);

        // 중간톤을 거의 띄우지 않음 (원본 Night는 gamma lift가 밝게 만듦)
        LiftGammaGain liftGammaGain = GetOrAdd<LiftGammaGain>(profile);
        liftGammaGain.active = true;
        liftGammaGain.lift.Override(new Vector4(1f, 1f, 1f, 0f));
        liftGammaGain.gamma.Override(new Vector4(1f, 1f, 1f, 0.05f));
        liftGammaGain.gain.Override(new Vector4(1f, 1f, 1f, 0f));

        // 채널 믹서 중립화 (파란 과장 제거)
        ChannelMixer mixer = GetOrAdd<ChannelMixer>(profile);
        mixer.active = true;
        mixer.redOutRedIn.Override(100f);
        mixer.redOutGreenIn.Override(0f);
        mixer.redOutBlueIn.Override(0f);
        mixer.greenOutRedIn.Override(0f);
        mixer.greenOutGreenIn.Override(100f);
        mixer.greenOutBlueIn.Override(0f);
        mixer.blueOutRedIn.Override(0f);
        mixer.blueOutGreenIn.Override(0f);
        mixer.blueOutBlueIn.Override(100f);

        ColorCurves curves = GetOrAdd<ColorCurves>(profile);
        curves.active = false;

        // 플래시 잔광용으로만 약하게 둘 수 있으나 기본은 끔
        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.active = false;
        bloom.threshold.Override(1.2f);
        bloom.intensity.Override(0.15f);

        // 주변부 암전
        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.active = true;
        vignette.color.Override(Color.black);
        vignette.center.Override(new Vector2(0.5f, 0.5f));
        vignette.intensity.Override(0.4f);
        vignette.smoothness.Override(0.45f);
        vignette.rounded.Override(false);
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component) && component != null)
            return component;
        return profile.Add<T>(overrides: true);
    }

    private static int DisableDirectionalLights()
    {
        Light[] lights = Object.FindObjectsByType<Light>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int count = 0;
        foreach (Light light in lights)
        {
            if (light == null || light.type != LightType.Directional)
                continue;

            // 이미 꺼진 오브젝트도 기록
            Undo.RecordObject(light.gameObject, "Disable Directional Light");
            if (light.gameObject.activeSelf)
            {
                light.gameObject.SetActive(false);
                count++;
            }
            else
            {
                // 비활성 상태면 Intensity만 0으로 맞춰 재활성 시에도 안전
                Undo.RecordObject(light, "Zero Directional Intensity");
                light.intensity = 0f;
            }
        }

        return count;
    }

    private static void ApplyRenderSettings()
    {
        // RenderSettings는 씬 소속 — dirty 마크로 저장 유도
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;
        RenderSettings.ambientSkyColor = Color.black;
        RenderSettings.ambientEquatorColor = Color.black;
        RenderSettings.ambientGroundColor = Color.black;
        RenderSettings.subtractiveShadowColor = Color.black;
        RenderSettings.reflectionIntensity = 0f;

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.015f, 0.015f, 0.018f, 1f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.025f;
    }

    private static int AssignProfileToVolumes(VolumeProfile profile)
    {
        Volume[] volumes = Object.FindObjectsByType<Volume>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int assigned = 0;
        Volume preferred = null;

        foreach (Volume volume in volumes)
        {
            if (volume == null) continue;
            if (volume.isGlobal)
            {
                preferred = volume;
                break;
            }

            if (preferred == null && volume.gameObject.name.Contains("Post"))
                preferred = volume;
        }

        if (preferred == null && volumes.Length > 0)
            preferred = volumes[0];

        if (preferred != null)
        {
            Undo.RecordObject(preferred, "Assign DDNB Night Profile");
            preferred.sharedProfile = profile;
            preferred.weight = 1f;
            if (!preferred.isGlobal)
            {
                // 가능하면 Global로 (암전은 전역이 자연스러움)
                preferred.isGlobal = true;
            }
            assigned = 1;
            return assigned;
        }

        // Volume이 없으면 생성
        GameObject go = new GameObject("Post-Process");
        Undo.RegisterCreatedObjectUndo(go, "Create Post-Process Volume");
        Volume volumeNew = Undo.AddComponent<Volume>(go);
        volumeNew.isGlobal = true;
        volumeNew.priority = 1f;
        volumeNew.weight = 1f;
        volumeNew.sharedProfile = profile;
        return 1;
    }
}
