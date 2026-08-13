using System.Collections;
using UnityEngine;

/// <summary>
/// 숨기 가능 상호작용 오브젝트.
/// 위치A(hidePoint)로 숨고, E로 위치B(exitPoint)에서 나온다.
/// </summary>
public class InteractableHide : InteractableObject
{
    [Header("Hide Points")]
    [SerializeField] private Transform hidePoint; // 위치 A — 숨을 때
    [SerializeField] private Transform exitPoint; // 위치 B — 나올 때 / AI 수색 목표

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 1f;

    private bool isOccupied;
    private bool isTransitioning;
    private Coroutine transitionRoutine;

    /// <summary>현재 이 은신처에 플레이어가 숨어 있는지</summary>
    public bool IsOccupied => isOccupied;

    /// <summary>숨기/나오기 연출 중인지</summary>
    public bool IsTransitioning => isTransitioning;

    /// <summary>빠져나올 위치 (AI Investigate 목표로도 사용)</summary>
    public Vector3 ExitPosition => exitPoint != null ? exitPoint.position : transform.position;

    /// <summary>AI가 수색 시 바라볼 지점 (숨는 안쪽)</summary>
    public Vector3 LookAtPosition => hidePoint != null ? hidePoint.position : transform.position;

    /// <summary>소리 위치와 은신처(루트/hide/exit)의 최소 수평 거리</summary>
    public float GetFlatDistanceTo(Vector3 worldPos)
    {
        float best = FlatDistance(transform.position, worldPos);
        if (hidePoint != null)
            best = Mathf.Min(best, FlatDistance(hidePoint.position, worldPos));
        if (exitPoint != null)
            best = Mathf.Min(best, FlatDistance(exitPoint.position, worldPos));
        return best;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    /// <summary>소리 위치 반경 내 가장 가까운 InteractableHide (없으면 null)</summary>
    public static InteractableHide FindNearestNearSound(Vector3 soundPos, float radius)
    {
        InteractableHide[] hides = Object.FindObjectsByType<InteractableHide>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        InteractableHide best = null;
        float bestDist = radius;

        for (int i = 0; i < hides.Length; i++)
        {
            InteractableHide hide = hides[i];
            if (hide == null) continue;

            float d = hide.GetFlatDistanceTo(soundPos);
            if (d <= bestDist)
            {
                bestDist = d;
                best = hide;
            }
        }

        return best;
    }

    private void Reset()
    {
        inputMode = InteractInputMode.Tap;
    }

    private void Awake()
    {
        inputMode = InteractInputMode.Tap;
    }

    public override void Interact()
    {
        // 이동 연출 중에는 중복 입력 무시
        if (isTransitioning) return;

        if (isOccupied) ExitHide();
        else EnterHide();
    }

    private void EnterHide()
    {
        if (hidePoint == null)
        {
            Debug.LogError("[InteractableHide] hidePoint(위치 A)가 비어 있습니다.");
            return;
        }

        Player player = GameManager.Instance.Player;
        if (player == null) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) return;

        isOccupied = true;
        player.SetActiveHideSpot(this);
        player.ChangePlayerState(PlayerState.Hidden);
        player.SetHideTransitioning(true);

        // 은신처 콜라이더에 막히지 않도록 충돌 비활성 후 보간 이동
        PrepareRigidbodyForTransition(rb);

        // Chase 중이면 Investigate(위치 B)로 전환 (연출 시작 시점에 즉시)
        if (StageManager.Instance != null && StageManager.Instance.CurrentUnitAI != null)
            StageManager.Instance.CurrentUnitAI.OnPlayerHidden(ExitPosition);

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionRoutine(
            rb,
            hidePoint.position,
            GetYawRotation(hidePoint.rotation),
            onComplete: () =>
            {
                player.SetHideTransitioning(false);
            }));
    }

    private void ExitHide()
    {
        if (exitPoint == null)
        {
            Debug.LogError("[InteractableHide] exitPoint(위치 B)가 비어 있습니다.");
            return;
        }

        Player player = GameManager.Instance.Player;
        if (player == null) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) return;

        player.SetHideTransitioning(true);
        PrepareRigidbodyForTransition(rb);

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionRoutine(
            rb,
            exitPoint.position,
            targetRotation: null, // 나오기는 위치만 이동
            onComplete: () =>
            {
                isOccupied = false;
                player.SetActiveHideSpot(null);

                // 밖으로 도착한 뒤 충돌 복구
                RestoreRigidbodyAfterExit(rb);

                player.ChangePlayerState(PlayerState.Idle);
                player.SetHideTransitioning(false);
            }));
    }

    private IEnumerator TransitionRoutine(
        Rigidbody rb,
        Vector3 targetPosition,
        Quaternion? targetRotation,
        System.Action onComplete)
    {
        isTransitioning = true;

        Vector3 startPos = rb.position;
        Quaternion startRot = rb.rotation;
        float duration = Mathf.Max(0.01f, transitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            rb.position = Vector3.Lerp(startPos, targetPosition, t);
            if (targetRotation.HasValue)
                rb.rotation = Quaternion.Slerp(startRot, targetRotation.Value, t);

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            yield return null;
        }

        rb.position = targetPosition;
        if (targetRotation.HasValue)
            rb.rotation = targetRotation.Value;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        isTransitioning = false;
        transitionRoutine = null;
        onComplete?.Invoke();
    }

    private static void PrepareRigidbodyForTransition(Rigidbody rb)
    {
        rb.detectCollisions = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private static void RestoreRigidbodyAfterExit(Rigidbody rb)
    {
        rb.detectCollisions = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // 캡슐이 기울어지지 않도록 Yaw만 맞춤
    private static Quaternion GetYawRotation(Quaternion rotation)
    {
        Vector3 euler = rotation.eulerAngles;
        return Quaternion.Euler(0f, euler.y, 0f);
    }

    private void OnDisable()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }
        isTransitioning = false;
    }

    /// <summary>
    /// 넉아웃/리스폰 시 은신 연출을 즉시 끊고 점유·충돌을 복구합니다.
    /// (exitPoint로 이동하지 않음)
    /// </summary>
    public void ForceReleaseForRespawn()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        isTransitioning = false;
        isOccupied = false;

        Player player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player == null) return;

        player.SetHideTransitioning(false);
        player.SetActiveHideSpot(null);

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
            RestoreRigidbodyAfterExit(rb);
    }
}
