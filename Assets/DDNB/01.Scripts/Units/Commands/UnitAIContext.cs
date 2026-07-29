using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 커맨드 클래스들이 공유하는 유닛 AI 실행 컨텍스트.
/// UnitAI의 내부 상태와 컴포넌트 참조를 캡슐화하여 커맨드 간 의존성을 줄입니다.
/// </summary>
public class UnitAIContext
{
    public NavMeshAgent Agent { get; }
    public Transform Transform { get; }
    public UnitData UnitData { get; set; }

    public Transform PlayerTransform { get; set; }

    public Vector3[] PatrolPositions { get; set; }

    public int CurrentPatrolIndex { get; set; }

    /// <summary>수색 목표 지점 (추격 실패 시 마지막으로 본 위치)</summary>
    public Vector3 InvestigatePosition { get; set; }

    /// <summary>플레이어를 마지막으로 본/들은 위치</summary>
    public Vector3 LastHeardPosition { get; set; }

    /// <summary>상태 전환 요청 콜백 (UnitAI.ChangeState에 연결)</summary>
    public Action<UnitState> RequestStateChange { get; set; }

    /// <summary>플레이어 시야 감지 판정 (UnitAI.IsPlayerInSight에 연결)</summary>
    public Func<bool> IsPlayerInSight { get; set; }

    public UnitAIContext(NavMeshAgent agent, Transform transform)
    {
        Agent = agent;
        Transform = transform;
    }

    /// <summary>
    /// 다음 순찰 지점으로 NavMesh 목적지를 설정하고 인덱스를 순환합니다.
    /// </summary>
    /// <returns>설정된 목적지 좌표 (도착 판정·링크 복원에 사용)</returns>
    public Vector3 MoveToNextPatrolPoint()
    {
        if (PatrolPositions == null || PatrolPositions.Length == 0)
        {
            return Transform.position;
        }

        Vector3 target = PatrolPositions[CurrentPatrolIndex];
        Agent.SetDestination(target);
        CurrentPatrolIndex = (CurrentPatrolIndex + 1) % PatrolPositions.Length;
        return target;
    }
}
