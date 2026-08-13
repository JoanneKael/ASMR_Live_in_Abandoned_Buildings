using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI_Setting Sounds 패널에 Master / Ambient / SFX 행(슬라이더·뮤트·숫자)을 생성합니다.
/// </summary>
public static class UISettingSoundsSetup
{
    private const string PrefabPath = "Assets/Resources/PopupUI/UI_Setting.prefab";
    private const string MenuPath = "Tools/DDNB/Setup UI_Setting Sounds Rows";

    [MenuItem(MenuPath)]
    public static void Setup()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            EditorUtility.DisplayDialog("UI_Setting Sounds", $"프리팹을 열 수 없습니다:\n{PrefabPath}", "OK");
            return;
        }

        try
        {
            Undo.RegisterFullObjectHierarchyUndo(root, "Setup UI_Setting Sounds");

            Transform sounds = root.transform.Find("Sounds");
            if (sounds == null)
            {
                EditorUtility.DisplayDialog("UI_Setting Sounds", "Sounds 패널이 없습니다.", "OK");
                return;
            }

            TMP_FontAsset font = LoadPreferredFont();
            Transform input = root.transform.Find("Input");
            Transform templateRow = input != null ? input.Find("MouseSensitivity") : null;

            // Display 스타일 드롭다운 행은 제거하고 사운드 행만 유지
            DestroyNamedChildrenExcept(sounds, "Title", "Buttons");

            float y = 80f;
            CreateVolumeRow(sounds, "Master", "마스터", y, font, templateRow);
            y -= 90f;
            CreateVolumeRow(sounds, "Ambient", "환경음", y, font, templateRow);
            y -= 90f;
            CreateVolumeRow(sounds, "SFX", "효과음", y, font, templateRow);

            EnsureComponent<UI_SettingSounds>(sounds.gameObject);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            EditorUtility.DisplayDialog(
                "UI_Setting Sounds",
                "Master / Ambient / SFX 행을 생성했습니다.\n슬라이더: 0~1, 기본값 1.00\nConfirm 시 SettingManager.ApplySounds가 호출됩니다.",
                "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void DestroyNamedChildrenExcept(Transform parent, params string[] keep)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            bool keepIt = false;
            for (int k = 0; k < keep.Length; k++)
            {
                if (child.name == keep[k])
                {
                    keepIt = true;
                    break;
                }
            }

            if (!keepIt)
                Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private static void CreateVolumeRow(
        Transform parent,
        string rowName,
        string title,
        float anchoredY,
        TMP_FontAsset font,
        Transform templateRow)
    {
        Transform existing = parent.Find(rowName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject rowGo;
        if (templateRow != null)
        {
            rowGo = Object.Instantiate(templateRow.gameObject, parent);
            rowGo.name = rowName;
            Undo.RegisterCreatedObjectUndo(rowGo, "Create volume row");
        }
        else
        {
            rowGo = new GameObject(rowName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rowGo, "Create volume row");
            rowGo.transform.SetParent(parent, false);
        }

        RectTransform rt = rowGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, anchoredY);
        rt.sizeDelta = new Vector2(700f, 80f);

        // Title
        TextMeshProUGUI titleTmp = rowGo.GetComponentInChildren<TextMeshProUGUI>(true);
        Transform titleTf = rowGo.transform.Find("Title");
        if (titleTf != null)
            titleTmp = titleTf.GetComponentInChildren<TextMeshProUGUI>(true);
        if (titleTmp != null)
        {
            titleTmp.text = title;
            if (font != null) titleTmp.font = font;
            titleTmp.fontSize = 28f;
        }

        // 감도 템플릿(0~3, 기본 1.5) 잔여값 제거 — 볼륨은 항상 0~1, 기본 1
        Slider[] allSliders = rowGo.GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < allSliders.Length; i++)
        {
            allSliders[i].minValue = 0f;
            allSliders[i].maxValue = 1f;
            allSliders[i].wholeNumbers = false;
            allSliders[i].SetValueWithoutNotify(1f);
            allSliders[i].gameObject.name = "Slider";
        }

        if (allSliders.Length == 0)
        {
            GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGo.name = "Slider";
            sliderGo.transform.SetParent(rowGo.transform, false);
            RectTransform srt = sliderGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(40f, 8f);
            srt.sizeDelta = new Vector2(320f, 24f);
            Slider slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(1f);
        }

        // Mute (왼쪽 아래)
        Transform muteTf = rowGo.transform.Find("Mute");
        Toggle mute;
        if (muteTf == null)
        {
            GameObject muteGo = DefaultControls.CreateToggle(new DefaultControls.Resources());
            muteGo.name = "Mute";
            muteGo.transform.SetParent(rowGo.transform, false);
            RectTransform mrt = muteGo.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.5f, 0.5f);
            mrt.anchorMax = new Vector2(0.5f, 0.5f);
            mrt.anchoredPosition = new Vector2(-120f, -28f);
            mrt.sizeDelta = new Vector2(160f, 24f);
            mute = muteGo.GetComponent<Toggle>();

            Text label = muteGo.GetComponentInChildren<Text>(true);
            if (label != null) label.text = "뮤트";
            TextMeshProUGUI labelTmp = muteGo.GetComponentInChildren<TextMeshProUGUI>(true);
            if (labelTmp != null)
            {
                labelTmp.text = "뮤트";
                if (font != null) labelTmp.font = font;
            }
        }
        else
        {
            mute = muteTf.GetComponent<Toggle>();
        }

        if (mute != null)
            mute.isOn = false;

        // Value (오른쪽 아래) — 감도 템플릿의 1.50을 1.00으로 교체
        Transform valueTf = rowGo.transform.Find("Value");
        TextMeshProUGUI valueTmp = null;
        if (valueTf == null)
        {
            GameObject valueGo = new GameObject("Value", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(valueGo, "Create Value");
            valueGo.transform.SetParent(rowGo.transform, false);
            RectTransform vrt = valueGo.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0.5f, 0.5f);
            vrt.anchorMax = new Vector2(0.5f, 0.5f);
            vrt.anchoredPosition = new Vector2(200f, -28f);
            vrt.sizeDelta = new Vector2(80f, 28f);
            valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
            valueTmp.fontSize = 22f;
            valueTmp.alignment = TextAlignmentOptions.MidlineRight;
            if (font != null) valueTmp.font = font;
        }
        else
        {
            valueTmp = valueTf.GetComponent<TextMeshProUGUI>()
                ?? valueTf.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (valueTmp != null)
            valueTmp.text = "1.00";

        // Dropdown 잔여물 제거
        TMP_Dropdown dropdown = rowGo.GetComponentInChildren<TMP_Dropdown>(true);
        if (dropdown != null)
            Undo.DestroyObjectImmediate(dropdown.gameObject);
    }

    private static void EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null)
            Undo.AddComponent<T>(go);
    }

    private static TMP_FontAsset LoadPreferredFont()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset SUIT");
        if (guids != null && guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
        return null;
    }
}
