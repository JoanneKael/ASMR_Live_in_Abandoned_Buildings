using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Player")]
    public PlayerState CurrentState { get; private set; }
    [SerializeField] private PlayerState lastState = PlayerState.None;
    private PlayerStatus status;
    private PlayerInteraction interaction;

    /// <summary>현재 숨어 있는 은신처 (Hidden일 때만)</summary>
    public InteractableHide ActiveHideSpot { get; private set; }

    [Header("CameraRig")]
    private Transform cameraRig;

    void Awake()
    {
        status = GetComponent<PlayerStatus>();
        interaction = GetComponent<PlayerInteraction>();
        cameraRig = transform.GetChild(0);
    }

    private void Update()
    {
        UpdatePlayerState();

        if (CurrentState != lastState)
        {
            UpdateCameraRig();
            lastState = CurrentState;
        }
    }

    private void UpdatePlayerState()
    {
        if (CurrentState == PlayerState.ASMR
            || CurrentState == PlayerState.Lobby
            || CurrentState == PlayerState.Hidden)
            return;

        if (status.IsExhauseted) CurrentState = PlayerState.Exhaustion;
        else if (interaction != null && interaction.IsHoldingInteraction) CurrentState = PlayerState.Interacting;
        else if (InputManager.Instance.IsCrouching) CurrentState = PlayerState.Crouching;
        else if (InputManager.Instance.IsRunning) CurrentState = PlayerState.Running;
        else if (InputManager.Instance.MoveValue.magnitude > 0f) CurrentState = PlayerState.Walking;
        else CurrentState = PlayerState.Idle;
    }

    private void UpdateCameraRig()
    {
        if (CurrentState == PlayerState.Crouching)
        {
            cameraRig.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        }
        else
        {
            cameraRig.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        }
    }

    public void ChangePlayerState(PlayerState state)
    {
        CurrentState = state;
    }

    public void SetActiveHideSpot(InteractableHide hideSpot)
    {
        ActiveHideSpot = hideSpot;
    }
}
