using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Data Asset")]
    [SerializeField] private StageData stageData;

    [Header("Scene Patrol Anchors")]
    [SerializeField] private Transform[] patrolAnchors;

    private Vector3[] patrolPositions;
    private UnitAI currentUnitAI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 씬 시작 시 Transform 위치들을 Vector3 배열로 변환
        InitPatrolPositions();
    }

    private void Start()
    {

        StartCoroutine(IE_GhostSpawnLoop());
    }

    private void InitPatrolPositions()
    {
        if (patrolAnchors != null && patrolAnchors.Length > 0)
        {
            patrolPositions = new Vector3[patrolAnchors.Length];
            for (int i = 0; i < patrolAnchors.Length; i++)
            {
                patrolPositions[i] = patrolAnchors[i].position;
            }
        }
    }

    #region UnitSpawn
    private IEnumerator IE_GhostSpawnLoop()
    {
        // 1. 인게임 진입 후 첫 스폰까지 30초 대기
        yield return new WaitForSeconds(10);

        SpawnUnit();
    }

    private void SpawnUnit()
    {
        if (patrolPositions == null || patrolPositions.Length == 0)
        {
            Debug.LogError("[GameManager] 패트롤 포인트가 설정되지 않았습니다!");
            return;
        }

        // 1. 랜덤 패트롤 포인트 인덱스 뽑기
        int randomIndex = Random.Range(0, patrolPositions.Length);
        Vector3 spawnPos = patrolPositions[randomIndex];

        // 2. 해당 패트롤 포인트 위치에 유닛UI 생성
        GameObject unitObj = Instantiate(stageData.unitPrefab, spawnPos, Quaternion.identity);
        currentUnitAI = unitObj.GetComponent<UnitAI>();

        // 3. GhostAI 컴포넌트에 정보 전달하여 초기화 (뽑힌 패트롤 지점부터 순회 시작)
        if (currentUnitAI != null)
        {
            currentUnitAI.InitUnit(stageData.unitData, patrolPositions, randomIndex);
        }
    }

    public void OnUnitDespawned()
    {
        StartCoroutine(IE_RespawnTimer());
    }

    private IEnumerator IE_RespawnTimer()
    {
        yield return new WaitForSeconds(15);
        SpawnUnit();
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (patrolAnchors == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolAnchors.Length; i++)
        {
            if (patrolAnchors[i] != null)
            {
                Gizmos.DrawWireSphere(patrolAnchors[i].position, 1f);
            }
        }
    }
}