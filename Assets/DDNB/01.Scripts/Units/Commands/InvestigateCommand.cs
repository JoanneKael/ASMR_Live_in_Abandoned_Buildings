using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 수색 상태 커맨드.
/// 마지막 소음 위치로 이동하되, 1m 안에 은신처가 있으면 그쪽으로 가서
/// 마주보고 두리번인 뒤 순찰로 복귀합니다.
/// </summary>
public class InvestigateCommand : IUnitCommand
{
    private readonly UnitAIContext _context;

    /// <summary>소음 지점 기준 은신처 탐색 반경</summary>
    private const float HideNearSoundRadius = 1.0f;
    private const float FaceHideDuration = 0.4f;
    private const float ScanDuration = 2.0f;
    private const float ArrivalDistance = 0.55f;

    public InvestigateCommand(UnitAIContext context)
    {
        _context = context;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        _context.Agent.speed = _context.UnitData.walkSpeed;
        _context.Agent.isStopped = false;
        _context.IsLocomotionLocked = false;

        Vector3 soundPos = _context.InvestigatePosition;
        InteractableHide nearbyHide = InteractableHide.FindNearestNearSound(soundPos, HideNearSoundRadius);

        Vector3 moveTarget;
        Vector3? lookAt = null;

        if (nearbyHide != null)
        {
            // 은신처 앞(exit)으로 걸어가 안쪽(hide)을 마주봄
            moveTarget = nearbyHide.ExitPosition;
            lookAt = nearbyHide.LookAtPosition;
        }
        else
        {
            moveTarget = soundPos;
        }

        _context.Agent.SetDestination(moveTarget);

        await UnitMovementHelper.MoveToDestinationAsync(
            _context,
            moveTarget,
            ArrivalDistance,
            token,
            clearBlockingDoors: true);

        // 도착 후 정지하고 수색
        _context.IsLocomotionLocked = true;
        _context.Agent.isStopped = true;
        _context.Agent.ResetPath();
        _context.Agent.velocity = Vector3.zero;
        _context.AnimatorPlayer?.SetSpeedImmediate(0f);

        if (lookAt.HasValue)
        {
            await UnitMovementHelper.FaceTowardPositionAsync(
                _context,
                lookAt.Value,
                FaceHideDuration,
                token);
        }

        _context.AnimatorPlayer?.PlayScan();
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(ScanDuration), cancellationToken: token);
        }
        finally
        {
            _context.AnimatorPlayer?.StopScan();
            _context.IsLocomotionLocked = false;
            _context.Agent.isStopped = false;
        }

        _context.RequestStateChange?.Invoke(UnitState.Patrol);
    }
}
