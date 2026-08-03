using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 수색 상태 커맨드.
/// investigatePosition(소리·마지막 목격 지점)으로 이동한 뒤 잠시 주변을 살피고 순찰로 복귀합니다.
/// </summary>
public class InvestigateCommand : IUnitCommand
{
    private readonly UnitAIContext _context;

    public InvestigateCommand(UnitAIContext context)
    {
        _context = context;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        _context.Agent.speed = _context.UnitData.walkSpeed;
        _context.Agent.isStopped = false;
        Vector3 investigateTarget = _context.InvestigatePosition;
        _context.Agent.SetDestination(investigateTarget);

        await UnitMovementHelper.MoveToDestinationAsync(_context, investigateTarget, 1f, token);

        _context.Agent.isStopped = true;

        // 목적지 도착 후 주변 관찰 대기
        await UniTask.Delay(TimeSpan.FromSeconds(2.0f), cancellationToken: token);


        _context.RequestStateChange?.Invoke(UnitState.Patrol);
    }
}
