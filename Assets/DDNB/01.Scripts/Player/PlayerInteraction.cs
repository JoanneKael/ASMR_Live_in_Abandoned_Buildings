using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float holdThreshold = 0.3f;

    public IInteractable CurrentInteractable { get; private set; }

    /// <summary>홀드/드래그 입력 진행 중인지</summary>
    public bool IsHoldingInteraction { get; private set; }

    /// <summary>ExitDoor 홀드 상호작용 중 — 크로스헤어 숨김용</summary>
    public bool IsHidingCrosshairForInteraction =>
        IsHoldingInteraction && activeInteractable is InteractableExitDoor;

    private float pressStartTime;
    private bool holdBegun;
    private IInteractable activeInteractable;

    private void OnEnable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OnInteractStarted += HandleInteractStarted;
        InputManager.Instance.OnInteractCanceled += HandleInteractCanceled;
    }

    private void OnDisable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OnInteractStarted -= HandleInteractStarted;
        InputManager.Instance.OnInteractCanceled -= HandleInteractCanceled;
        SetCurrentInteractable(null);
    }

    private void Start()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OnInteractStarted -= HandleInteractStarted;
        InputManager.Instance.OnInteractCanceled -= HandleInteractCanceled;
        InputManager.Instance.OnInteractStarted += HandleInteractStarted;
        InputManager.Instance.OnInteractCanceled += HandleInteractCanceled;
    }

    private void Update()
    {
        if (GameManager.Instance.Player == null) return;

        PlayerState state = GameManager.Instance.Player.CurrentState;
        if (GameManager.Instance.Player.IsHideTransitioning) return;
        if (state == PlayerState.Stunned) return;
        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked) return;

        if (state != PlayerState.Interacting
            && state != PlayerState.ASMR
            && state != PlayerState.Lobby
            && state != PlayerState.Hidden)
        {
            CheckInteractable();
        }
        else if (state == PlayerState.ASMR || state == PlayerState.Lobby)
        {
            // ASMR/로비 중에는 포커스 해제
            SetCurrentInteractable(null);
        }

        if (!IsHoldingInteraction || activeInteractable == null) return;
        if (Time.time - pressStartTime < holdThreshold) return;

        if (!holdBegun)
        {
            holdBegun = true;
            activeInteractable.BeginHold();
        }

        activeInteractable.TickHold(InputManager.Instance.MouseDelta);
    }

    private void HandleInteractStarted()
    {
        Player player = GameManager.Instance.Player;
        if (player == null) return;

        if (player.IsHideTransitioning) return;
        if (player.CurrentState == PlayerState.Stunned) return;

        if (player.CurrentState == PlayerState.ASMR)
        {
            ASMRManager.Instance.EndASMR();
            return;
        }

        if (player.CurrentState == PlayerState.Hidden)
        {
            player.ActiveHideSpot?.Interact();
            return;
        }

        if (CurrentInteractable == null) return;

        // 완료된 ASMR은 상호작용 불가 (툴팁만 표시)
        if (CurrentInteractable is InteractableASMR asmr && asmr.isASMRCompleted)
            return;

        // 탈출문: 로비 미준비 / 인게임 미션 미달성이면 홀드 시작하지 않음
        if (CurrentInteractable is InteractableExitDoor
            && !UI_Crosshair.CanUseExitDoorIcon())
        {
            return;
        }

        activeInteractable = CurrentInteractable;
        pressStartTime = Time.time;
        holdBegun = false;

        switch (activeInteractable.InputMode)
        {
            case InteractInputMode.Tap:
                activeInteractable.Interact();
                activeInteractable = null;
                break;

            case InteractInputMode.Hold:
            case InteractInputMode.TapAndDrag:
                IsHoldingInteraction = true;
                break;
        }
    }

    private void HandleInteractCanceled()
    {
        if (activeInteractable == null)
        {
            ClearHoldState();
            return;
        }

        bool wasHeld = Time.time - pressStartTime >= holdThreshold;
        InteractInputMode mode = activeInteractable.InputMode;

        switch (mode)
        {
            case InteractInputMode.Tap:
                break;

            case InteractInputMode.Hold:
                if (wasHeld && holdBegun)
                    activeInteractable.EndHold(true);
                else
                    activeInteractable.CancelHold();
                break;

            case InteractInputMode.TapAndDrag:
                if (wasHeld && holdBegun)
                    activeInteractable.EndHold(true);
                else
                    activeInteractable.Interact();
                break;
        }

        ClearHoldState();
    }

    public void CancelInteraction()
    {
        if (activeInteractable != null && holdBegun)
            activeInteractable.CancelHold();

        ClearHoldState();
    }

    private void ClearHoldState()
    {
        IsHoldingInteraction = false;
        holdBegun = false;
        activeInteractable = null;
    }

    private void CheckInteractable()
    {
        if (Camera.main == null) return;

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            IInteractable hitInteractable = hit.collider.GetComponent<IInteractable>();
            if (hitInteractable == null)
                hitInteractable = hit.collider.GetComponentInParent<IInteractable>();

            SetCurrentInteractable(hitInteractable);
        }
        else
        {
            SetCurrentInteractable(null);
        }
    }

    private void SetCurrentInteractable(IInteractable next)
    {
        if (ReferenceEquals(CurrentInteractable, next)) return;

        CurrentInteractable = next;
        RefreshCrosshairIcon(next);
    }

    private void RefreshCrosshairIcon(IInteractable target)
    {
        if (UI_Crosshair.Instance != null)
            UI_Crosshair.Instance.SetFromInteractable(target);
    }
}
