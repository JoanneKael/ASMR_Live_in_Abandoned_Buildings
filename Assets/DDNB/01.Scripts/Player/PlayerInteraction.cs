using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float holdThreshold = 0.3f;

    public IInteractable CurrentInteractable { get; private set; }

    /// <summary>홀드/드래그 입력 진행 중인지</summary>
    public bool IsHoldingInteraction { get; private set; }

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
    }

    private void Start()
    {
        // Awake 순서에 따라 OnEnable에서 구독 실패했을 수 있음
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
        if (state != PlayerState.Interacting
            && state != PlayerState.ASMR
            && state != PlayerState.Lobby
            && state != PlayerState.Hidden)
        {
            CheckInteractable();
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

        if (player.CurrentState == PlayerState.ASMR)
        {
            ASMRManager.Instance.EndASMR();
            return;
        }

        // 숨은 상태: E로 은신처에서 탈출
        if (player.CurrentState == PlayerState.Hidden)
        {
            player.ActiveHideSpot?.Interact();
            return;
        }

        if (CurrentInteractable == null) return;

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

    /// <summary>
    /// 외부(씬 이동 완료 등)에서 홀드 입력 상태를 강제 해제
    /// </summary>
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
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            IInteractable hitInteractable = hit.collider.GetComponent<IInteractable>();
            if (hitInteractable == CurrentInteractable) return;

            CurrentInteractable = hitInteractable;
            if (CurrentInteractable != null && CurrentInteractable.Data != null)
                Debug.Log($"currentInteractable : {CurrentInteractable.Data.itemName}");
        }
        else
        {
            if (CurrentInteractable != null) CurrentInteractable = null;
        }
    }
}