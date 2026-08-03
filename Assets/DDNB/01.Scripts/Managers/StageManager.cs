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
    private Vector3[] patrolPositions;

    private UnitAI currentUnitAI;

    public UnitAI CurrentUnitAI => currentUnitAI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitAnchorPositions();
    }

    private void Start()
    {
        StartCoroutine(IE_GhostSpawnLoop());
    }

    private void InitAnchorPositions()
    {
        spawnPositions = ToPositionArray(spawnAnchors);
        patrolPositions = ToPositionArray(patrolAnchors);
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
    private IEnumerator IE_GhostSpawnLoop()
    {
        yield return new WaitForSeconds(10);
        SpawnUnit();
    }

    private void SpawnUnit()
    {
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            Debug.LogError("[StageManager] 스폰 포인트가 설정되지 않았습니다!");
            return;
        }

        if (patrolPositions == null || patrolPositions.Length == 0)
        {
            Debug.LogError("[StageManager] 패트롤 포인트가 설정되지 않았습니다!");
            return;
        }

        int randomIndex = Random.Range(0, spawnPositions.Length);
        Vector3 spawnPos = spawnPositions[randomIndex];

        GameObject unitObj = Instantiate(stageData.unitPrefab, spawnPos, Quaternion.identity);
        currentUnitAI = unitObj.GetComponent<UnitAI>();

        if (currentUnitAI != null)
            currentUnitAI.InitUnit(stageData.unitData, patrolPositions);
    }

    public void OnUnitDespawned()
    {
        currentUnitAI = null;
        StartCoroutine(IE_RespawnTimer());
    }

    /// <summary>소음 발생 위치를 현재 유닛에게 전달</summary>
    public void NotifyUnitHeardNoise(Vector3 worldPosition)
    {
        if (currentUnitAI == null) return;
        currentUnitAI.HearNoise(worldPosition);
    }

    private IEnumerator IE_RespawnTimer()
    {
        yield return new WaitForSeconds(15);
        SpawnUnit();
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
