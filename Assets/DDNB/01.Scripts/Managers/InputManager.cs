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
        if(context.interaction is HoldInteraction && context.performed)
        {
            // 오브젝트가 문이면 문 살살 열기/닫기

            Debug.Log("홀드 진입");
            
            IsInteracting = true;
        }
        if (context.canceled)
        {
            if (!IsInteracting)
            {
                Debug.Log("일반 상호작용 실행");

                // 오브젝트가 문이면 문 팍 열기/닫기
                // 오브젝트가 asmr이면 asmr 시작
                // 현재 asmr 중이라면 asmr 중단
            }

            IsInteracting = false;
        }
    }

    public void OnASMR(InputAction.CallbackContext context)
    {
        if (context.performed) OnASMRPerformed?.Invoke();
    }

    public void OnFlashlight(InputAction.CallbackContext context)
    {
        if(context.performed) OnFlashlightPerformed?.Invoke();
    }
}