using UnityEngine;

public class HUD : MonoBehaviour
{
    [SerializeField] private GameObject[] uis;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private UI_Crosshair crosshairUI;
    [SerializeField] private UI_Tooltip tooltip;
    [SerializeField] private UI_Quest questUI;

    private bool isLobbyHud;
    private PlayerFlashlight _flashlight;

    private void Awake()
    {
        AutoBindCrosshair();
        AutoBindTooltip();
        AutoBindQuest();
    }

    private void LateUpdate()
    {
        RefreshCrosshairVisibility();
        RefreshTooltip();
    }

    public void Init(bool lobby)
    {
        isLobbyHud = lobby;
        AutoBindCrosshair();
        AutoBindTooltip();
        AutoBindQuest();

        if (lobby)
        {
            foreach (var go in uis)
            {
                go.SetActive(false);
            }
            uis[4].SetActive(true);
            // 로비에서도 크로스헤어·퀘스트 표시
            if (crosshair != null)
                crosshair.SetActive(true);
            if (questUI != null)
                questUI.gameObject.SetActive(true);
        }
        else
        {
            foreach (var go in uis)
            {
                go.SetActive(true);
            }
            uis[2].SetActive(false);
        }

        RefreshCrosshairVisibility();
        RefreshTooltip();
        if (questUI != null)
            questUI.Refresh();
    }

    private void AutoBindCrosshair()
    {
        if (crosshair == null)
            crosshair = FindChildByName("UI_Crosshair");

        if (crosshair == null) return;

        if (crosshairUI == null)
            crosshairUI = crosshair.GetComponent<UI_Crosshair>();
        if (crosshairUI == null)
            crosshairUI = crosshair.AddComponent<UI_Crosshair>();
    }

    private void AutoBindTooltip()
    {
        if (tooltip != null) return;

        GameObject go = FindChildByName("UI_Tooltip");
        if (go != null)
            tooltip = go.GetComponent<UI_Tooltip>() ?? go.AddComponent<UI_Tooltip>();
    }

    private void AutoBindQuest()
    {
        if (questUI != null) return;

        GameObject go = FindChildByName("UI_Quest");
        if (go != null)
            questUI = go.GetComponent<UI_Quest>() ?? go.AddComponent<UI_Quest>();
    }

    private GameObject FindChildByName(string objectName)
    {
        Transform t = transform.Find(objectName);
        if (t != null) return t.gameObject;

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child.gameObject;
        }

        return null;
    }

    private void RefreshCrosshairVisibility()
    {
        if (crosshair == null) return;

        // 로비 포함 평소 ON. ASMR/은신/ExitDoor홀드/스턴·넉아웃만 OFF
        bool show = CrosshairVisibility.ShouldShow();
        if (crosshair.activeSelf != show)
            crosshair.SetActive(show);

        // 표시 중일 때 포커스에 맞춰 아이콘 동기화
        if (show && crosshairUI != null
            && GameManager.Instance != null
            && GameManager.Instance.Player != null)
        {
            PlayerInteraction interaction = GameManager.Instance.Player.GetComponent<PlayerInteraction>();
            crosshairUI.SetFromInteractable(interaction != null ? interaction.CurrentInteractable : null);
        }
    }

    private void RefreshTooltip()
    {
        if (tooltip == null) return;

        if (isLobbyHud
            || GameManager.Instance == null
            || GameManager.Instance.Player == null)
        {
            tooltip.Clear();
            return;
        }

        Player player = GameManager.Instance.Player;
        PlayerState state = player.CurrentState;

        // ASMR 진행 / 스턴 / 로비 / 입력 차단 / ExitDoor 홀드 중에는 툴팁 숨김
        if (state == PlayerState.ASMR
            || state == PlayerState.Stunned
            || state == PlayerState.Lobby
            || player.IsHideTransitioning
            || (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked))
        {
            tooltip.Clear();
            return;
        }

        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (interaction != null && interaction.IsHidingCrosshairForInteraction)
        {
            tooltip.Clear();
            return;
        }

        if (_flashlight == null)
            _flashlight = player.GetComponentInChildren<PlayerFlashlight>(true);

        InteractPromptLine? flashLine = null;
        InteractPromptLine? interactLine = null;

        // 은신 중: E + 나오기 (플래시 안내도 함께 가능)
        if (state == PlayerState.Hidden)
        {
            if (InteractPromptBuilder.TryBuildHiddenPrompt(out InteractPromptLine hiddenLine))
                interactLine = hiddenLine;
        }
        else if (interaction != null && interaction.CurrentInteractable != null)
        {
            if (InteractPromptBuilder.TryBuildInteractPrompt(
                    interaction.CurrentInteractable,
                    player,
                    out InteractPromptLine line))
            {
                interactLine = line;
            }
        }

        if (InteractPromptBuilder.TryBuildFlashlightPrompt(_flashlight, out InteractPromptLine fLine))
            flashLine = fLine;

        tooltip.Show(flashLine, interactLine);
    }
}
