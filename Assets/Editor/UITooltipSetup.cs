using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 하위에 UI_Tooltip (F/E 키 안내)을 생성·연결합니다.
/// </summary>
public static class UITooltipSetup
{
    private const string MenuPath = "Tools/DDNB/Setup HUD UI_Tooltip";
    private const string HudPrefabPath = "Assets/DDNB/Prefabs/UI/HUD.prefab";
    private const string KeyEPath = "Assets/GameInputControllerIconsFree/keyboard/keyboard-solid/e.png";
    private const string KeyFPath = "Assets/GameInputControllerIconsFree/keyboard/keyboard-solid/f.png";

    [MenuItem(MenuPath)]
    public static void Setup()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        if (root == null)
        {
            EditorUtility.DisplayDialog("UI_Tooltip", $"HUD 프리팹을 열 수 없습니다:\n{HudPrefabPath}", "OK");
            return;
        }

        try
        {
            Undo.RegisterFullObjectHierarchyUndo(root, "Setup UI_Tooltip");

            Transform existing = root.transform.Find("UI_Tooltip");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            Sprite spriteE = AssetDatabase.LoadAssetAtPath<Sprite>(KeyEPath);
            Sprite spriteF = AssetDatabase.LoadAssetAtPath<Sprite>(KeyFPath);
            TMP_FontAsset font = LoadPreferredFont();

            GameObject tooltipGo = CreateTooltipRoot(root.transform);
            GameObject rowFlash = CreatePromptRow(tooltipGo.transform, "Row_Flash", "플래시라이트", spriteF, font);
            GameObject rowInteract = CreatePromptRow(tooltipGo.transform, "Row_Interact", "상호작용", spriteE, font);

            UI_Tooltip tooltip = tooltipGo.GetComponent<UI_Tooltip>();
            if (tooltip == null)
                tooltip = Undo.AddComponent<UI_Tooltip>(tooltipGo);

            SerializedObject so = new SerializedObject(tooltip);
            so.FindProperty("row_Flash").objectReferenceValue = rowFlash;
            so.FindProperty("row_Interact").objectReferenceValue = rowInteract;
            so.FindProperty("image_FlashKey").objectReferenceValue =
                rowFlash.transform.Find("Image_Key")?.GetComponent<Image>();
            so.FindProperty("text_Flash").objectReferenceValue =
                rowFlash.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("image_InteractKey").objectReferenceValue =
                rowInteract.transform.Find("Image_Key")?.GetComponent<Image>();
            so.FindProperty("text_Interact").objectReferenceValue =
                rowInteract.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("spriteKeyE").objectReferenceValue = spriteE;
            so.FindProperty("spriteKeyF").objectReferenceValue = spriteF;
            so.ApplyModifiedPropertiesWithoutUndo();

            // HUD 참조 연결
            HUD hud = root.GetComponent<HUD>();
            if (hud != null)
            {
                SerializedObject hso = new SerializedObject(hud);
                hso.FindProperty("tooltip").objectReferenceValue = tooltip;
                Transform cross = root.transform.Find("UI_Crosshair");
                if (cross != null)
                    hso.FindProperty("crosshair").objectReferenceValue = cross.gameObject;
                hso.ApplyModifiedPropertiesWithoutUndo();
            }

            rowFlash.SetActive(false);
            rowInteract.SetActive(false);
            tooltipGo.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            EditorUtility.DisplayDialog(
                "UI_Tooltip",
                "HUD에 UI_Tooltip을 생성하고 키 아이콘(E/F)을 연결했습니다.",
                "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static GameObject CreateTooltipRoot(Transform parent)
    {
        GameObject go = new GameObject("UI_Tooltip", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create UI_Tooltip");
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -80f);
        rt.sizeDelta = new Vector2(520f, 120f);

        VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.padding = new RectOffset(8, 8, 8, 8);

        ContentSizeFitter fitter = go.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        return go;
    }

    private static GameObject CreatePromptRow(
        Transform parent,
        string rowName,
        string defaultText,
        Sprite keySprite,
        TMP_FontAsset font)
    {
        GameObject row = new GameObject(rowName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(row, "Create Prompt Row");
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.SetParent(parent, false);
        rowRt.sizeDelta = new Vector2(480f, 48f);

        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.spacing = 12f;
        h.childControlHeight = true;
        h.childControlWidth = false;
        h.childForceExpandHeight = true;
        h.childForceExpandWidth = false;
        h.padding = new RectOffset(12, 12, 4, 4);

        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;

        // Key image
        GameObject keyGo = new GameObject("Image_Key", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(keyGo, "Create Key Image");
        RectTransform keyRt = keyGo.GetComponent<RectTransform>();
        keyRt.SetParent(rowRt, false);
        keyRt.sizeDelta = new Vector2(40f, 40f);
        Image keyImage = keyGo.AddComponent<Image>();
        keyImage.sprite = keySprite;
        keyImage.preserveAspect = true;
        keyImage.raycastTarget = false;
        LayoutElement keyLe = keyGo.AddComponent<LayoutElement>();
        keyLe.preferredWidth = 40f;
        keyLe.preferredHeight = 40f;

        // Text
        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(textGo, "Create Text");
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.SetParent(rowRt, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = 28f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
        LayoutElement textLe = textGo.AddComponent<LayoutElement>();
        textLe.preferredWidth = 360f;
        textLe.flexibleWidth = 1f;

        return row;
    }

    private static TMP_FontAsset LoadPreferredFont()
    {
        string[] guids = AssetDatabase.FindAssets("SUIT-Regular SDF t:TMP_FontAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) return font;
        }
        return TMP_Settings.defaultFontAsset;
    }
}
