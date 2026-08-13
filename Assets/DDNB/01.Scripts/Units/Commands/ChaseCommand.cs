using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 추적 상태 커맨드 (리썰/패니코어식).
/// 플레이어·마지막 목격 위치로 경로만 계속 갱신하며 달리고,
/// 문은 멈추지 않고 통과형으로 엽니다.
/// </summary>
public class ChaseCommand : IUnitCommand
{
    private readonly UnitAIContext _context;

    private const float MaxLostTime = 2.0f;
    private const float AttackDistance = 1.4f;
    private const float AttackHeightDiff = 0.8f;
    private const float RepathInterval = 0.12f;
    private const float PredictSeconds = 0.15f;

    public ChaseCommand(UnitAIContext context)
    {
        _context = context;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        _context.Agent.speed = _context.UnitData.runSpeed;
        _context.Agent.isStopped = false;
        _context.Agent.autoBraking = false;
        _context.Agent.stoppingDistance = 0.8f;
        _context.IsLocomotionLocked = false;

        float lostTimer = 0f;
        float repathTimer = 0f;
        Rigidbody playerRb = null;

        while (!token.IsCancellationRequested)
        {
            if (_context.PlayerTransform == null)
            {
                Debug.LogWarning("Player Transform이 Null입니다. Patrol 상태로 복귀합니다.");
                _context.RequestStateChange?.Invoke(UnitState.Patrol);
                return;
            }

            if (playerRb == null)
                playerRb = _context.PlayerTransform.GetComponent<Rigidbody>();

            bool inSight = _context.IsPlayerInSight != null && _context.IsPlayerInSight();
            Vector3 chaseTarget;

            if (inSight)
            {
                lostTimer = 0f;
                chaseTarget = PredictPlayerPosition(playerRb);
                _context.LastHeardPosition = _context.PlayerTransform.position;
            }
            else
            {
                lostTimer += Time.deltaTime;
                chaseTarget = _context.LastHeardPosition;

                if (lostTimer >= MaxLostTime)
                {
                    _context.InvestigatePosition = _context.LastHeardPosition;
                    _context.RequestStateChange?.Invoke(UnitState.Investigate);
                    return;
                }
            }

            // 경로만 짧은 간격으로 갱신 — 멈추지 않음
            repathTimer += Time.deltaTime;
            if (repathTimer >= RepathInterval || !_context.Agent.hasPath)
            {
                repathTimer = 0f;
                _context.Agent.SetDestination(chaseTarget);
            }

            // 통과형 문 개방 (하드스톱 없음)
            UnitMovementHelper.TryOpenNearbyDoors(_context);
            UnitMovementHelper.ClearBlockingDoorToward(_context, chaseTarget);

            if (_context.Agent.isOnOffMeshLink)
            {
                await new TraverseLinkCommand(_context, chaseTarget).ExecuteAsync(token);
            }

            float distance = Vector3.Distance(_context.Transform.position, _context.PlayerTransform.position);
            float heightDiff = Mathf.Abs(_context.Transform.position.y - _context.PlayerTransform.position.y);

            if (distance <= AttackDistance && heightDiff <= AttackHeightDiff && inSight)
            {
                _context.RequestStateChange?.Invoke(UnitState.Attack);
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private Vector3 PredictPlayerPosition(Rigidbody playerRb)
    {
        Vector3 pos = _context.PlayerTransform.position;
        if (playerRb == null) return pos;

        Vector3 vel = playerRb.linearVelocity;
        vel.y = 0f;
        return pos + vel * PredictSeconds;
    }
}
