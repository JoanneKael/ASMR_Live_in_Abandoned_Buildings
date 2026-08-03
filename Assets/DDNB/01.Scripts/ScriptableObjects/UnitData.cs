using UnityEngine;

[CreateAssetMenu(fileName = "NewGhostData", menuName = "Unit/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Visuals")]
    public GameObject unitModelPrefab; // 모델링 프리팹

    [Header("Stats")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4f;

    [Header("Senses")]
    public float sightRange = 5.0f;     // 시야 범위 (수평)
    public float hearingRange = 10.0f;   // 청각 예민도 (소리 감지 거리)
    public float fovAngle = 90.0f;       // 시야각
    public float maxSightHeightDiff = 2.0f; // 시야 허용 높이 차 (다른 층 제외)

    [Header("Abilities")]
    public int damage = 20;
}
