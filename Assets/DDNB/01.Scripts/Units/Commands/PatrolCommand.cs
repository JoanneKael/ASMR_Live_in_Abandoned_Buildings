using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 순찰 상태 커맨드.
/// patrolPositions를 순환하며 목적지에 도착할 때마다 잠시 대기 후 다음 지점으로 이동합니다.
/// </summary>
public class PatrolCommand : IUnitCommand
{
    private readonly UnitAIContext _context;

    public PatrolCommand(UnitAIContext context)
    {
        _context = context;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        _context.Agent.speed = _context.UnitData.walkSpeed;
        _context.Agent.isStopped = false;

        // 상태 전환(CancellationToken 취소) 전까지 무한 순찰
        while (!token.IsCancellationRequested)
        {
            Vector3 patrolTarget = _context.MoveToNextPatrolPoint();

            await UnitMovementHelper.MoveToDestinationAsync(_context, patrolTarget, 0.5f, token);

            // 도착 후 짧은 대기 (다음 패트롤 포인트로 이동 준비)
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: token);
        }
    }
}
