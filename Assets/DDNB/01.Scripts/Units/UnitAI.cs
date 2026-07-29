using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 유닛 AI 관리자.
/// 상태(State)에 따라 커맨드를 생성·교체하고, CancellationToken으로 실행 중인 비동기 작업을 안전하게 중단합니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class UnitAI : MonoBehaviour
{
    [Header("Data")]
    private UnitData unitData;

    [Header("Unit")]
    private NavMeshAgent agent;
    [SerializeField] private UnitState currentState;

    [Header("Patrol")]
    private Vector3[] patrolPositions;

    [Header("Chase")]
    private Transform playerTransform;

    // 커맨드 패턴 실행 컨텍스트 및 취소 토큰
    private UnitAIContext _context;
    private CancellationTokenSource _commandCts;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // OffMeshLink는 TraverseLinkCommand에서 수동 처리

        _context = new UnitAIContext(agent, transform)
        {
            RequestStateChange = ChangeState,
            IsPlayerInSight = IsPlayerInSight
        };

        agent.autoTraverseOffMeshLink = false;
    }

    public void InitUnit(UnitData data, Vector3[] points, int startPatrolIndex)
    {
        unitData = data;
        patrolPositions = points;

        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            playerTransform = GameManager.Instance.Player.transform;
        }

        _context.UnitData = unitData;
        _context.PlayerTransform = playerTransform;
        _context.PatrolPositions = patrolPositions;
        _context.CurrentPatrolIndex = startPatrolIndex;

        if (unitData.unitModelPrefab != null)
        {
            var unit = Instantiate(unitData.unitModelPrefab, transform);
            unit.transform.localPosition = new Vector3(0f, -1f, 0f);
        }

        ChangeState(UnitState.Patrol);
    }

    private void Update()
    {
        CheckSensorySystem();
    }

    private void OnDestroy()
    {
        CancelCurrentCommand();
    }

    /// <summary>
    /// 상태를 변경하고 해당 상태의 커맨드를 비동기 실행합니다.
    /// 기존 실행 중인 커맨드는 CancellationTokenSource.Cancel()로 즉시 중단됩니다.
    /// </summary>
    private void ChangeState(UnitState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        CancelCurrentCommand();
        _context.PlayerTransform = playerTransform;

        _commandCts = new CancellationTokenSource();
        IUnitCommand command = CreateCommandForState(newState);

        RunCommandAsync(command, _commandCts.Token).Forget();
    }

    /// <summary>
    /// 현재 상태에 맞는 커맨드 인스턴스를 생성합니다.
    /// </summary>
    private IUnitCommand CreateCommandForState(UnitState state)
    {
        return state switch
        {
            UnitState.Patrol => new PatrolCommand(_context),
            UnitState.Investigate => new InvestigateCommand(_context),
            UnitState.Chase => new ChaseCommand(_context),
            UnitState.Attack => new AttackCommand(_context),
            _ => new PatrolCommand(_context)
        };
    }

    /// <summary>
    /// 커맨드를 비동기 실행합니다. 상태 전환 시 발생하는 OperationCanceledException은 정상 처리합니다.
    /// </summary>
    private async UniTaskVoid RunCommandAsync(IUnitCommand command, CancellationToken token)
    {
        try
        {
            await command.ExecuteAsync(token);
        }
        catch (OperationCanceledException)
        {
            // ChangeState() 호출로 인한 정상적인 커맨드 중단
        }
    }

    /// <summary>
    /// 실행 중인 커맨드의 CancellationTokenSource를 취소·해제합니다.
    /// </summary>
    private void CancelCurrentCommand()
    {
        if (_commandCts == null) return;

        _commandCts.Cancel();
        _commandCts.Dispose();
        _commandCts = null;
    }

    /// <summary>
    /// 매 프레임 시야 감지를 수행합니다. 플레이어 발견 시 추적 상태로 전환합니다.
    /// </summary>
    private void CheckSensorySystem()
    {
        if (playerTransform == null || currentState == UnitState.Attack) return;

        if (IsPlayerInSight()) ChangeState(UnitState.Chase);
    }

    /// <summary>
    /// 거리, 시야각(FOV), 장애물 레이캐스트를 종합하여 플레이어 감지 여부를 판정합니다.
    /// </summary>
    private bool IsPlayerInSight()
    {
        if (playerTransform == null || unitData == null) return false;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > unitData.sightRange) return false;

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToPlayer) > unitData.fovAngle / 2f) return false;

        Vector3 eyePos = transform.position + Vector3.up;// * 1.5f;
        Vector3 targetEyePos = playerTransform.position + Vector3.up;// * 1.5f;

        if (Physics.Linecast(eyePos, targetEyePos, LayerMask.GetMask("Obstacle", "Ground")))
        {
            return false;
        }

        return true;
    }
}
