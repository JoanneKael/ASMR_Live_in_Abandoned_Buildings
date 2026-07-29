using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 추적 상태 커맨드.
/// 플레이어를 runSpeed로 추격하며, 시야에서 사라지면 lostTimer를 누적합니다.
/// maxLostTime 초과 시 마지막 목격 위치로 수색 상태로 전환합니다.
/// </summary>
public class ChaseCommand : IUnitCommand
{
    private readonly UnitAIContext _context;

    private const float MaxLostTime = 2.0f;
    private const float DestinationUpdateThreshold = 0.3f;
    private const float AttackDistance = 1.5f;
    private const float AttackHeightDiff = 0.8f;

    public ChaseCommand(UnitAIContext context)
    {
        _context = context;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        _context.Agent.speed = _context.UnitData.runSpeed;
        _context.Agent.isStopped = false;

        float lostTimer = 0f;

        while (!token.IsCancellationRequested)
        {
            // 플레이어 참조 소실 시 순찰로 복귀
            if (_context.PlayerTransform == null)
            {
                Debug.LogWarning("Player Transform이 Null입니다. Patrol 상태로 복귀합니다.");
                _context.RequestStateChange?.Invoke(UnitState.Patrol);
                return;
            }

            if (_context.IsPlayerInSight())
            {
                // 시야 내: 타이머 리셋 및 목적지 갱신
                lostTimer = 0f;
                _context.LastHeardPosition = _context.PlayerTransform.position;

                if (Vector3.Distance(_context.Agent.destination, _context.PlayerTransform.position) > DestinationUpdateThreshold)
                {
                    _context.Agent.SetDestination(_context.PlayerTransform.position);
                }
            }
            else
            {
                // 시야 이탈: 타이머 누적 후 수색 전환
                lostTimer += Time.deltaTime;

                if (lostTimer >= MaxLostTime)
                {
                    _context.InvestigatePosition = _context.LastHeardPosition;
                    _context.RequestStateChange?.Invoke(UnitState.Investigate);
                    return;
                }
            }

            // 이동 중 OffMeshLink(계단) 통과 — 현재 추격 목적지를 저장해 링크 후 경로 복원
            if (_context.Agent.isOnOffMeshLink)
            {
                Vector3 chaseTarget = _context.IsPlayerInSight()
                    ? _context.PlayerTransform.position
                    : _context.Agent.destination;

                await new TraverseLinkCommand(_context, chaseTarget).ExecuteAsync(token);
            }

            // 공격 범위 및 시야 조건 충족 시 공격 상태로 전환
            float distance = Vector3.Distance(_context.Transform.position, _context.PlayerTransform.position);
            float heightDiff = Mathf.Abs(_context.Transform.position.y - _context.PlayerTransform.position.y);

            if (distance <= AttackDistance && heightDiff <= AttackHeightDiff && _context.IsPlayerInSight())
            {
                _context.RequestStateChange?.Invoke(UnitState.Attack);
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }
}
