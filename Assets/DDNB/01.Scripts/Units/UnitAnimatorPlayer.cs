using UnityEngine;

/// <summary>
/// 유닛 모델 Animator 제어.
/// 공통 파라미터(Speed / Attack / IsScanning)로 재생하고,
/// 클립 차이는 UnitData.animatorOverride(또는 모델 프리팹 자체 Controller)로 해결합니다.
/// </summary>
[DisallowMultipleComponent]
public class UnitAnimatorPlayer : MonoBehaviour
{
    // Animator Controller에 동일 이름으로 파라미터를 만들어야 합니다.
    public static readonly int SpeedHash = Animator.StringToHash("Speed");
    public static readonly int AttackHash = Animator.StringToHash("Attack");
    public static readonly int IsScanningHash = Animator.StringToHash("IsScanning");

    [Header("Refs")]
    [SerializeField] private Animator animator;

    [Header("Locomotion")]
    [Tooltip("이 속도 미만이면 Idle(Speed=0)")]
    [SerializeField] private float idleSpeedThreshold = 0.05f;
    [Tooltip("Walk로 취급할 최대 수평 속도 (그 이상은 Run 쪽으로 보간)")]
    [SerializeField] private float walkReferenceSpeed = 1.5f;
    [Tooltip("Run으로 취급할 기준 수평 속도 (Speed=1)")]
    [SerializeField] private float runReferenceSpeed = 4f;

    [Header("Smoothing")]
    [SerializeField] private float speedDampTime = 0.12f;

    private float _speedParam;
    private bool _bound;
    private bool _hasSpeed;
    private bool _hasAttack;
    private bool _hasScanning;

    public Animator Animator => animator;
    public bool IsBound => _bound && animator != null;

    /// <summary>
    /// 스폰된 모델에서 Animator를 찾고, UnitData Override가 있으면 적용합니다.
    /// </summary>
    public void BindFromModel(GameObject modelInstance, AnimatorOverrideController overrideController)
    {
        if (modelInstance == null)
        {
            Debug.LogWarning("[UnitAnimatorPlayer] 모델 인스턴스가 없습니다.");
            _bound = false;
            return;
        }

        animator = modelInstance.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Debug.LogWarning($"[UnitAnimatorPlayer] '{modelInstance.name}'에 Animator가 없습니다. 모델 프리팹에 Animator를 추가하세요.");
            _bound = false;
            return;
        }

        if (overrideController != null)
            animator.runtimeAnimatorController = overrideController;

        CacheParameterFlags();
        _speedParam = 0f;
        _bound = true;

        SetScanning(false);
        SetSpeedImmediate(0f);
    }

    /// <summary>NavMeshAgent 수평 속도로 이동 애니 갱신 (Idle/Walk/Run 블렌드)</summary>
    public void UpdateLocomotion(float horizontalSpeed, float walkSpeed, float runSpeed)
    {
        if (!IsBound || !_hasSpeed) return;

        float walkRef = walkSpeed > 0.01f ? walkSpeed : walkReferenceSpeed;
        float runRef = runSpeed > 0.01f ? runSpeed : runReferenceSpeed;

        float target;
        if (horizontalSpeed <= idleSpeedThreshold)
            target = 0f;
        else if (horizontalSpeed <= walkRef)
            target = Mathf.InverseLerp(0f, walkRef, horizontalSpeed) * 0.5f; // 0~0.5 = Idle→Walk
        else
            target = 0.5f + Mathf.InverseLerp(walkRef, Mathf.Max(walkRef + 0.01f, runRef), horizontalSpeed) * 0.5f; // 0.5~1 = Walk→Run

        _speedParam = Mathf.Lerp(_speedParam, target, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, speedDampTime)));
        animator.SetFloat(SpeedHash, _speedParam);
    }

    public void SetSpeedImmediate(float speed01)
    {
        if (!IsBound || !_hasSpeed) return;
        _speedParam = Mathf.Clamp01(speed01);
        animator.SetFloat(SpeedHash, _speedParam);
    }

    /// <summary>공격 트리거 (Attack 상태 진입용)</summary>
    public void PlayAttack()
    {
        if (!IsBound || !_hasAttack) return;
        SetScanning(false);
        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);
    }

    /// <summary>제자리 두리번(수색) 루프 on/off</summary>
    public void SetScanning(bool scanning)
    {
        if (!IsBound || !_hasScanning) return;
        animator.SetBool(IsScanningHash, scanning);
    }

    /// <summary>수색 시작 — Speed를 끊고 Scan 재생</summary>
    public void PlayScan()
    {
        SetSpeedImmediate(0f);
        SetScanning(true);
    }

    public void StopScan()
    {
        SetScanning(false);
    }

    private void CacheParameterFlags()
    {
        _hasSpeed = HasParam(SpeedHash, AnimatorControllerParameterType.Float);
        _hasAttack = HasParam(AttackHash, AnimatorControllerParameterType.Trigger);
        _hasScanning = HasParam(IsScanningHash, AnimatorControllerParameterType.Bool);

        if (!_hasSpeed || !_hasAttack || !_hasScanning)
        {
            Debug.LogWarning(
                "[UnitAnimatorPlayer] Animator에 필요한 파라미터가 없습니다. " +
                "Tools/DDNB/Create Unit Base Animator Controller 로 베이스를 만들거나, " +
                "Speed(Float), Attack(Trigger), IsScanning(Bool)을 추가하세요.");
        }
    }

    private bool HasParam(int hash, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        var parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == hash && parameters[i].type == type)
                return true;
        }
        return false;
    }
}
