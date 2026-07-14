using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;

    public static InputManager Instance { get; private set; }

    public Vector2 MoveValue { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsInteracting { get; private set; }

    public event Action OnASMRPerformed;
    public event Action OnFlashlightPerformed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void OnMove(InputAction.CallbackContext context) => MoveValue = context.ReadValue<Vector2>();

    public void OnLook(InputAction.CallbackContext context)
    {
        MouseDelta = context.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext context) => IsRunning = context.ReadValueAsButton();

    public void OnCrouch(InputAction.CallbackContext context) => IsCrouching = context.ReadValueAsButton();

    public void OnInteraction(InputAction.CallbackContext context)
    {
        if (context.started) IsInteracting = true;
        if (context.performed) Debug.Log("홀드 진입");
        if (context.canceled)
        {
            if (IsInteracting)
            {
                // 현재 asmr 중이라면 asmr 중단
                if (GameManager.Instance.Player.CurrentState == PlayerState.ASMR)
                {
                    Debug.Log("ASMR 중단");
                    GameManager.Instance.EndASMR();
                }
                else
                {
                    Debug.Log("일반 상호작용");
                    GameManager.Instance.Player.GetComponent<PlayerInteraction>().CurrentInteractable.Interact();
                }
            }
            IsInteracting = false;
        }
    }

    public void OnASMR(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.Player.CurrentState == PlayerState.ASMR && context.performed) OnASMRPerformed?.Invoke();
    }

    public void OnFlashlight(InputAction.CallbackContext context)
    {
        if (context.performed) OnFlashlightPerformed?.Invoke();
    }
}