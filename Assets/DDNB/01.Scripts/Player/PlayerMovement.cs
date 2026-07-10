using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ref")]
    private Player player;

    [Header("Speed")]
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float interactSpeed = 0f;
    [SerializeField] private float exhaustionSpeed = 3f;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    void FixedUpdate()
    {
        Move();
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

        player.rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, player.rb.linearVelocity.y, moveDir.z * currentSpeed);
    }

    private float CurrentSpeed()
    {
        switch (player.CurrentState)
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
            default:
                return walkSpeed;
        }
    }
}