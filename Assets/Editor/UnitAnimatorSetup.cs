using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 유닛 공용 Animator Controller 베이스 생성.
/// 파라미터: Speed(Float), Attack(Trigger), IsScanning(Bool)
/// </summary>
public static class UnitAnimatorSetup
{
    private const string DefaultFolder = "Assets/DDNB/Animations/Units";
    private const string ControllerName = "UnitBaseAnimator.controller";

    [MenuItem("Tools/DDNB/Create Unit Base Animator Controller")]
    public static void CreateBaseAnimatorController()
    {
        if (!AssetDatabase.IsValidFolder("Assets/DDNB"))
            AssetDatabase.CreateFolder("Assets", "DDNB");
        if (!AssetDatabase.IsValidFolder("Assets/DDNB/Animations"))
            AssetDatabase.CreateFolder("Assets/DDNB", "Animations");
        if (!AssetDatabase.IsValidFolder(DefaultFolder))
            AssetDatabase.CreateFolder("Assets/DDNB/Animations", "Units");

        string path = Path.Combine(DefaultFolder, ControllerName).Replace('\\', '/');

        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Unit Base Animator",
                $"이미 존재합니다:\n{path}\n\n파라미터만 보강할까요?",
                "파라미터 보강",
                "취소");
            if (!overwrite) return;

            EnsureParameters(existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            Selection.activeObject = existing;
            Debug.Log($"[UnitAnimatorSetup] 파라미터 보강 완료: {path}");
            return;
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        EnsureParameters(controller);
        BuildDefaultStateMachine(controller);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Selection.activeObject = controller;

        Debug.Log(
            $"[UnitAnimatorSetup] 생성됨: {path}\n" +
            "1) Locomotion 블렌드에 Idle/Walk/Run 클립을 넣고\n" +
            "2) Attack / Scan 상태에 클립을 넣은 뒤\n" +
            "3) 모델별로 Animator Override Controller를 만들어 UnitData.animatorOverride에 할당하세요.");
    }

    private static void EnsureParameters(AnimatorController controller)
    {
        EnsureParam(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParam(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParam(controller, "IsScanning", AnimatorControllerParameterType.Bool);
    }

    private static void EnsureParam(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(name, type);
    }

    private static void BuildDefaultStateMachine(AnimatorController controller)
    {
        AnimatorStateMachine root = controller.layers[0].stateMachine;

        // 이동: Speed 파라미터용 단일 상태 (클립/블렌드는 유저가 채움)
        AnimatorState locomotion = root.AddState("Locomotion", new Vector3(300, 0, 0));
        root.defaultState = locomotion;

        AnimatorState attack = root.AddState("Attack", new Vector3(300, 80, 0));
        AnimatorState scan = root.AddState("Scan", new Vector3(300, 160, 0));

        // Attack 진입/복귀
        var toAttack = locomotion.AddTransition(attack);
        toAttack.hasExitTime = false;
        toAttack.duration = 0.05f;
        toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        var attackToLoco = attack.AddTransition(locomotion);
        attackToLoco.hasExitTime = true;
        attackToLoco.exitTime = 0.9f;
        attackToLoco.duration = 0.1f;

        // Scan 진입/복귀
        var toScan = locomotion.AddTransition(scan);
        toScan.hasExitTime = false;
        toScan.duration = 0.1f;
        toScan.AddCondition(AnimatorConditionMode.If, 0f, "IsScanning");

        var scanToLoco = scan.AddTransition(locomotion);
        scanToLoco.hasExitTime = false;
        scanToLoco.duration = 0.1f;
        scanToLoco.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsScanning");
    }

    [MenuItem("Tools/DDNB/Create Unit Animator Override From Selection")]
    public static void CreateOverrideFromSelection()
    {
        AnimatorController baseController = Selection.activeObject as AnimatorController;
        if (baseController == null)
        {
            EditorUtility.DisplayDialog(
                "Override 생성",
                "Project 창에서 UnitBaseAnimator(또는 베이스 Controller)를 선택한 뒤 다시 실행하세요.",
                "OK");
            return;
        }

        string basePath = AssetDatabase.GetAssetPath(baseController);
        string dir = Path.GetDirectoryName(basePath)?.Replace('\\', '/') ?? DefaultFolder;
        string overridePath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{baseController.name}_Override.overrideController");

        var overrideCtrl = new AnimatorOverrideController(baseController);
        AssetDatabase.CreateAsset(overrideCtrl, overridePath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = overrideCtrl;

        Debug.Log($"[UnitAnimatorSetup] Override 생성: {overridePath} — 클립만 교체한 뒤 UnitData.animatorOverride에 넣으세요.");
    }
}
