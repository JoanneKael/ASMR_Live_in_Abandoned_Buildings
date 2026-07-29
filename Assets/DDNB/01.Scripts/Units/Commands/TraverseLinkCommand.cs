using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// NavMesh OffMeshLink(가파른 계단·사다리)를 현재 agent.speed에 맞춰 Lerp 보간으로 통과합니다.
/// 링크는 경로 위의 중간 구간이므로, 통과 후 원래 목적지로 SetDestination하여 경로를 복원합니다.
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

        // 현재 속도 기준으로 이동 시간 계산
        float distance = Vector3.Distance(startPos, endPos);
        float duration = _context.Agent.speed > 0f ? distance / _context.Agent.speed : 1f;
        float timer = 0f;

        while (timer < duration)
        {
            token.ThrowIfCancellationRequested();

            timer += Time.deltaTime;
            _context.Transform.position = Vector3.Lerp(startPos, endPos, timer / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // 링크 종료 지점에 스냅 후 NavMesh에 완료 신호
        _context.Transform.position = endPos;
        _context.Agent.Warp(endPos);
        _context.Agent.CompleteOffMeshLink();

        // 링크 통과 후 원래 목적지(B)로 경로 복원 — 링크는 중간 구간일 뿐 최종 목적지는 유지
        _context.Agent.isStopped = false;
        _context.Agent.SetDestination(_restoreDestination);

        while (_context.Agent.pathPending)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }
}
