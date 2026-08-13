using UnityEngine;

/// <summary>
/// 크로스헤어 표시 여부.
/// 평소·로비 ON.
/// OFF: ASMR / 은신 / ExitDoor E홀드 / 스턴·넉아웃.
/// 일어나거나(Idle 복귀)·하루 경과 후 자동 ON.
/// </summary>
public static class CrosshairVisibility
{
    public static bool ShouldShow()
    {
        // 플레이어가 아직 없으면 기본 표시 유지
        if (GameManager.Instance == null || GameManager.Instance.Player == null)
            return true;

        Player player = GameManager.Instance.Player;
        PlayerState state = player.CurrentState;

        if (state == PlayerState.ASMR)
            return false;

        if (state == PlayerState.Hidden)
            return false;

        // 스턴 / 넉아웃 연출 중
        if (state == PlayerState.Stunned)
            return false;

        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (interaction != null && interaction.IsHidingCrosshairForInteraction)
            return false;

        return true;
    }
}
