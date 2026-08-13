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

    /// <summary>숨기/나오기 위치 보간 중 — 이동·시야 입력 무시</summary>
    public bool IsHideTransitioning { get; private set; }

    [Header("CameraRig")]
    private Transform cameraRig;

    void Awake()
    {
        status = GetComponent<PlayerStatus>();
        interaction = GetComponent<PlayerInteraction>();
        cameraRig = transform.GetChild(0);

        if (cameraRig != null && cameraRig.GetComponent<PlayerCameraCollision>() == null)
            cameraRig.gameObject.AddComponent<PlayerCameraCollision>();

        if (GetComponent<PlayerHitPresenter>() == null)
            gameObject.AddComponent<PlayerHitPresenter>();

        if (GetComponent<PlayerAudio>() == null)
            gameObject.AddComponent<PlayerAudio>();
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
            || CurrentState == PlayerState.Hidden
            || CurrentState == PlayerState.Stunned
            || IsHideTransitioning)
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
        // 기절/넉아웃 연출 중에는 HitPresenter가 카메라 높이를 제어
        if (CurrentState == PlayerState.Stunned) return;

        if (CurrentState == PlayerState.Crouching)
            cameraRig.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        else
            cameraRig.transform.localPosition = new Vector3(0f, 1.2f, 0f);
    }

    public void ChangePlayerState(PlayerState state)
    {
        CurrentState = state;
    }

    public void SetActiveHideSpot(InteractableHide hideSpot)
    {
        ActiveHideSpot = hideSpot;
    }

    public void SetHideTransitioning(bool value)
    {
        IsHideTransitioning = value;
    }
}