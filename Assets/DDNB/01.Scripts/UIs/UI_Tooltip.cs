using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상호작용/플래시 키 안내 툴팁 (HUD).
/// 1개: 가운데(x=0), 미사용 행 비활성.
/// 2개: F 왼쪽(-130), E 오른쪽(130).
/// </summary>
public class UI_Tooltip : MonoBehaviour
{
    [Header("Flash Row")]
    [SerializeField] private GameObject row_Flash;
    [SerializeField] private Image image_FlashKey;
    [SerializeField] private TextMeshProUGUI text_Flash;

    [Header("Interact Row")]
    [SerializeField] private GameObject row_Interact;
    [SerializeField] private Image image_InteractKey;
    [SerializeField] private TextMeshProUGUI text_Interact;

    [Header("Key Icons")]
    [SerializeField] private Sprite spriteKeyE;
    [SerializeField] private Sprite spriteKeyF;

    [Header("Layout")]
    [SerializeField] private float singleRowX = 0f;
    [SerializeField] private float flashDualX = -130f;
    [SerializeField] private float interactDualX = 130f;

    private void Awake()
    {
        AutoBindIfNeeded();
        Clear();
    }

    private void AutoBindIfNeeded()
    {
        if (row_Flash == null)
        {
            Transform t = transform.Find("Row_Flash");
            if (t != null) row_Flash = t.gameObject;
        }

        if (row_Interact == null)
        {
            Transform t = transform.Find("Row_Interact");
            if (t != null) row_Interact = t.gameObject;
        }

        if (row_Flash != null)
        {
            if (image_FlashKey == null)
                image_FlashKey = row_Flash.transform.Find("Image_Key")?.GetComponent<Image>();
            if (text_Flash == null)
                text_Flash = row_Flash.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        }

        if (row_Interact != null)
        {
            if (image_InteractKey == null)
                image_InteractKey = row_Interact.transform.Find("Image_Key")?.GetComponent<Image>();
            if (text_Interact == null)
                text_Interact = row_Interact.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        }
    }

    public void SetKeySprites(Sprite e, Sprite f)
    {
        if (e != null) spriteKeyE = e;
        if (f != null) spriteKeyF = f;
    }

    public void Clear()
    {
        AutoBindIfNeeded();
        if (row_Flash != null) row_Flash.SetActive(false);
        if (row_Interact != null) row_Interact.SetActive(false);
        if (!gameObject.activeSelf) return;
        gameObject.SetActive(false);
    }

    public void Show(InteractPromptLine? flashLine, InteractPromptLine? interactLine)
    {
        AutoBindIfNeeded();

        bool hasFlash = flashLine.HasValue;
        bool hasInteract = interactLine.HasValue;

        if (!hasFlash && !hasInteract)
        {
            Clear();
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // 미사용 행은 비활성, 사용 행만 위치 조정
        if (row_Flash != null)
        {
            row_Flash.SetActive(hasFlash);
            if (hasFlash)
            {
                float x = hasInteract ? flashDualX : singleRowX;
                SetRowAnchoredX(row_Flash, x);
                ApplyLine(flashLine.Value, image_FlashKey, text_Flash, spriteKeyF);
            }
        }

        if (row_Interact != null)
        {
            row_Interact.SetActive(hasInteract);
            if (hasInteract)
            {
                float x = hasFlash ? interactDualX : singleRowX;
                SetRowAnchoredX(row_Interact, x);
                Sprite keySprite = interactLine.Value.keyName == "f" ? spriteKeyF : spriteKeyE;
                ApplyLine(interactLine.Value, image_InteractKey, text_Interact, keySprite);
            }
        }
    }

    private static void SetRowAnchoredX(GameObject row, float x)
    {
        RectTransform rt = row.transform as RectTransform;
        if (rt == null) return;

        Vector2 pos = rt.anchoredPosition;
        pos.x = x;
        rt.anchoredPosition = pos;
    }

    private static void ApplyLine(InteractPromptLine line, Image keyImage, TextMeshProUGUI label, Sprite keySprite)
    {
        if (label != null)
            label.text = line.text;

        if (keyImage != null)
        {
            bool showIcon = line.showKeyIcon && keySprite != null;
            keyImage.gameObject.SetActive(showIcon);
            if (showIcon)
                keyImage.sprite = keySprite;
        }
    }
}
