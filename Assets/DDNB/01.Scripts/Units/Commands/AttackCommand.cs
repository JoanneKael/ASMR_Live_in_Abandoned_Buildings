using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 공격 상태 커맨드.
/// 유닛·플레이어가 서로 정면을 마주보게 한 뒤 공격 연출을 진행합니다.
/// </summary>
public class AttackCommand : IUnitCommand
{
    private readonly UnitAIContext _context;

    private const float FaceDuration = 0.45f;
    private const float PreAttackWait = 0.2f;
    /// <summary>기절/넉아웃 UI가 완전 암전에 가까운 상태가 될 때까지 대기</summary>
    private const float FullBlackoutWait = 1.75f;

    public AttackCommand(UnitAIContext context)
    {
        _context = context;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        _context.Agent.isStopped = true;
        _context.Agent.ResetPath();
        _context.Agent.velocity = Vector3.zero;
        Debug.Log("공격 시작!");

        _context.AnimatorPlayer?.SetSpeedImmediate(0f);

        Player player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player == null)
        {
            DespawnSelf();
            return;
        }

        if (ASMRManager.Instance != null)
            ASMRManager.Instance.InterruptByCombat();

        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputBlocked(true);

        player.ChangePlayerState(PlayerState.Stunned);

        // 서로 정면을 마주보게 회전
        PlayerRotation rotation = player.GetComponent<PlayerRotation>();
        Vector3 unitLookAt = player.transform.position;
        Vector3 playerLookAt = _context.Transform.position + Vector3.up * 1.0f;

        UniTask unitFace = FaceUnitTowardAsync(unitLookAt, FaceDuration, token);
        UniTask playerFace = rotation != null
            ? rotation.FaceTargetAsync(playerLookAt, FaceDuration, token)
            : UniTask.Delay(TimeSpan.FromSeconds(FaceDuration), cancellationToken: token);

        await UniTask.WhenAll(unitFace, playerFace);
        await UniTask.Delay(TimeSpan.FromSeconds(PreAttackWait), cancellationToken: token);

        UnitAudio unitAudio = _context.Transform != null
            ? _context.Transform.GetComponent<UnitAudio>()
            : null;
        unitAudio?.PlayGhostAttack();

        _context.AnimatorPlayer?.PlayAttack();

        GameEvents.RaiseUnitAttackSucceeded();

        await UniTask.Delay(TimeSpan.FromSeconds(FullBlackoutWait), cancellationToken: token);

        DespawnSelf();
    }

    /// <summary>유닛 Yaw만 목표를 바라보도록 보간</summary>
    private async UniTask FaceUnitTowardAsync(Vector3 targetWorldPos, float duration, CancellationToken token)
    {
        Transform unit = _context.Transform;
        if (unit == null) return;

        Vector3 flat = targetWorldPos - unit.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) return;

        bool prevUpdateRotation = _context.Agent != null && _context.Agent.updateRotation;
        if (_context.Agent != null)
            _context.Agent.updateRotation = false;

        Quaternion start = unit.rotation;
        Quaternion end = Quaternion.LookRotation(flat.normalized, Vector3.up);
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        try
        {
            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                unit.rotation = Quaternion.Slerp(start, end, t);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            unit.rotation = end;
        }
        finally
        {
            if (_context.Agent != null)
                _context.Agent.updateRotation = prevUpdateRotation;
        }
    }

    private void DespawnSelf()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnUnitDespawned();

        if (_context.Transform != null)
            UnityEngine.Object.Destroy(_context.Transform.gameObject);
    }
}
