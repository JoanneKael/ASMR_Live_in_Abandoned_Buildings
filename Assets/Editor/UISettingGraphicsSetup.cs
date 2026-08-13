using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI_Setting 프리팹을 현재 구조(Brief / Display / Input / Sounds)에 맞게 정리합니다.
/// - Display 행: 기존 Title/Dropdown/line 유지, 옵션·폰트·캡션만 갱신
/// - Input: Display와 같은 골격 + 마우스 감도 슬라이더
/// - Sounds: Display와 같은 골격(사운드 컨트롤은 추후)
/// - 패널 스크립트 부착 및 Brief 참조 연결
/// </summary>
public static class UISettingGraphicsSetup
{
    private const string PrefabPath = "Assets/Resources/PopupUI/UI_Setting.prefab";
    private const string MenuPath = "Tools/DDNB/Setup UI_Setting Graphics Rows";

    [MenuItem(MenuPath)]
    public static void Setup()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            EditorUtility.DisplayDialog("UI_Setting Setup", $"프리팹을 열 수 없습니다:\n{PrefabPath}", "OK");
            return;
        }

        try
        {
            Undo.RegisterFullObjectHierarchyUndo(root, "Setup UI_Setting Panels");

            TMP_FontAsset font = LoadPreferredFont();
            TMP_DefaultControls.Resources uiResources = CreateTmpResources();

            Transform brief = FindChildIgnoreCase(root.transform, "Brief");
            Transform display = root.transform.Find("Display");
            Transform input = root.transform.Find("Input");
            Transform sounds = root.transform.Find("Sounds");

            if (display == null)
            {
                EditorUtility.DisplayDialog("UI_Setting Setup", "Display 패널을 찾지 못했습니다.", "OK");
                return;
            }

            // --- Display: 기존 행 유지, 옵션/타이틀/폰트만 ---
            ConfigureDisplayPanel(display, font);

            // --- Input / Sounds: Display 골격 복제 후 행 교체 ---
            if (input != null)
                BuildPanelFromDisplayTemplate(input, display, "입력", font, uiResources, buildMouseSensitivity: true);
            if (sounds != null)
                BuildPanelFromDisplayTemplate(sounds, display, "사운드", font, uiResources, buildMouseSensitivity: false);

            // 패널 스크립트
            EnsureComponent<UI_SettingDisplay>(display.gameObject);
            if (input != null) EnsureComponent<UI_SettingInput>(input.gameObject);
            if (sounds != null) EnsureComponent<UI_SettingSounds>(sounds.gameObject);

            // 시작 시 Brief만 활성
            if (brief != null) brief.gameObject.SetActive(true);
            display.gameObject.SetActive(false);
            if (input != null) input.gameObject.SetActive(false);
            if (sounds != null) sounds.gameObject.SetActive(false);

            // UI_Setting (Brief 셸) 참조
            UI_Setting uiSetting = root.GetComponent<UI_Setting>();
            if (uiSetting == null)
                uiSetting = Undo.AddComponent<UI_Setting>(root);
            BindRootRefs(uiSetting, brief, display, input, sounds);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            EditorUtility.DisplayDialog(
                "UI_Setting Setup",
                "Display 옵션/폰트 갱신, Input 감도 슬라이더, 패널 스크립트 연결을 완료했습니다.\n\n" +
                "드롭다운 Label=캡션, Template/Item Label=목록 항목입니다. 옵션 텍스트는 코드·툴에서 채웁니다.",
                "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureDisplayPanel(Transform display, TMP_FontAsset font)
    {
        SetSectionTitle(display, "Display", "디스플레이 모드", font);
        SetSectionTitle(display, "Frame", "프레임 제한", font);
        SetSectionTitle(display, "Graphic", "그래픽 품질", font);
        SetSectionTitle(display, "VSync", "수직동기화", font);

        ConfigureDropdown(display.Find("Display")?.GetComponentInChildren<TMP_Dropdown>(true),
            SettingManager.DisplayModeLabels, font);

        string[] frameLabels = new string[SettingManager.FrameRateOptions.Length];
        for (int i = 0; i < frameLabels.Length; i++)
            frameLabels[i] = $"{SettingManager.FrameRateOptions[i]} FPS";
        ConfigureDropdown(display.Find("Frame")?.GetComponentInChildren<TMP_Dropdown>(true),
            frameLabels, font);

        ConfigureDropdown(display.Find("Graphic")?.GetComponentInChildren<TMP_Dropdown>(true),
            SettingManager.QualityLabels, font);

        // 패널 타이틀
        SetPanelHeaderTitle(display, "화면 및 그래픽", font);
    }

    private static void BuildPanelFromDisplayTemplate(
        Transform target,
        Transform displayTemplate,
        string headerTitle,
        TMP_FontAsset font,
        TMP_DefaultControls.Resources uiResources,
        bool buildMouseSensitivity)
    {
        // 기존 자식 제거 후 Display 복제
        for (int i = target.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(target.GetChild(i).gameObject);

        for (int i = 0; i < displayTemplate.childCount; i++)
        {
            Transform src = displayTemplate.GetChild(i);
            GameObject clone = Object.Instantiate(src.gameObject, target, false);
            clone.name = src.name;
            Undo.RegisterCreatedObjectUndo(clone, "Clone Setting Panel Child");
        }

        // 그래픽 전용 행 제거
        DestroyChildIfExists(target, "Display");
        DestroyChildIfExists(target, "Frame");
        DestroyChildIfExists(target, "Graphic");
        DestroyChildIfExists(target, "VSync");

        SetPanelHeaderTitle(target, headerTitle, font);

        if (buildMouseSensitivity)
            CreateMouseSensitivityRow(target, font, uiResources);
        // Sounds는 Buttons/Title/line만 두고 컨트롤은 추후
    }

    private static void CreateMouseSensitivityRow(
        Transform panel,
        TMP_FontAsset font,
        TMP_DefaultControls.Resources uiResources)
    {
        // Display의 Frame 행 레이아웃을 참고한 빈 행
        GameObject row = new GameObject("MouseSensitivity", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(row, "Create MouseSensitivity");
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.SetParent(panel, false);
        rowRt.anchorMin = new Vector2(0.5f, 0.5f);
        rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = new Vector2(0f, 100f);
        rowRt.sizeDelta = new Vector2(580f, 140f);

        // Title
        GameObject titleGo = new GameObject("Title", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(titleGo, "Create Title");
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.SetParent(rowRt, false);
        titleRt.anchorMin = new Vector2(0f, 0.5f);
        titleRt.anchorMax = new Vector2(0f, 0.5f);
        titleRt.pivot = new Vector2(0f, 0.5f);
        titleRt.anchoredPosition = Vector2.zero;
        titleRt.sizeDelta = new Vector2(220f, 60f);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "마우스 감도";
        titleTmp.fontSize = 36;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        titleTmp.raycastTarget = false;
        if (font != null) titleTmp.font = font;

        // Value
        GameObject valueGo = new GameObject("Value", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(valueGo, "Create Value");
        RectTransform valueRt = valueGo.GetComponent<RectTransform>();
        valueRt.SetParent(rowRt, false);
        valueRt.anchorMin = new Vector2(1f, 0.5f);
        valueRt.anchorMax = new Vector2(1f, 0.5f);
        valueRt.pivot = new Vector2(1f, 0.5f);
        valueRt.anchoredPosition = new Vector2(0f, 28f);
        valueRt.sizeDelta = new Vector2(80f, 40f);
        TextMeshProUGUI valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
        valueTmp.text = "1.50";
        valueTmp.fontSize = 28;
        valueTmp.color = Color.white;
        valueTmp.alignment = TextAlignmentOptions.MidlineRight;
        valueTmp.raycastTarget = false;
        if (font != null) valueTmp.font = font;

        // Slider (Unity UI)
        GameObject sliderGo = DefaultControls.CreateSlider(CreateUgUiResources(uiResources));
        sliderGo.name = "Slider";
        Undo.RegisterCreatedObjectUndo(sliderGo, "Create Slider");
        RectTransform sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.SetParent(rowRt, false);
        sliderRt.anchorMin = new Vector2(1f, 0.5f);
        sliderRt.anchorMax = new Vector2(1f, 0.5f);
        sliderRt.pivot = new Vector2(1f, 0.5f);
        sliderRt.anchoredPosition = new Vector2(0f, -10f);
        sliderRt.sizeDelta = new Vector2(280f, 28f);

        Slider slider = sliderGo.GetComponent<Slider>();
        slider.minValue = SettingManager.MinMouseSensitivity;
        slider.maxValue = SettingManager.MaxMouseSensitivity;
        slider.wholeNumbers = false;
        slider.value = SettingManager.DefaultMouseSensitivity;

        // 구분선
        GameObject lineGo = new GameObject("line", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(lineGo, "Create line");
        RectTransform lineRt = lineGo.GetComponent<RectTransform>();
        lineRt.SetParent(rowRt, false);
        lineRt.anchorMin = new Vector2(0.5f, 0.5f);
        lineRt.anchorMax = new Vector2(0.5f, 0.5f);
        lineRt.anchoredPosition = new Vector2(-40f, 0f);
        lineRt.sizeDelta = new Vector2(1f, 40f);
        Image lineImg = lineGo.AddComponent<Image>();
        lineImg.color = new Color(0.78f, 0.78f, 0.78f, 1f);
    }

    private static void ConfigureDropdown(TMP_Dropdown dropdown, string[] options, TMP_FontAsset font)
    {
        if (dropdown == null || options == null) return;

        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string>(options));
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        StyleDropdown(dropdown, font);

        if (dropdown.captionText != null && options.Length > 0)
            dropdown.captionText.text = options[Mathf.Clamp(dropdown.value, 0, options.Length - 1)];

        if (dropdown.itemText != null)
            dropdown.itemText.text = options.Length > 0 ? options[0] : string.Empty;
    }

    /// <summary>드롭다운 Label 폰트 24, Content/Item 높이 40</summary>
    private static void StyleDropdown(TMP_Dropdown dropdown, TMP_FontAsset font)
    {
        if (dropdown == null) return;

        if (dropdown.captionText != null)
        {
            if (font != null) dropdown.captionText.font = font;
            dropdown.captionText.fontSize = 24f;
        }

        if (dropdown.itemText != null)
        {
            if (font != null) dropdown.itemText.font = font;
            dropdown.itemText.fontSize = 24f;
        }

        if (dropdown.template != null)
        {
            Transform item = dropdown.template.Find("Viewport/Content/Item");
            if (item is RectTransform itemRt)
            {
                Vector2 size = itemRt.sizeDelta;
                size.y = 40f;
                itemRt.sizeDelta = size;
                itemRt.anchoredPosition = new Vector2(itemRt.anchoredPosition.x, 0f);
            }

            dropdown.template.gameObject.SetActive(false);
        }

        ApplyFontRecursive(dropdown.transform, font);
    }

    private static void SetSectionTitle(Transform panel, string sectionName, string title, TMP_FontAsset font)
    {
        Transform section = panel.Find(sectionName);
        if (section == null) return;

        Transform titleTf = section.Find("Title");
        TextMeshProUGUI tmp = titleTf != null
            ? titleTf.GetComponent<TextMeshProUGUI>()
            : section.GetComponentInChildren<TextMeshProUGUI>(true);

        // Title이 이미지 래퍼인 경우 자식 TMP
        if (tmp == null && titleTf != null)
            tmp = titleTf.GetComponentInChildren<TextMeshProUGUI>(true);

        if (tmp == null) return;
        tmp.text = title;
        if (font != null) tmp.font = font;
    }

    private static void SetPanelHeaderTitle(Transform panel, string title, TMP_FontAsset font)
    {
        Transform titleRoot = panel.Find("Title");
        if (titleRoot == null) return;

        TextMeshProUGUI tmp = titleRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null) return;
        tmp.text = title;
        if (font != null) tmp.font = font;
    }

    private static void BindRootRefs(
        UI_Setting uiSetting,
        Transform brief,
        Transform display,
        Transform input,
        Transform sounds)
    {
        SerializedObject so = new SerializedObject(uiSetting);
        so.FindProperty("brief").objectReferenceValue = brief != null ? brief.gameObject : null;
        so.FindProperty("panel_Display").objectReferenceValue = display != null ? display.gameObject : null;
        so.FindProperty("panel_Input").objectReferenceValue = input != null ? input.gameObject : null;
        so.FindProperty("panel_Sounds").objectReferenceValue = sounds != null ? sounds.gameObject : null;

        if (brief != null)
        {
            so.FindProperty("button_Back").objectReferenceValue =
                brief.Find("Button_Back")?.GetComponent<Button>();
            so.FindProperty("button_Graphics").objectReferenceValue =
                brief.Find("Button_Graphics")?.GetComponent<Button>();
            so.FindProperty("button_Input").objectReferenceValue =
                brief.Find("Button_Input")?.GetComponent<Button>();
            so.FindProperty("button_Sounds").objectReferenceValue =
                brief.Find("Button_Sounds")?.GetComponent<Button>();
        }

        // 구 필드가 남아있으면 무시 (스크립트에서 제거됨)
        so.ApplyModifiedPropertiesWithoutUndo();

        // Display 패널 참조
        UI_SettingDisplay displayUi = display.GetComponent<UI_SettingDisplay>();
        if (displayUi != null)
        {
            SerializedObject dso = new SerializedObject(displayUi);
            Transform buttons = display.Find("Buttons");
            dso.FindProperty("button_Back").objectReferenceValue =
                buttons != null ? buttons.Find("Button_Back")?.GetComponent<Button>() : null;
            dso.FindProperty("button_Reset").objectReferenceValue =
                buttons != null ? buttons.Find("Button_Reset")?.GetComponent<Button>() : null;
            dso.FindProperty("button_Confirm").objectReferenceValue =
                buttons != null ? buttons.Find("Button_Confirm")?.GetComponent<Button>() : null;
            dso.FindProperty("dropdown_Display").objectReferenceValue =
                display.Find("Display")?.GetComponentInChildren<TMP_Dropdown>(true);
            dso.FindProperty("dropdown_Frame").objectReferenceValue =
                display.Find("Frame")?.GetComponentInChildren<TMP_Dropdown>(true);
            dso.FindProperty("dropdown_Graphic").objectReferenceValue =
                display.Find("Graphic")?.GetComponentInChildren<TMP_Dropdown>(true);
            dso.FindProperty("toggle_VSync").objectReferenceValue =
                display.Find("VSync")?.GetComponentInChildren<Toggle>(true);
            dso.ApplyModifiedPropertiesWithoutUndo();
        }

        if (input != null)
        {
            UI_SettingInput inputUi = input.GetComponent<UI_SettingInput>();
            if (inputUi != null)
            {
                SerializedObject iso = new SerializedObject(inputUi);
                Transform buttons = input.Find("Buttons");
                Transform row = input.Find("MouseSensitivity");
                iso.FindProperty("button_Back").objectReferenceValue =
                    buttons != null ? buttons.Find("Button_Back")?.GetComponent<Button>() : null;
                iso.FindProperty("button_Reset").objectReferenceValue =
                    buttons != null ? buttons.Find("Button_Reset")?.GetComponent<Button>() : null;
                iso.FindProperty("button_Confirm").objectReferenceValue =
                    buttons != null ? buttons.Find("Button_Confirm")?.GetComponent<Button>() : null;
                iso.FindProperty("slider_MouseSensitivity").objectReferenceValue =
                    row != null ? row.GetComponentInChildren<Slider>(true) : null;
                iso.FindProperty("text_MouseSensitivityValue").objectReferenceValue =
                    row != null ? row.Find("Value")?.GetComponent<TextMeshProUGUI>() : null;
                iso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        if (sounds != null)
        {
            UI_SettingSounds soundsUi = sounds.GetComponent<UI_SettingSounds>();
            if (soundsUi != null)
            {
                SerializedObject sso = new SerializedObject(soundsUi);
                Transform buttons = sounds.Find("Buttons");
                sso.FindProperty("button_Back").objectReferenceValue =
                    buttons != null ? buttons.Find("Button_Back")?.GetComponent<Button>() : null;
                sso.FindProperty("button_Reset").objectReferenceValue =
                    buttons != null ? buttons.Find("Button_Reset")?.GetComponent<Button>() : null;
                sso.FindProperty("button_Confirm").objectReferenceValue =
                    buttons != null ? buttons.Find("Button_Confirm")?.GetComponent<Button>() : null;
                sso.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void DestroyChildIfExists(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null)
            Undo.DestroyObjectImmediate(t.gameObject);
    }

    private static Transform FindChildIgnoreCase(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (string.Equals(c.name, name, System.StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return null;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null)
            c = Undo.AddComponent<T>(go);
        return c;
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

    private static TMP_DefaultControls.Resources CreateTmpResources()
    {
        return new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
    }

    private static DefaultControls.Resources CreateUgUiResources(TMP_DefaultControls.Resources tmp)
    {
        return new DefaultControls.Resources
        {
            standard = tmp.standard,
            background = tmp.background,
            inputField = tmp.inputField,
            knob = tmp.knob,
            checkmark = tmp.checkmark,
            dropdown = tmp.dropdown,
            mask = tmp.mask
        };
    }

    private static void ApplyFontRecursive(Transform root, TMP_FontAsset font)
    {
        if (font == null || root == null) return;
        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI t in texts)
            t.font = font;
    }
}
