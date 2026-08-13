using UnityEngine;

[CreateAssetMenu(fileName = "NewGhostData", menuName = "Unit/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Visuals")]
    public GameObject unitModelPrefab; // 모델링 프리팹

    [Tooltip("모델별 클립 Override. null이면 모델 프리팹 Animator의 Controller를 그대로 사용")]
    public AnimatorOverrideController animatorOverride;

    [Header("Stats")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4f;

    [Header("Senses")]
    public float sightRange = 4.0f;     // 시야 범위 (수평)
    public float hearingRange = 10.0f;   // 청각 예민도 (소리 감지 거리)
    public float fovAngle = 90.0f;       // 시야각
    public float maxSightHeightDiff = 2.0f; // 시야 허용 높이 차 (다른 층 제외)
}
