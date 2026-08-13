using UnityEngine;

/// <summary>HUD 툴팁에 표시할 한 줄 프롬프트</summary>
public struct InteractPromptLine
{
    public bool showKeyIcon;
    public string keyName; // "e" / "f"
    public string text;
}

/// <summary>상호작용 대상별 툴팁 문구</summary>
public static class InteractPromptBuilder
{
    public static bool TryBuildInteractPrompt(IInteractable target, Player player, out InteractPromptLine line)
    {
        line = default;

        if (target == null) return false;

        // 완료된 ASMR: 아이콘 없이 안내만
        if (target is InteractableASMR asmr && asmr.isASMRCompleted)
        {
            line = new InteractPromptLine
            {
                showKeyIcon = false,
                keyName = "e",
                text = "이미 ASMR을 완료했습니다"
            };
            return true;
        }

        line.showKeyIcon = true;
        line.keyName = "e";

        if (target is InteractableASMR)
        {
            line.text = "ASMR 하기";
            return true;
        }

        if (target is InteractableDoor door)
        {
            line.text = door.IsOpened ? "문 닫기" : "문 열기";
            return true;
        }

        if (target is InteractableHide)
        {
            line.text = "숨기";
            return true;
        }

        if (target is InteractableExitDoor)
        {
            bool inLobby = NewSceneManager.Instance != null
                && NewSceneManager.Instance.IsCurrentSceneLobby();

            // 로비: 맵·난이도 미설정
            if (inLobby && (GameManager.Instance == null || !GameManager.Instance.AreYouReady))
            {
                line.showKeyIcon = false;
                line.keyName = "e";
                line.text = "먼저 맵과 난이도를 설정하세요";
                return true;
            }

            // 인게임: 미션 미달성
            bool missionDone = GameManager.Instance != null
                && GameManager.Instance.AllMissionCompleted;

            if (!inLobby && !missionDone)
            {
                line.showKeyIcon = false;
                line.keyName = "e";
                line.text = "미션 금액을 달성해야합니다";
                return true;
            }

            line.showKeyIcon = true;
            line.keyName = "e";
            line.text = "탈출하기";
            return true;
        }

        if (target is InteractableLobby)
        {
            line.text = "사용하기";
            return true;
        }

        if (target.Data != null && !string.IsNullOrEmpty(target.Data.itemName))
            line.text = target.Data.itemName;
        else
            line.text = "상호작용";

        return true;
    }

    public static bool TryBuildHiddenPrompt(out InteractPromptLine line)
    {
        line = new InteractPromptLine
        {
            showKeyIcon = true,
            keyName = "e",
            text = "나오기"
        };
        return true;
    }

    public static bool TryBuildFlashlightPrompt(PlayerFlashlight flashlight, out InteractPromptLine line)
    {
        line = default;
        if (flashlight == null || !flashlight.CanTurnOn)
            return false;

        line = new InteractPromptLine
        {
            showKeyIcon = true,
            keyName = "f",
            text = "플래시라이트"
        };
        return true;
    }
}
