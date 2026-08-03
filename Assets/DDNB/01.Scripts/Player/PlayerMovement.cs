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

    private bool isGrounded;
    private RaycastHit groundHit;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // 경사/계단에서 물리 마찰에 의존하지 않고 속도를 직접 제어
        rb.linearDamping = 0f;

        col = GetComponent<CapsuleCollider>();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.Player == null) return;

        // 숨은 상태: 이동·중력 스냅 중단
        if (GameManager.Instance.Player.CurrentState == PlayerState.Hidden)
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

        // 지면에 붙어 있으면 이동 방향을 경사면에 투영
        if (isGrounded)
        {
            moveDir = Vector3.ProjectOnPlane(moveDir, groundHit.normal).normalized;
            Vector3 slopeVelocity = moveDir * currentSpeed;
            rb.linearVelocity = slopeVelocity;
        }
        else
        {
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, v.y, moveDir.z * currentSpeed);
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
