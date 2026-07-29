using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;

    [Header("Speed")]
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float interactSpeed = 0f;
    [SerializeField] private float exhaustionSpeed = 3f;

    [Header("Ground Check & Step Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayDistance = 1.2f;   // 플레이어 중심에서 바닥까지의 레이 길이
    [SerializeField] private float snapForce = 15f;      // 계단을 내려갈 때 눌러주는 힘
    [SerializeField] private float maxStepHeight = 0.4f; // 허용할 최대 계단 높이

    private bool isGrounded;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        col = GetComponent<Collider>();
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.Player != null)
        {
            Move();
            CheckGroundAndSnap();
        }
    }

    void Move()
    {
        Vector2 moveInput = InputManager.Instance.MoveValue;

        float currentSpeed = CurrentSpeed();

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, rb.linearVelocity.y, moveDir.z * currentSpeed);
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
            default:
                return walkSpeed;
        }
    }

    private void CheckGroundAndSnap()
    {
        // 1. 발밑으로 레이캐스트 발사
        Ray ray = new Ray(transform.position, Vector3.down);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundLayer);

        if (hasHit)
        {
            isGrounded = true;

            // 계단이나 경사로를 내려가거나 올라갈 때 바닥에 착 붙게 함
            // 바닥과 일정한 거리를 유지하도록 아래쪽으로 수직 힘을 가해줌
            float distanceToGround = hit.distance;

            // 공중에 약간 떠 있는 상태(계단 내려가는 도중)라면 강제로 아래로 눌러줌
            if (distanceToGround > (col.bounds.extents.y + 0.05f))
            {
                // 점프 중이 아닐 때만 작동
                rb.AddForce(Vector3.down * snapForce, ForceMode.Acceleration);
            }
        }
        else
        {
            isGrounded = false;
        }
    }
}