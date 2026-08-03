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

    private bool isOccupied;

    /// <summary>현재 이 은신처에 플레이어가 숨어 있는지</summary>
    public bool IsOccupied => isOccupied;

    /// <summary>빠져나올 위치 (AI Investigate 목표로도 사용)</summary>
    public Vector3 ExitPosition => exitPoint != null ? exitPoint.position : transform.position;

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

        isOccupied = true;
        player.SetActiveHideSpot(this);
        player.ChangePlayerState(PlayerState.Hidden);

        // 순간이동 + 은신처 방향 맞춤
        player.transform.SetPositionAndRotation(hidePoint.position, hidePoint.rotation);

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        // Chase 중이면 Investigate(위치 B)로 전환 → 이후 Patrol로 자연스럽게 복귀
        if (StageManager.Instance != null && StageManager.Instance.CurrentUnitAI != null)
            StageManager.Instance.CurrentUnitAI.OnPlayerHidden(ExitPosition);
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

        isOccupied = false;
        player.SetActiveHideSpot(null);

        player.transform.SetPositionAndRotation(exitPoint.position, exitPoint.rotation);

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        player.ChangePlayerState(PlayerState.Idle);
    }
}
