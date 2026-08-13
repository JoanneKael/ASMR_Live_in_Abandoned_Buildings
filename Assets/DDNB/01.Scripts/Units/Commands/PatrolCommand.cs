using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 순찰 상태 커맨드.
/// 일반 포인트: 감속 없이 근처만 스치면 바로 다음으로 (슉 통과).
/// 스캔 포인트: 풀스피드로 접근 → 도착 시 즉시 하드스톱 → 두리번.
/// </summary>
public class PatrolCommand : IUnitCommand
{
    private readonly UnitAIContext _context;

    private const float PatrolScanDuration = 2.0f;
    private const float FaceScanDuration = 0.35f;

    /// <summary>스캔 포인트: 이 거리 안이면 즉시 하드스톱</summary>
    private const float ArrivalScan = 0.55f;

    /// <summary>
    /// 통과 포인트: 이 거리 안에 들어오면 다음 목적지로 전환.
    /// 너무 크면 조기 꺾임/지그재그가 생김.
    /// </summary>
    private const float PassSwitchDistance = 1.1f;

    public PatrolCommand(UnitAIContext context)
    {
        _context = context;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        _context.Agent.speed = _context.UnitData.walkSpeed;
        _context.Agent.isStopped = false;
        _context.Agent.autoBraking = false;
        // 통과 중 path 끝 조기 감속을 줄이되, 기본 튜닝(0.8)은 스캔 도착에 사용
        _context.Agent.stoppingDistance = 0.15f;
        _context.IsLocomotionLocked = false;
        _context.AnimatorPlayer?.StopScan();

        while (!token.IsCancellationRequested)
        {
            Vector3 patrolTarget = _context.MoveToNextPatrolPoint();
            int targetIndex = _context.LastPatrolTargetIndex;
            bool isScanPoint = UnitAIContext.IsPatrolScanIndex(targetIndex);

            _context.Agent.autoBraking = false;
            _context.Agent.stoppingDistance = isScanPoint ? 0.5f : 0.15f;
            _context.Agent.isStopped = false;

            if (isScanPoint)
            {
                // 풀스피드로 접근하다 반경 안이면 즉시 정지 (감속 곡선 대신 하드스톱)
                await UnitMovementHelper.MoveToDestinationAsync(
                    _context,
                    patrolTarget,
                    ArrivalScan,
                    token,
                    clearBlockingDoors: true);

                await PerformPatrolScanAsync(targetIndex, token);
            }
            else
            {
                // 통과: 가까이 오면 바로 break → 루프가 다음 SetDestination (속도 유지)
                await UnitMovementHelper.MoveUntilNearAsync(
                    _context,
                    patrolTarget,
                    PassSwitchDistance,
                    token,
                    clearBlockingDoors: true);
            }
        }
    }

    private async UniTask PerformPatrolScanAsync(int patrolIndex, CancellationToken token)
    {
        _context.IsLocomotionLocked = true;
        StopAgentImmediately();
        _context.AnimatorPlayer?.SetSpeedImmediate(0f);

        Vector3 faceDir = _context.GetPatrolForward(patrolIndex);
        await FaceDirectionAsync(faceDir, FaceScanDuration, token);

        _context.AnimatorPlayer?.PlayScan();

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(PatrolScanDuration), cancellationToken: token);
        }
        finally
        {
            _context.AnimatorPlayer?.StopScan();
            _context.IsLocomotionLocked = false;
            _context.Agent.isStopped = false;
            _context.Agent.autoBraking = false;
            _context.Agent.stoppingDistance = 0.15f;
        }
    }

    private void StopAgentImmediately()
    {
        // 감속 없이 속도 0으로 끊기
        _context.Agent.isStopped = true;
        _context.Agent.ResetPath();
        _context.Agent.velocity = Vector3.zero;
    }

    private async UniTask FaceDirectionAsync(Vector3 flatForward, float duration, CancellationToken token)
    {
        Transform unit = _context.Transform;
        if (unit == null) return;

        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.0001f) return;
        flatForward.Normalize();

        bool prevUpdateRotation = _context.Agent != null && _context.Agent.updateRotation;
        if (_context.Agent != null)
            _context.Agent.updateRotation = false;

        Quaternion start = unit.rotation;
        Quaternion end = Quaternion.LookRotation(flatForward, Vector3.up);
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
}
