using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class UnitAI : MonoBehaviour
{
    [Header("Data")]
    private UnitData unitData;

    [Header("Unit")]
    private NavMeshAgent agent;
    [SerializeField] private UnitState currentState;
    private Coroutine currentCoroutine;

    [Header("Patrol")]
    private Vector3[] patrolPositions;
    private int currentPatrolIndex;

    [Header("Investigate")]
    private Vector3 lastHeardPosition;

    [Header("Chase")]
    private Transform playerTransform;
    private Vector3 investigatePosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;
    }

    public void InitUnit(UnitData data, Vector3[] points, int startPatrolIndex)
    {
        unitData = data;
        patrolPositions = points;
        currentPatrolIndex = startPatrolIndex;

        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            playerTransform = GameManager.Instance.Player.transform;
        }

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

    private void ChangeState(UnitState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);

        switch (currentState)
        {
            case UnitState.Patrol:
                currentCoroutine = StartCoroutine(IE_PatrolState());
                break;
            case UnitState.Investigate:
                currentCoroutine = StartCoroutine(IE_InvestigateState());
                break;
            case UnitState.Chase:
                currentCoroutine = StartCoroutine(IE_ChaseState());
                break;
            case UnitState.Attack:
                currentCoroutine = StartCoroutine(IE_AttackState());
                break;
        }
    }

    #region Coroutines
    // 1. 순찰 상태
    private IEnumerator IE_PatrolState()
    {
        agent.speed = unitData.walkSpeed;
        agent.isStopped = false;

        while (true)
        {
            MoveToNextPatrolPoint();

            Debug.Log("이동 시작!!");

            while (agent.pathPending) yield return null;
            while (agent.remainingDistance > 0.5f)
            {
                if (agent.isOnOffMeshLink)
                {
                    yield return StartCoroutine(IE_CheckAndTraverseLink());
                }

                yield return null;
            }
            Debug.Log("목적지 도착! 다음 패트롤 포인트로 이동 준비...");

            yield return new WaitForSeconds(0.2f);
        }
    }

    // 2. 수색 상태 (소리 난 곳 조사)
    private IEnumerator IE_InvestigateState()
    {
        agent.speed = unitData.walkSpeed;
        agent.isStopped = false;
        agent.SetDestination(investigatePosition);

        while (agent.pathPending) yield return null;
        while (agent.remainingDistance > 0.5f)
        {
            if (agent.isOnOffMeshLink)
            {
                yield return StartCoroutine(IE_CheckAndTraverseLink());
            }

            yield return null;
        }

        agent.isStopped = true;
        yield return new WaitForSeconds(2.0f);

        ChangeState(UnitState.Patrol);
    }

    // 3. 추적 상태
    private IEnumerator IE_ChaseState()
    {
        agent.speed = unitData.runSpeed; // 추적 속도로 변경
        agent.isStopped = false;

        float lostTimer = 0f;          // 놓친 시간 카운트
        float maxLostTime = 2.0f;       // 2초 동안 놓치면 추적 포기

        while (true)
        {
            if (playerTransform == null)
            {
                Debug.LogWarning("Player Transform이 Null입니다. Patrol 상태로 복귀합니다.");
                ChangeState(UnitState.Patrol);
                yield break;
            }

            // 1. 플레이어가 시야에 있는지 검사
            if (IsPlayerInSight())
            {
                // 보이면 타이머 리셋 & 플레이어 최신 위치로 목적지 갱신
                lostTimer = 0f;
                lastHeardPosition = playerTransform.position;

                if (Vector3.Distance(agent.destination, playerTransform.position) > 0.3f)
                {
                    agent.SetDestination(playerTransform.position);
                }
            }
            else
            {
                // 안 보이기 시작하면 타이머 가동
                lostTimer += Time.deltaTime;

                // 지정한 최대 놓침 시간을 초과하면 수색 상태로 전환
                if (lostTimer >= maxLostTime)
                {
                    investigatePosition = lastHeardPosition;
                    ChangeState(UnitState.Investigate);
                    yield break;
                }
            }

            // 2. 이동 중 계단/사다리(Link)를 만났다면 속도에 맞춰 자연스럽게 통과
            if (agent.isOnOffMeshLink)
            {
                yield return StartCoroutine(IE_CheckAndTraverseLink());
            }

            // 3. 공격 범위 및 시야 조건 체크 (Null 체크가 이미 상단에서 완료되었으므로 안전!)
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            float heightDiff = Mathf.Abs(transform.position.y - playerTransform.position.y);

            if (distance <= 1.0f && heightDiff <= 0.8f && IsPlayerInSight())
            {
                ChangeState(UnitState.Attack);
                yield break;
            }

            yield return null; // 다음 프레임 대기
        }
    }

    // 4. 공격 상태
    private IEnumerator IE_AttackState()
    {
        agent.isStopped = true;
        Debug.Log("공격 시작!");

        // 애니메이션 재생 시간 대기 등의 처리를 코루틴으로 직관적으로 작성 가능
        yield return new WaitForSeconds(2.0f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Player.GetComponent<PlayerStatus>().TakeDamage(unitData.damage);
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnUnitDespawned();
        }

        Destroy(gameObject);
    }
    #endregion

    private void MoveToNextPatrolPoint()
    {
        if (patrolPositions == null || patrolPositions.Length == 0) return;

        agent.SetDestination(patrolPositions[currentPatrolIndex]);

        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPositions.Length;
    }

    private void CheckSensorySystem()
    {
        if (playerTransform == null || currentState == UnitState.Attack) return;

        if (IsPlayerInSight()) ChangeState(UnitState.Chase);
    }

    private bool IsPlayerInSight()
    {
        if (playerTransform == null) return false;

        // 1. 거리 체크
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > unitData.sightRange) return false;

        // 2. 시야각(FOV) 체크
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToPlayer) > unitData.fovAngle / 2f) return false;

        // 3. 눈높이 시선 레이캐스트 (장애물 여부)
        // ※ transform.position 대신 눈높이(Eye Position) 지점을 사용하는 것이 중요합니다.
        Vector3 eyePos = transform.position + Vector3.up * 1.5f; // 유닛 눈높이
        Vector3 targetEyePos = playerTransform.position + Vector3.up * 1.5f; // 플레이어 눈높이

        if (Physics.Linecast(eyePos, targetEyePos, LayerMask.GetMask("Obstacle", "Ground")))
        {
            return false; // 시야 차단 장애물에 가려짐
        }

        return true; // 감지 성공!
    }

    private IEnumerator IE_CheckAndTraverseLink()
    {
        if (!agent.isOnOffMeshLink) yield break;

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;

        // 실제 계단 거리 및 이동 시간 계산 (현재 설정된 agent.speed 기반)
        float distance = Vector3.Distance(startPos, endPos);
        float duration = (agent.speed > 0f) ? (distance / agent.speed) : 1f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, timer / duration);
            yield return null;
        }

        // 이동 완료 후 위치 고정 및 Link 완료 신호
        transform.position = endPos;
        agent.Warp(endPos);
        agent.CompleteOffMeshLink();
    }
}