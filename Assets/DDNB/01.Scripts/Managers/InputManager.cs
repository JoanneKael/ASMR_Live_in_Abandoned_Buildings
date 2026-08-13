using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public Vector2 MoveValue { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsCrouching { get; private set; }

    /// <summary>true면 ESC(Pause)를 제외한 게임플레이 입력 무시</summary>
    public bool IsGameplayInputBlocked { get; private set; }

    public event Action OnInteractStarted;
    public event Action OnInteractCanceled;
    public event Action OnASMRPerformed;
    public event Action OnFlashlightPerformed;
    public event Action OnPausePerformed;

    private PlayerInput playerInput;
    private InputAction pauseAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        BindPauseAction();
    }

    private void OnDisable()
    {
        UnbindPauseAction();
    }

    private void BindPauseAction()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (playerInput == null || playerInput.actions == null) return;

        pauseAction = playerInput.actions.FindAction("Pause", throwIfNotFound: false);
        if (pauseAction == null) return;

        pauseAction.performed -= OnPauseActionPerformed;
        pauseAction.performed += OnPauseActionPerformed;
    }

    private void UnbindPauseAction()
    {
        if (pauseAction == null) return;
        pauseAction.performed -= OnPauseActionPerformed;
        pauseAction = null;
    }

    private void OnPauseActionPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        OnPausePerformed?.Invoke();
    }

    public void SetGameplayInputBlocked(bool blocked)
    {
        IsGameplayInputBlocked = blocked;
        if (blocked)
        {
            MoveValue = Vector2.zero;
            MouseDelta = Vector2.zero;
            IsRunning = false;
            IsCrouching = false;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (IsGameplayInputBlocked)
        {
            MoveValue = Vector2.zero;
            return;
        }
        MoveValue = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (IsGameplayInputBlocked)
        {
            MouseDelta = Vector2.zero;
            return;
        }
        MouseDelta = context.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (IsGameplayInputBlocked)
        {
            IsRunning = false;
            return;
        }
        IsRunning = context.ReadValueAsButton();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (IsGameplayInputBlocked)
        {
            IsCrouching = false;
            return;
        }
        IsCrouching = context.ReadValueAsButton();
    }

    public void OnInteraction(InputAction.CallbackContext context)
    {
        if (IsGameplayInputBlocked) return;
        if (context.started) OnInteractStarted?.Invoke();
        if (context.canceled) OnInteractCanceled?.Invoke();
    }

    public void OnASMR(InputAction.CallbackContext context)
    {
        if (IsGameplayInputBlocked) return;
        if (context.performed) OnASMRPerformed?.Invoke();
    }

    public void OnFlashlight(InputAction.CallbackContext context)
    {
        if (IsGameplayInputBlocked) return;
        if (context.performed) OnFlashlightPerformed?.Invoke();
    }

    /// <summary>PlayerInput Unity Event용 (프리팹 연결 시). ESC는 입력 차단과 무관.</summary>
    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        // 코드 구독과 중복될 수 있어, 구독이 없을 때만 이벤트 발생
        if (pauseAction == null)
            OnPausePerformed?.Invoke();
    }
}
