using UnityEngine;

/// <summary>
/// 카메라 리그(머리) 위치가 벽·장애물에 파고들지 않도록 보정합니다.
/// </summary>
[DefaultExecutionOrder(50)]
public class PlayerCameraCollision : MonoBehaviour
{
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float headSphereRadius = 0.22f;
    [SerializeField] private float castRadius = 0.18f;
    [SerializeField] private float minLocalHeight = 0.55f;

    private Transform playerRoot;
    private CapsuleCollider bodyCollider;
    private Player player;

    private void Awake()
    {
        playerRoot = transform.parent;
        if (playerRoot != null)
        {
            bodyCollider = playerRoot.GetComponent<CapsuleCollider>();
            player = playerRoot.GetComponent<Player>();
        }

        if (collisionMask == 0)
            collisionMask = LayerMask.GetMask("Obstacle", "Default", "Stair");
    }

    private void LateUpdate()
    {
        if (playerRoot == null) return;

        // 기절/넉아웃 연출 중에는 HitPresenter가 카메라 위치를 제어
        if (player != null && player.CurrentState == PlayerState.Stunned)
            return;

        float desiredY = GetDesiredLocalY();
        Vector3 desiredWorld = playerRoot.TransformPoint(new Vector3(0f, desiredY, 0f));
        Vector3 resolved = ResolveHeadPosition(desiredWorld);
        transform.localPosition = playerRoot.InverseTransformPoint(resolved);
    }

    private float GetDesiredLocalY()
    {
        if (player != null && player.CurrentState == PlayerState.Crouching)
            return 0.6f;
        return 1.2f;
    }

    private Vector3 ResolveHeadPosition(Vector3 desiredWorld)
    {
        if (!Physics.CheckSphere(desiredWorld, headSphereRadius, collisionMask, QueryTriggerInteraction.Ignore))
            return desiredWorld;

        Vector3 castOrigin = bodyCollider != null
            ? bodyCollider.bounds.center
            : playerRoot.position + Vector3.up * 0.5f;

        Vector3 toHead = desiredWorld - castOrigin;
        float distance = toHead.magnitude;
        if (distance > 0.01f)
        {
            Vector3 dir = toHead / distance;
            if (Physics.SphereCast(castOrigin, castRadius, dir, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
                return hit.point - dir * headSphereRadius;
        }

        for (float y = desiredWorld.y - 0.05f; y >= playerRoot.position.y + minLocalHeight; y -= 0.05f)
        {
            Vector3 lowered = new Vector3(desiredWorld.x, y, desiredWorld.z);
            if (!Physics.CheckSphere(lowered, headSphereRadius, collisionMask, QueryTriggerInteraction.Ignore))
                return lowered;
        }

        return desiredWorld;
    }
}
