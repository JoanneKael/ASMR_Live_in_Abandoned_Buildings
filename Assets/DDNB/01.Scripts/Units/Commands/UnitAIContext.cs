using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 커맨드 클래스들이 공유하는 유닛 AI 실행 컨텍스트.
/// </summary>
public class UnitAIContext
{
    /// <summary>두리번거릴 패트롤 인덱스 (0-based, Inspector 배열 인덱스)</summary>
    private static readonly HashSet<int> PatrolScanIndices = new HashSet<int>
    {
        5, 8, 10, 12, 18, 21, 24, 27, 29, 31, 39
    };

    public NavMeshAgent Agent { get; }
    public Transform Transform { get; }
    public UnitData UnitData { get; set; }

    public Transform PlayerTransform { get; set; }

    public Vector3[] PatrolPositions { get; set; }
    /// <summary>각 패트롤 앵커의 수평 forward</summary>
    public Vector3[] PatrolForwards { get; set; }

    public int CurrentPatrolIndex { get; set; }

    /// <summary>방금 SetDestination한 패트롤 인덱스</summary>
    public int LastPatrolTargetIndex { get; private set; }

    /// <summary>패트롤 진행 방향 (1: 0→끝, -1: 끝→0)</summary>
    public int PatrolDirection { get; set; } = 1;

    public Vector3 InvestigatePosition { get; set; }
    public Vector3 LastHeardPosition { get; set; }

    public Action<UnitState> RequestStateChange { get; set; }
    public Func<bool> IsPlayerInSight { get; set; }
    public UnitAnimatorPlayer AnimatorPlayer { get; set; }

    /// <summary>스캔 중 이동 애니/경로 갱신 잠금</summary>
    public bool IsLocomotionLocked { get; set; }

    /// <summary>
    /// OffMeshLink 등 agent.velocity가 0일 때 사용할 강제 이동 속도.
    /// null이면 일반 velocity 기반 로코모션.
    /// </summary>
    public float? ForcedLocomotionSpeed { get; set; }

    public UnitAIContext(NavMeshAgent agent, Transform transform)
    {
        Agent = agent;
        Transform = transform;
    }

    public static bool IsPatrolScanIndex(int index)
    {
        return PatrolScanIndices.Contains(index);
    }

    public bool IsLastTargetScanPoint()
    {
        return IsPatrolScanIndex(LastPatrolTargetIndex);
    }

    /// <summary>해당 패트롤 포인트의 수평 정면 방향</summary>
    public Vector3 GetPatrolForward(int index)
    {
        if (PatrolForwards == null || index < 0 || index >= PatrolForwards.Length)
            return Transform.forward;

        Vector3 fwd = PatrolForwards[index];
        if (fwd.sqrMagnitude < 0.0001f)
            return Transform.forward;
        return fwd;
    }

    /// <summary>
    /// 다음 순찰 지점으로 이동. 0→마지막→0 왕복(핑퐁).
    /// </summary>
    public Vector3 MoveToNextPatrolPoint()
    {
        if (PatrolPositions == null || PatrolPositions.Length == 0)
            return Transform.position;

        LastPatrolTargetIndex = CurrentPatrolIndex;
        Vector3 target = PatrolPositions[CurrentPatrolIndex];
        Agent.SetDestination(target);

        if (PatrolPositions.Length == 1)
            return target;

        AdvancePatrolIndex();
        return target;
    }

    private void AdvancePatrolIndex()
    {
        int next = CurrentPatrolIndex + PatrolDirection;
        if (next >= PatrolPositions.Length || next < 0)
        {
            PatrolDirection *= -1;
            next = CurrentPatrolIndex + PatrolDirection;
            next = Mathf.Clamp(next, 0, PatrolPositions.Length - 1);
        }

        CurrentPatrolIndex = next;
    }
}