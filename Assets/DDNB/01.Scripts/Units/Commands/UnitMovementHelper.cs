using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMesh 이동 대기 및 OffMeshLink(계단)·문 개방을 공통 처리하는 헬퍼.
/// 문은 멈추지 않고 통과형으로 엽니다 (리썰/패니코어식 추격용).
/// </summary>
public static class UnitMovementHelper
{
    private const float DoorFrontDistance = 2.0f;
    /// <summary>유닛이 문 쪽을 대략 바라보는 허용 각도</summary>
    private const float DoorFacingAngle = 55f;
    /// <summary>문 forward 축 대비 유닛 위치 허용 (옆 스침만 제외)</summary>
    private const float DoorApproachMinAbsDot = 0.55f;
    private const float DoorOverlapRadius = 1.8f;
    private const float DoorApproachDistance = 1.05f;
    /// <summary>이 거리 안이면 approach 없이 즉시 개방</summary>
    private const float DoorInstantOpenDistance = 1.6f;
    private const float DestinationRepathThreshold = 0.35f;

    /// <summary>
    /// 저장한 목적지 좌표 기준으로 도착할 때까지 대기합니다.
    /// </summary>
    /// <param name="clearBlockingDoors">
    /// true면 유닛↔목적지 사이 닫힌 문을 논블로킹으로 엽니다.
    /// </param>
    public static async UniTask MoveToDestinationAsync(
        UnitAIContext context,
        Vector3 destination,
        float arrivalThreshold,
        CancellationToken token,
        bool clearBlockingDoors = true)
    {
        await MoveUntilNearAsync(context, destination, arrivalThreshold, token, clearBlockingDoors);
    }

    /// <summary>
    /// 목표까지 nearDistance 이내로 올 때까지 이동합니다.
    /// 도착해도 Agent를 멈추지 않습니다 (패트롤 통과용).
    /// </summary>
    public static async UniTask MoveUntilNearAsync(
        UnitAIContext context,
        Vector3 destination,
        float nearDistance,
        CancellationToken token,
        bool clearBlockingDoors = true)
    {
        if (context.Agent != null)
        {
            context.Agent.autoBraking = false;
            context.Agent.isStopped = false;
        }

        while (context.Agent.pathPending)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        while (!HasArrivedAt(context, destination, nearDistance))
        {
            TryOpenNearbyDoors(context);

            if (clearBlockingDoors)
                ClearBlockingDoorToward(context, destination);

            if (context.Agent.isOnOffMeshLink)
            {
                await new TraverseLinkCommand(context, destination).ExecuteAsync(token);
            }

            EnsureDestination(context, destination);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    public static bool HasArrivedAt(UnitAIContext context, Vector3 destination, float threshold)
    {
        return FlatDistance(context.Transform.position, destination) <= threshold;
    }

    /// <summary>
    /// 막힌 문이 있으면 멈추지 않고 즉시 열거나, 멀면 approach로만 경로를 잠시 돌립니다.
    /// (구 API 호환용 async 래퍼)
    /// </summary>
    public static UniTask ClearBlockingDoorTowardAsync(
        UnitAIContext context,
        Vector3 finalDestination,
        CancellationToken token)
    {
        ClearBlockingDoorToward(context, finalDestination);
        return UniTask.CompletedTask;
    }

    /// <summary>논블로킹 문 처리 — 하드스톱/Face/Delay 없음</summary>
    public static void ClearBlockingDoorToward(UnitAIContext context, Vector3 finalDestination)
    {
        InteractableDoor door = FindClosedDoorBlocking(context.Transform.position, finalDestination);
        if (door == null || door.IsOpened)
            return;

        float distToDoor = FlatDistance(context.Transform.position, door.transform.position);
        if (distToDoor <= DoorInstantOpenDistance)
        {
            door.OpenForAi();
            EnsureDestination(context, finalDestination);
            return;
        }

        // 아직 멀면 문 앞으로만 경로를 잠깐 돌림 (속도 유지)
        Vector3 approach = GetDoorApproachPoint(context.Transform.position, door);
        EnsureDestination(context, approach);
    }

    /// <summary>수평 Yaw만 목표 위치를 바라보도록 보간</summary>
    public static async UniTask FaceTowardPositionAsync(
        UnitAIContext context,
        Vector3 worldTarget,
        float duration,
        CancellationToken token)
    {
        Transform unit = context.Transform;
        if (unit == null) return;

        Vector3 flat = worldTarget - unit.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) return;

        bool prevUpdateRotation = context.Agent != null && context.Agent.updateRotation;
        if (context.Agent != null)
            context.Agent.updateRotation = false;

        Quaternion start = unit.rotation;
        Quaternion end = Quaternion.LookRotation(flat.normalized, Vector3.up);
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
            if (context.Agent != null)
                context.Agent.updateRotation = prevUpdateRotation;
        }
    }

    /// <summary>유닛↔목표 사이 닫힌 InteractableDoor를 찾습니다.</summary>
    public static InteractableDoor FindClosedDoorBlocking(Vector3 from, Vector3 to)
    {
        Vector3 eye = from + Vector3.up * 1.0f;
        Vector3 targetEye = to + Vector3.up * 1.0f;
        Vector3 delta = targetEye - eye;
        float dist = delta.magnitude;
        if (dist < 0.05f) return null;

        int mask = LayerMask.GetMask("Obstacle", "Interactable");
        RaycastHit[] hits = Physics.RaycastAll(
            eye,
            delta / dist,
            dist,
            mask,
            QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0) return null;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            InteractableDoor door = FindDoor(hits[i].collider);
            if (door != null && !door.IsOpened)
                return door;
        }

        return null;
    }

    private static Vector3 GetDoorApproachPoint(Vector3 unitPos, InteractableDoor door)
    {
        Transform doorT = door.transform;
        Vector3 doorPos = doorT.position;

        Vector3 doorFwd = doorT.forward;
        doorFwd.y = 0f;
        if (doorFwd.sqrMagnitude < 0.0001f)
            doorFwd = Vector3.forward;
        else
            doorFwd.Normalize();

        Vector3 toUnit = unitPos - doorPos;
        toUnit.y = 0f;
        float side = Vector3.Dot(toUnit, doorFwd) >= 0f ? 1f : -1f;

        Vector3 approach = doorPos + doorFwd * (side * DoorApproachDistance);
        approach.y = unitPos.y;

        if (NavMesh.SamplePosition(approach, out NavMeshHit hit, 1.25f, NavMesh.AllAreas))
            return hit.position;

        return approach;
    }

    private static void EnsureDestination(UnitAIContext context, Vector3 destination)
    {
        if (FlatDistance(context.Agent.destination, destination) > DestinationRepathThreshold)
            context.Agent.SetDestination(destination);
    }

    /// <summary>가까이 있고 대략 문 방향이면 이동 중 즉시 개방 (옆 스침만 제외)</summary>
    public static void TryOpenNearbyDoors(UnitAIContext context)
    {
        Transform unit = context.Transform;
        Vector3 center = unit.position + unit.forward * 0.35f;
        Collider[] hits = Physics.OverlapSphere(
            center,
            DoorOverlapRadius,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits.Length; i++)
        {
            InteractableDoor door = FindDoor(hits[i]);
            if (door == null || door.IsOpened) continue;

            if (!CanAiOpenDoor(unit, door.transform)) continue;

            door.OpenForAi();
        }
    }

    private static InteractableDoor FindDoor(Collider hit)
    {
        if (hit == null) return null;

        InteractableDoor door = hit.GetComponent<InteractableDoor>();
        if (door != null) return door;

        door = hit.GetComponentInParent<InteractableDoor>();
        if (door != null) return door;

        Transform parent = hit.transform.parent;
        if (parent != null)
            door = parent.GetComponentInChildren<InteractableDoor>(true);

        return door;
    }

    /// <summary>
    /// 1) 문과 근접 2) 대략 문 쪽을 봄 3) 문 앞/뒤 통로에 있음 — 옆 스침만 제외
    /// </summary>
    private static bool CanAiOpenDoor(Transform unit, Transform door)
    {
        Vector3 unitPos = unit.position;
        Vector3 doorPos = door.position;
        unitPos.y = 0f;
        doorPos.y = 0f;

        Vector3 toDoor = doorPos - unitPos;
        float dist = toDoor.magnitude;
        if (dist > DoorFrontDistance || dist < 0.01f) return false;

        Vector3 toDoorDir = toDoor / dist;

        Vector3 unitFwd = unit.forward;
        unitFwd.y = 0f;
        if (unitFwd.sqrMagnitude < 0.0001f) return false;
        unitFwd.Normalize();

        if (Vector3.Angle(unitFwd, toDoorDir) > DoorFacingAngle) return false;

        Vector3 doorFwd = door.forward;
        doorFwd.y = 0f;
        if (doorFwd.sqrMagnitude < 0.0001f) return false;
        doorFwd.Normalize();

        Vector3 fromDoorToUnit = (unitPos - doorPos).normalized;
        if (Mathf.Abs(Vector3.Dot(fromDoorToUnit, doorFwd)) < DoorApproachMinAbsDot) return false;

        return true;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
