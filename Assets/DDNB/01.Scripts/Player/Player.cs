using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Player")]
    public PlayerState CurrentState { get; private set; }
    [SerializeField] private PlayerState lastState = PlayerState.None;
    private CapsuleCollider col;
    private PlayerStatus status;

    [Header("CameraRig")]
    private Transform cameraRig;

    void Awake()
    {
        col = GetComponent<CapsuleCollider>();
        status = GetComponent<PlayerStatus>();
        cameraRig = transform.GetChild(0);
    }

    private void Update()
    {
        UpdatePlayerState();

        if (CurrentState != lastState)
        {
            UpdateCameraRig();
            UpdateCollider();

            lastState = CurrentState;
        }
    }

    private void UpdatePlayerState()
    {
        if (CurrentState == PlayerState.ASMR || CurrentState == PlayerState.Lobby) return;

        if (status.IsExhauseted) CurrentState = PlayerState.Exhaustion;
        else if (InputManager.Instance.IsInteracting) CurrentState = PlayerState.Interacting;
        else if (InputManager.Instance.IsCrouching) CurrentState = PlayerState.Crouching;
        else if (InputManager.Instance.IsRunning) CurrentState = PlayerState.Running;
        else if (InputManager.Instance.MoveValue.magnitude > 0f) CurrentState = PlayerState.Walking;
        else CurrentState = PlayerState.Idle;
    }

    private void UpdateCameraRig()
    {
        if (CurrentState == PlayerState.Crouching)
        {
            cameraRig.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        }
        else
        {
            cameraRig.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        }
    }

    private void UpdateCollider()
    {
        if (CurrentState == PlayerState.Crouching)
        {
            col.height = 1.2f;
            col.center = new Vector3(0f, 0.6f, 0f);
        }
        else
        {
            col.height = 2.0f;
            col.center = new Vector3(0f, 1f, 0f);
        }
    }

    public void ChangePlayerState(PlayerState state)
    {
        CurrentState = state;
    }
}