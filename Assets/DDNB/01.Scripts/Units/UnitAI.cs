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
    public UnitState CurrentState => currentState;

    /// <summary>OffMeshLink 등 velocity=0일 때 애니/발소리용 강제 속도</summary>
    public float? ForcedLocomotionSpeed => _context != null ? _context.ForcedLocomotionSpeed : null;

    [Header("Patrol")]
    private Vector3[] patrolPositions;
    private Vector3[] patrolForwards;

    [Header("NavMesh Clearance")]
    [Tooltip("Bake Agent Radius와 같게 유지 (문 통과). 키우면 좁은 문을 못 지남.")]
    [SerializeField] private float agentClearanceRadius = 0.1f;

    [Tooltip("NavMesh 가장자리(벽)에서 이 거리보다 가까우면 안쪽으로 살짝 밀어냄. radius와 별개.")]
    [SerializeField] private float wallEdgeClearance = 0.2f;

    [Tooltip("벽 가장자리 밀어내기 세기")]
    [SerializeField] private float wallPushStrength = 2.2f;

    [Header("Chase")]
    private Transform playerTransform;

    private UnitAnimatorPlayer animatorPlayer;

    // 커맨드 패턴 실행 컨텍스트 및 취소 토큰
    private UnitAIContext _context;
    private CancellationTokenSource _commandCts;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // OffMeshLink는 TraverseLinkCommand에서 수동 처리

        animatorPlayer = GetComponent<UnitAnimatorPlayer>();
        if (animatorPlayer == null)
            animatorPlayer = gameObject.AddComponent<UnitAnimatorPlayer>();

        if (GetComponent<UnitAudio>() == null)
            gameObject.AddComponent<UnitAudio>();

        _context = new UnitAIContext(agent, transform)
        {
            RequestStateChange = ChangeState,
            IsPlayerInSight = IsPlayerInSight,
            AnimatorPlayer = animatorPlayer
        };

        agent.autoTraverseOffMeshLink = false;
        ConfigureAgentMovement();
    }

    /// <summary>
    /// radius는 bake(문폭)와 맞추고, 벽 여유는 edge push로 처리합니다.
    /// </summary>
    private void ConfigureAgentMovement()
    {
        // 문 통과용 — bake Agent Radius(~0.2)와 동일하게
        agent.radius = Mathf.Clamp(agentClearanceRadius, 0.15f, 0.25f);

        // 리썰/패니코어식: 중간 가속·회전으로 붙는 느낌
        agent.angularSpeed = 220f;
        agent.acceleration = 16f;
        agent.stoppingDistance = 0.8f;
        agent.autoBraking = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    public void InitUnit(UnitData data, Transform[] patrolAnchors)
    {
        unitData = data;
        BuildPatrolArrays(patrolAnchors);

        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            playerTransform = GameManager.Instance.Player.transform;
        }

        ConfigureAgentMovement();

        _context.UnitData = unitData;
        _context.PlayerTransform = playerTransform;
        _context.PatrolPositions = patrolPositions;
        _context.PatrolForwards = patrolForwards;
        _context.CurrentPatrolIndex = 0;
        _context.PatrolDirection = 1;
        _context.AnimatorPlayer = animatorPlayer;
        _context.IsLocomotionLocked = false;

        if (unitData.unitModelPrefab != null)
        {
            var unit = Instantiate(unitData.unitModelPrefab, transform);
            unit.transform.localPosition = new Vector3(0f, -1f, 0f);
            animatorPlayer.BindFromModel(unit, unitData.animatorOverride);
        }
        else
        {
            Debug.LogWarning("[UnitAI] UnitData.unitModelPrefab이 비어 있습니다.");
        }

        ChangeState(UnitState.Patrol);
    }

    private void BuildPatrolArrays(Transform[] anchors)
    {
        if (anchors == null || anchors.Length == 0)
        {
            patrolPositions = null;
            patrolForwards = null;
            return;
        }

        patrolPositions = new Vector3[anchors.Length];
        patrolForwards = new Vector3[anchors.Length];

        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] == null)
            {
                patrolPositions[i] = Vector3.zero;
                patrolForwards[i] = Vector3.forward;
                continue;
            }

            patrolPositions[i] = anchors[i].position;
            Vector3 fwd = anchors[i].forward;
            fwd.y = 0f;
            patrolForwards[i] = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
        }
    }

    private void Update()
    {
        CheckSensorySystem();
        UpdateAnimatorLocomotion();
    }

    private void LateUpdate()
    {
        // Agent가 속도를 잡은 뒤, 벽(NavMesh edge)에서만 살짝 밀어 자연스러운 여유
        ApplyWallEdgeClearance();
    }

    /// <summary>
    /// radius를 키우지 않고, 가장자리에 붙었을 때만 통로 안쪽으로 속도를 보정해
    /// 문폭은 유지하면서 벽 클리핑을 줄입니다.
    /// </summary>
    private void ApplyWallEdgeClearance()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (agent.isStopped) return;
        if (currentState == UnitState.Attack) return;
        if (_context != null && _context.IsLocomotionLocked) return;
        if (wallEdgeClearance <= 0.01f || wallPushStrength <= 0f) return;

        if (!NavMesh.FindClosestEdge(agent.nextPosition, out NavMeshHit edge, NavMesh.AllAreas))
            return;

        if (edge.distance >= wallEdgeClearance) return;

        // normal은 내비 가능 영역 안쪽을 가리킴 → 벽에서 멀어지는 방향
        Vector3 push = edge.normal;
        push.y = 0f;
        if (push.sqrMagnitude < 0.0001f) return;
        push.Normalize();

        float t = 1f - Mathf.Clamp01(edge.distance / wallEdgeClearance);
        t *= t; // 가까울수록 더 세게

        Vector3 v = agent.velocity;
        v += push * (wallPushStrength * t);

        float maxSpeed = Mathf.Max(agent.speed, 0.01f);
        if (v.sqrMagnitude > maxSpeed * maxSpeed)
            v = v.normalized * maxSpeed;

        agent.velocity = v;
    }

    private void UpdateAnimatorLocomotion()
    {
        if (animatorPlayer == null || !animatorPlayer.IsBound || unitData == null) return;
        if (currentState == UnitState.Attack) return;
        if (_context != null && _context.IsLocomotionLocked) return;

        float horizontalSpeed;
        if (_context != null && _context.ForcedLocomotionSpeed.HasValue)
        {
            horizontalSpeed = _context.ForcedLocomotionSpeed.Value;
        }
        else
        {
            Vector3 v = agent.velocity;
            v.y = 0f;
            horizontalSpeed = v.magnitude;

            // Investigate / Patrol 스캔 정지 중에는 Speed로 Scan을 덮지 않음
            if (agent.isStopped && horizontalSpeed < 0.05f
                && (currentState == UnitState.Investigate || currentState == UnitState.Patrol))
                return;
        }

        animatorPlayer.UpdateLocomotion(horizontalSpeed, unitData.walkSpeed, unitData.runSpeed);
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

        if (currentState == UnitState.Investigate || currentState == UnitState.Patrol)
        {
            animatorPlayer?.StopScan();
            if (_context != null)
                _context.IsLocomotionLocked = false;
        }

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
