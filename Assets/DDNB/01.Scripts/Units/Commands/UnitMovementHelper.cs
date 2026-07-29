using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// NavMesh 이동 대기 및 OffMeshLink(계단) 통과를 공통 처리하는 헬퍼.
/// Patrol / Investigate 등 목적지 이동 커맨드에서 재사용합니다.
/// </summary>
public static class UnitMovementHelper
{
    /// <summary>
    /// 저장한 목적지 좌표 기준으로 도착할 때까지 대기합니다.
    /// remainingDistance는 링크 구간에서 부정확할 수 있으므로 사용하지 않습니다.
    /// 이동 중 OffMeshLink를 만나면 통과 후 원래 목적지로 경로를 복원합니다.
    /// </summary>
    public static async UniTask MoveToDestinationAsync(
        UnitAIContext context,
        Vector3 destination,
        float arrivalThreshold,
        CancellationToken token)
    {
        while (context.Agent.pathPending)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        while (!HasArrivedAt(context, destination, arrivalThreshold))
        {
            if (context.Agent.isOnOffMeshLink)
            {
                await new TraverseLinkCommand(context, destination).ExecuteAsync(token);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    /// <summary>
    /// NavMeshAgent.remainingDistance 대신, 출발 시 저장한 목적지 좌표와의 거리로 도착 여부를 판정합니다.
    /// </summary>
    public static bool HasArrivedAt(UnitAIContext context, Vector3 destination, float threshold)
    {
        return Vector3.Distance(context.Transform.position, destination) <= threshold;
    }
}
