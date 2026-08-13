using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Data Asset")]
    [SerializeField] private StageData stageData;

    [Header("Spawn Anchors")]
    [SerializeField] private Transform[] spawnAnchors;

    [Header("Patrol Anchors")]
    [SerializeField] private Transform[] patrolAnchors;

    private Vector3[] spawnPositions;
    // patrolAnchors 직접 전달 (위치 + forward 필요)

    private UnitAI currentUnitAI;
    private Coroutine spawnRoutine;

    public UnitAI CurrentUnitAI => currentUnitAI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitAnchorPositions();
    }

    private void Start()
    {
        spawnRoutine = StartCoroutine(IE_SpawnAfterDelay(10f));
    }

    private void InitAnchorPositions()
    {
        spawnPositions = ToPositionArray(spawnAnchors);
    }

    private static Vector3[] ToPositionArray(Transform[] anchors)
    {
        if (anchors == null || anchors.Length == 0) return null;

        Vector3[] positions = new Vector3[anchors.Length];
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] != null)
                positions[i] = anchors[i].position;
        }
        return positions;
    }

    #region UnitSpawn

    private IEnumerator IE_SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        spawnRoutine = null;
        SpawnUnit();
    }

    private void SpawnUnit()
    {
        // 이미 살아 있으면 중복 스폰 방지
        if (currentUnitAI != null) return;

        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            Debug.LogError("[StageManager] 스폰 포인트가 설정되지 않았습니다!");
            return;
        }

        if (patrolAnchors == null || patrolAnchors.Length == 0)
        {
            Debug.LogError("[StageManager] 패트롤 포인트가 설정되지 않았습니다!");
            return;
        }

        if (stageData == null || stageData.unitPrefab == null)
        {
            Debug.LogError("[StageManager] StageData 또는 unitPrefab이 없습니다!");
            return;
        }

        int randomIndex = Random.Range(0, spawnPositions.Length);
        Vector3 spawnPos = spawnPositions[randomIndex];

        GameObject unitObj = Instantiate(stageData.unitPrefab, spawnPos, Quaternion.identity);
        currentUnitAI = unitObj.GetComponent<UnitAI>();

        if (currentUnitAI != null)
            currentUnitAI.InitUnit(stageData.unitData, patrolAnchors);
    }

    /// <summary>공격 후 자폭 등 — 레퍼런스 해제 후 리스폰 예약</summary>
    public void OnUnitDespawned()
    {
        currentUnitAI = null;
        ScheduleRespawn(15f);
    }

    /// <summary>넉아웃/하루 종료 — 즉시 제거, 리스폰은 호출측에서 다음날 예약</summary>
    public void DespawnForDayEnd()
    {
        CancelScheduledSpawn();

        if (currentUnitAI != null)
        {
            Destroy(currentUnitAI.gameObject);
            currentUnitAI = null;
        }
    }

    /// <summary>다음날 시작 등 — 딜레이 후 스폰</summary>
    public void ScheduleRespawn(float delaySeconds)
    {
        CancelScheduledSpawn();
        spawnRoutine = StartCoroutine(IE_SpawnAfterDelay(delaySeconds));
    }

    private void CancelScheduledSpawn()
    {
        if (spawnRoutine == null) return;
        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    /// <summary>소음 발생 위치를 현재 유닛에게 전달</summary>
    public void NotifyUnitHeardNoise(Vector3 worldPosition)
    {
        if (currentUnitAI == null) return;
        currentUnitAI.HearNoise(worldPosition);
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (spawnAnchors != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < spawnAnchors.Length; i++)
            {
                if (spawnAnchors[i] != null)
                    Gizmos.DrawWireSphere(spawnAnchors[i].position, 1f);
            }
        }

        if (patrolAnchors != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < patrolAnchors.Length; i++)
            {
                if (patrolAnchors[i] == null) continue;
                Gizmos.DrawWireSphere(patrolAnchors[i].position, 0.6f);
                if (i + 1 < patrolAnchors.Length && patrolAnchors[i + 1] != null)
                    Gizmos.DrawLine(patrolAnchors[i].position, patrolAnchors[i + 1].position);
            }
        }
    }
}
