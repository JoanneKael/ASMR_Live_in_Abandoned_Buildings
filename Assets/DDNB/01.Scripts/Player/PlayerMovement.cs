using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private CapsuleCollider col;

    [Header("Speed")]
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float interactSpeed = 0f;
    [SerializeField] private float exhaustionSpeed = 3f;

    [Header("Ground Stick")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundProbeExtra = 0.35f; // 발밑에서 추가로 탐색할 거리
    [SerializeField] private float groundSnapOffset = 0.02f; // 바닥에서 살짝 띄워 끼임 방지
    [SerializeField] private float slopeSlideLimit = 50f;    // 이 각도 이상이면 미끄러짐 허용(낙하)

    [Header("Collider")]
    [SerializeField] private float colliderHeight = 1.4f;
    [SerializeField] private float colliderRadius = 0.4f;
    [SerializeField] private Vector3 colliderCenter = new Vector3(0f, 0.5f, 0f);

    [Header("Wall Slide")]
    [SerializeField] private LayerMask obstacleMask = ~0; // 벽/가구 등 슬라이딩 대상
    [SerializeField] private float wallSkin = 0.05f;       // 접촉 감지용 여유 두께
    [SerializeField] private int maxSlideIterations = 2;  // 코너 대응 반복 횟수

    private bool isGrounded;
    private RaycastHit groundHit;
    private readonly Collider[] overlapBuffer = new Collider[16];

    private void Awake()
    {
        col = GetComponent<CapsuleCollider>();
        ApplyColliderSettings();
        ApplyNoFrictionMaterial();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // 경사/계단에서 물리 마찰에 의존하지 않고 속도를 직접 제어
        rb.linearDamping = 0f;

        ApplyColliderSettings();
    }

    private void ApplyColliderSettings()
    {
        if (col == null) return;

        col.height = colliderHeight;
        col.radius = colliderRadius;
        col.center = colliderCenter;
        col.direction = 1;
    }

    // 벽/소파에 붙었을 때 마찰로 수평 속도가 죽지 않도록 마찰 0 적용
    private void ApplyNoFrictionMaterial()
    {
        if (col == null) return;

        var mat = new PhysicsMaterial("PlayerNoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        col.material = mat;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.Player == null) return;

        // 숨은 상태 / 숨기·나오기 연출 / 기절: 이동·중력 스냅 중단
        if (GameManager.Instance.Player.CurrentState == PlayerState.Hidden
            || GameManager.Instance.Player.CurrentState == PlayerState.Stunned
            || GameManager.Instance.Player.IsHideTransitioning
            || (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked))
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            return;
        }

        ProbeGround();
        Move();
        StickToGround();
    }

    private void Move()
    {
        Vector2 moveInput = InputManager.Instance.MoveValue;
        float currentSpeed = CurrentSpeed();

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * moveInput.y + camRight * moveInput.x);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        // 입력 없으면 수평 속도 0 → 계단/경사에서 미끄러짐 방지
        if (moveDir.sqrMagnitude < 0.0001f)
        {
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, isGrounded ? 0f : v.y, 0f);
            return;
        }

        Vector3 desiredVelocity;

        // 지면에 붙어 있으면 이동 방향을 경사면에 투영
        if (isGrounded)
        {
            moveDir = Vector3.ProjectOnPlane(moveDir, groundHit.normal).normalized;
            desiredVelocity = moveDir * currentSpeed;
        }
        else
        {
            Vector3 v = rb.linearVelocity;
            desiredVelocity = new Vector3(moveDir.x * currentSpeed, v.y, moveDir.z * currentSpeed);
        }

        // 벽/가구 법선 성분 제거 → 옆으로 미끄러지며 이동
        desiredVelocity = SlideAlongSurfaces(desiredVelocity);
        rb.linearVelocity = desiredVelocity;
    }

    // 충돌면에 파고드는 속도 성분을 제거하고 접선 방향으로 투영
    private Vector3 SlideAlongSurfaces(Vector3 velocity)
    {
        Vector3 result = velocity;

        for (int i = 0; i < maxSlideIterations; i++)
        {
            Vector3 horizontal = new Vector3(result.x, 0f, result.z);
            if (horizontal.sqrMagnitude < 0.0001f) break;

            float speed = horizontal.magnitude;
            if (!TryGetBlockingNormal(horizontal.normalized, speed, out Vector3 wallNormal))
                break;

            // 벽으로 파고드는 성분만 제거
            if (Vector3.Dot(result, wallNormal) < 0f)
                result = Vector3.ProjectOnPlane(result, wallNormal);
            else
                break;
        }

        // 공중일 때는 기존 낙하 속도 유지
        if (!isGrounded)
            result.y = velocity.y;

        return result;
    }

    // 이동 방향에 있는 벽 법선 탐색 (이미 붙어 있는 경우 + 앞으로 막히는 경우)
    private bool TryGetBlockingNormal(Vector3 moveDir, float speed, out Vector3 wallNormal)
    {
        wallNormal = Vector3.zero;
        GetCapsuleWorldPoints(wallSkin * 0.5f, out Vector3 p1, out Vector3 p2, out float radius);

        // 1) 이미 접촉/근접한 장애물 (벽에 비벼지는 중)
        float overlapRadius = radius + wallSkin;
        int count = Physics.OverlapCapsuleNonAlloc(
            p1, p2, overlapRadius, overlapBuffer, obstacleMask, QueryTriggerInteraction.Ignore);

        float bestDot = 0f; // moveDir와 -normal의 정렬도 (클수록 정면 벽)
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            Collider other = overlapBuffer[i];
            if (other == null || other == col || other.attachedRigidbody == rb) continue;

            if (!TryGetContactNormal(other, out Vector3 normal)) continue;
            if (!IsWallNormal(normal)) continue;

            // 이동 방향과 마주보는 면만 처리
            float intoWall = Vector3.Dot(moveDir, -normal);
            if (intoWall <= 0.01f) continue;

            if (!found || intoWall > bestDot)
            {
                bestDot = intoWall;
                wallNormal = normal;
                found = true;
            }
        }

        if (found) return true;

        // 2) 아직 안 닿았지만 이번 스텝에서 막히는 경우
        float castDistance = Mathf.Max(wallSkin, speed * Time.fixedDeltaTime) + wallSkin;
        if (Physics.CapsuleCast(
                p1, p2, radius, moveDir, out RaycastHit hit, castDistance,
                obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != col && hit.collider.attachedRigidbody != rb
                && IsWallNormal(hit.normal) && Vector3.Dot(moveDir, hit.normal) < 0f)
            {
                wallNormal = hit.normal;
                return true;
            }
        }

        return false;
    }

    private bool TryGetContactNormal(Collider other, out Vector3 normal)
    {
        // 침투 중이면 ComputePenetration 분리 방향이 곧 벽 법선
        if (Physics.ComputePenetration(
                col, transform.position, transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 direction, out float distance))
        {
            if (distance > 0f && direction.sqrMagnitude > 0.0001f)
            {
                normal = direction.normalized;
                return true;
            }
        }

        // 접촉만 한 경우 ClosestPoint로 대략적 법선 추정
        Vector3 capsuleCenter = col.bounds.center;
        Vector3 onOther = other.ClosestPoint(capsuleCenter);
        Vector3 onSelf = col.ClosestPoint(onOther);
        Vector3 delta = onSelf - onOther;

        if (delta.sqrMagnitude < 0.000001f)
        {
            // 거의 같은 점이면 중심 기준으로 폴백
            delta = capsuleCenter - onOther;
            if (delta.sqrMagnitude < 0.000001f)
            {
                normal = Vector3.zero;
                return false;
            }
        }

        normal = delta.normalized;
        return true;
    }

    private bool IsWallNormal(Vector3 normal)
    {
        // 바닥/천장에 가까운 면은 벽 슬라이딩에서 제외
        float angle = Vector3.Angle(normal, Vector3.up);
        return angle > slopeSlideLimit && angle < 180f - slopeSlideLimit;
    }

    private void GetCapsuleWorldPoints(float shrink, out Vector3 p1, out Vector3 p2, out float radius)
    {
        radius = Mathf.Max(0.01f, col.radius - shrink);

        float height = Mathf.Max(col.height, col.radius * 2f);
        float half = height * 0.5f - col.radius;
        Vector3 center = transform.TransformPoint(col.center);
        Vector3 up = transform.up;

        p1 = center + up * half;
        p2 = center - up * half;

        // shrink로 높이가 줄어든 만큼 포인트도 안쪽으로
        if (shrink > 0f && half > shrink)
        {
            p1 -= up * shrink;
            p2 += up * shrink;
        }
    }

    private void ProbeGround()
    {
        float radius = Mathf.Max(0.05f, col.radius * 0.9f);
        Vector3 origin = col.bounds.center;
        float distance = col.bounds.extents.y + groundProbeExtra;

        if (Physics.SphereCast(origin, radius, Vector3.down, out groundHit, distance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(groundHit.normal, Vector3.up);
            isGrounded = angle <= slopeSlideLimit;
        }
        else
        {
            isGrounded = false;
        }

        // 지면에 붙어 있을 때는 중력 끄고 직접 스냅 (계단 미끄러짐/부상 완화)
        rb.useGravity = !isGrounded;
    }

    private void StickToGround()
    {
        if (!isGrounded) return;

        // 발(bounds 하단)이 바닥에 닿도록 Y 스냅
        float bottomOffset = transform.position.y - col.bounds.min.y;
        Vector3 pos = rb.position;
        pos.y = groundHit.point.y + bottomOffset + groundSnapOffset;
        rb.MovePosition(pos);

        Vector3 vel = rb.linearVelocity;
        if (vel.y > 0f) vel.y = 0f;
        rb.linearVelocity = vel;
    }

    private float CurrentSpeed()
    {
        switch (GameManager.Instance.Player.CurrentState)
        {
            case PlayerState.Walking:
                return walkSpeed;
            case PlayerState.Running:
                return runSpeed;
            case PlayerState.Crouching:
                return crouchSpeed;
            case PlayerState.Interacting:
                return interactSpeed;
            case PlayerState.Exhaustion:
                return exhaustionSpeed;
            case PlayerState.ASMR:
                return interactSpeed;
            case PlayerState.Hidden:
                return 0f;
            default:
                return walkSpeed;
        }
    }
}
