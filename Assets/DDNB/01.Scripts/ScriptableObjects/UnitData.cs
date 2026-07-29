using UnityEngine;

[CreateAssetMenu(fileName = "NewGhostData", menuName = "Unit/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Visuals")]
    public GameObject unitModelPrefab; // 모델링 프리팹

    [Header("Stats")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 4.5f;

    [Header("Senses")]
    public float sightRange = 5.0f;     // 시야 범위
    public float hearingRange = 10.0f;   // 청각 예민도 (소리 감지 거리)
    public float fovAngle = 90.0f;       // 시야각

    [Header("Abilities")]
    public int damage = 10;
}
