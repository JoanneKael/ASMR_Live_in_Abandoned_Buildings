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

    public void InitUnit(UnitData data, Vector3[] patrolPoints)
    {
        unitData = data;
        patrolPositions = patrolPoints;

        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            playerTransform = GameManager.Instance.Player.transform;
        }

        _context.UnitData = unitData;
        _context.PlayerTransform = playerTransform;
        _context.PatrolPositions = patrolPositions;
        _context.CurrentPatrolIndex = 0;
        _context.PatrolDirection = 1;

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
    /// 소음을 듣고 해당 위치로 수색합니다. 추적/공격 중에는 무시합니다.
    /// </summary>
    public void HearNoise(Vector3 noisePos)
    {
        if (currentState == UnitState.Attack || currentState == UnitState.Chase) return;

        _context.InvestigatePosition = noisePos;
        _context.LastHeardPosition = noisePos;

        if (currentState == UnitState.Investigate)
        {
            // 이미 수색 중이면 목적지만 갱신하고 커맨드 재시작
            CancelCurrentCommand();
            _commandCts = new CancellationTokenSource();
            RunCommandAsync(new InvestigateCommand(_context), _commandCts.Token).Forget();
            return;
        }

        ChangeState(UnitState.Investigate);
    }

    /// <summary>
    /// 플레이어가 숨으면 Chase 중일 때 Investigate(위치 B)로 전환합니다.
    /// </summary>
    public void OnPlayerHidden(Vector3 investigatePos)
    {
        if (currentState != UnitState.Chase) return;

        _context.InvestigatePosition = investigatePos;
        _context.LastHeardPosition = investigatePos;
        ChangeState(UnitState.Investigate);
    }

    /// <summary>
    /// 매 프레임 시야 감지를 수행합니다. 플레이어 발견 시 추적 상태로 전환합니다.
    /// </summary>
    private void CheckSensorySystem()
    {
        if (playerTransform == null || currentState == UnitState.Attack) return;
        if (GameManager.Instance != null
            && GameManager.Instance.Player != null
            && GameManager.Instance.Player.CurrentState == PlayerState.Hidden)
            return;

        if (IsPlayerInSight()) ChangeState(UnitState.Chase);
    }

    /// <summary>
    /// 거리, 시야각(FOV), 장애물 레이캐스트를 종합하여 플레이어 감지 여부를 판정합니다.
    /// 수평 거리 + 높이 차로 층을 가르고, Obstacle만 가림으로 취급합니다.
    /// </summary>
    private bool IsPlayerInSight()
    {
        if (playerTransform == null || unitData == null) return false;
        if (GameManager.Instance != null
            && GameManager.Instance.Player != null
            && GameManager.Instance.Player.CurrentState == PlayerState.Hidden)
            return false;

        Vector3 unitPos = transform.position;
        Vector3 playerPos = playerTransform.position;

        // 다른 층(높이 차) 제외
        if (Mathf.Abs(playerPos.y - unitPos.y) > unitData.maxSightHeightDiff) return false;

        // 수평 거리
        Vector3 flat = playerPos - unitPos;
        flat.y = 0f;
        float flatDistance = flat.magnitude;
        if (flatDistance > unitData.sightRange) return false;

        // FOV (수평 방향 기준)
        if (flatDistance > 0.001f)
        {
            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;
            flatForward.Normalize();
            if (Vector3.Angle(flatForward, flat.normalized) > unitData.fovAngle / 2f) return false;
        }

        Vector3 eyePos = unitPos + Vector3.up * 1.0f;
        Vector3 targetEyePos = playerPos + Vector3.up * 1.0f;

        // Ground는 제외 — 바닥 스침으로 시야가 막히지 않게 함
        if (Physics.Linecast(eyePos, targetEyePos, LayerMask.GetMask("Obstacle")))
            return false;

        return true;
    }
}
