using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// NavMesh 이동 대기 및 OffMeshLink(계단) 통과를 공통 처리하는 헬퍼.
/// Patrol / Investigate 등 목적지 이동 커맨드에서 재사용합니다.
/// </summary>
public static class UnitMovementHelper
{
    private const float DoorFrontDistance = 1.15f; // 문과의 최대 거리
    private const float DoorFacingAngle = 45f;     // 유닛이 문을 바라보는 허용 각도
    private const float DoorApproachDot = 0.35f;   // 문 forward 축 정렬(옆에서 스치면 제외)
    private const float DoorOverlapRadius = 1.3f;

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
            TryOpenNearbyDoors(context);

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

    /// <summary>문 앞에 있고 문을 바라볼 때만 닫힌 문을 연다.</summary>
    public static void TryOpenNearbyDoors(UnitAIContext context)
    {
        Transform unit = context.Transform;
        Vector3 center = unit.position + unit.forward * 0.4f;
        Collider[] hits = Physics.OverlapSphere(center, DoorOverlapRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            InteractableDoor door = hits[i].GetComponent<InteractableDoor>();
            if (door == null) door = hits[i].GetComponentInParent<InteractableDoor>();
            if (door == null || door.IsOpened) continue;

            if (!IsUnitInFrontOfDoor(unit, door.transform)) continue;

            door.OpenForAi();
        }
    }

    /// <summary>
    /// 유닛이 문 앞에 있는지 판정:
    /// 1) 거리 근접 2) 유닛이 문 쪽을 바라봄 3) 문 forward 축(앞/뒤) 쪽에 위치
    /// </summary>
    private static bool IsUnitInFrontOfDoor(Transform unit, Transform door)
    {
        Vector3 unitPos = unit.position;
        Vector3 doorPos = door.position;
        unitPos.y = 0f;
        doorPos.y = 0f;

        Vector3 toDoor = doorPos - unitPos;
        float dist = toDoor.magnitude;
        if (dist > DoorFrontDistance || dist < 0.01f) return false;

        Vector3 unitFwd = unit.forward;
        unitFwd.y = 0f;
        unitFwd.Normalize();
        if (Vector3.Angle(unitFwd, toDoor.normalized) > DoorFacingAngle) return false;

        Vector3 doorFwd = door.forward;
        doorFwd.y = 0f;
        if (doorFwd.sqrMagnitude < 0.0001f) return true;
        doorFwd.Normalize();

        // 문 앞/뒤 통로에 있는지 (옆에서 스치는 경우 제외)
        Vector3 fromDoorToUnit = (unitPos - doorPos).normalized;
        if (Mathf.Abs(Vector3.Dot(fromDoorToUnit, doorFwd)) < DoorApproachDot) return false;

        return true;
    }
}
