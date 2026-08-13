using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// NavMesh OffMeshLink(가파른 계단·사다리)를 현재 agent.speed에 맞춰 Lerp 보간으로 통과합니다.
/// Patrol/Investigate(walk) · Chase(run) 등 현재 속도에 맞는 이동 애니를 유지합니다.
/// </summary>
public class TraverseLinkCommand : IUnitCommand
{
    private readonly UnitAIContext _context;
    private readonly Vector3 _restoreDestination;

    public TraverseLinkCommand(UnitAIContext context, Vector3 restoreDestination)
    {
        _context = context;
        _restoreDestination = restoreDestination;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!_context.Agent.isOnOffMeshLink) return;

        var data = _context.Agent.currentOffMeshLinkData;
        Vector3 startPos = _context.Transform.position;
        Vector3 endPos = data.endPos + Vector3.up * _context.Agent.baseOffset;

        float distance = Vector3.Distance(startPos, endPos);
        float moveSpeed = Mathf.Max(0.01f, _context.Agent.speed);
        float duration = distance / moveSpeed;
        float timer = 0f;

        // 링크 중에는 agent.velocity=0 → Idle이 되지 않도록 현재 상태 속도(walk/run)로 강제
        _context.AnimatorPlayer?.StopScan();
        _context.ForcedLocomotionSpeed = moveSpeed;

        bool prevUpdateRotation = _context.Agent.updateRotation;
        _context.Agent.updateRotation = false;

        Vector3 moveDir = endPos - startPos;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.0001f)
            _context.Transform.rotation = Quaternion.LookRotation(moveDir.normalized, Vector3.up);

        try
        {
            while (timer < duration)
            {
                token.ThrowIfCancellationRequested();

                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                _context.Transform.position = Vector3.Lerp(startPos, endPos, t);

                // UnitAI.Update와 별도로도 매 프레임 보강 (실행 순서 무관하게 유지)
                if (_context.UnitData != null)
                {
                    _context.AnimatorPlayer?.UpdateLocomotion(
                        moveSpeed,
                        _context.UnitData.walkSpeed,
                        _context.UnitData.runSpeed);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _context.Transform.position = endPos;
            _context.Agent.Warp(endPos);
            _context.Agent.CompleteOffMeshLink();

            _context.Agent.isStopped = false;
            _context.Agent.SetDestination(_restoreDestination);

            while (_context.Agent.pathPending)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            _context.ForcedLocomotionSpeed = null;
            _context.Agent.updateRotation = prevUpdateRotation;
        }
    }
}
