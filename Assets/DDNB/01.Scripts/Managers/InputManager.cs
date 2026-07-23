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

    private const float HoldThreshold = 0.3f;
    private float pressStartTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (GetInteractable() == null || !IsInteracting) return;

        if (Time.time - pressStartTime < HoldThreshold) return;

        IInteractable current = GetInteractable();
        if (current is InteractableDraggableDoor doorDraggable) doorDraggable.HoldInteract(MouseDelta);
        else if (current is InteractableDoor doorExit && (GameManager.Instance.AreYouReady || GameManager.Instance.AllMissionCompleted)) doorExit.HoldInteract();
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
        if (GetInteractable() == null) return;

        IInteractable current = GetInteractable();

        // 버튼 누름
        if (context.started)
        {
            pressStartTime = Time.time;

            // 문은 드래그 가능하도록 대기
            if (current is InteractableDoor || current is InteractableDraggableDoor)
            {
                IsInteracting = true;
            }
            // 일반 오브젝트는 바로 실행
            else
            {
                if (GameManager.Instance.Player.CurrentState == PlayerState.ASMR)
                {
                    GameManager.Instance.EndASMR();
                }
                else
                {
                    current.Interact();
                }
            }
        }

        // 버튼 뗌
        if (context.canceled)
        {
            if (current is InteractableDraggableDoor doorDraggable)
            {
                if (Time.time - pressStartTime >= HoldThreshold)
                {
                    // 드래그 종료
                    doorDraggable.FinalizeInteraction();
                }
                else
                {
                    // 짧게 눌렀으면 자동 열기
                    doorDraggable.Interact();
                }
            }
            else if (current is InteractableDoor doorExit)
            {
                if (Time.time - pressStartTime >= HoldThreshold)
                {
                    doorExit.StopHold();
                }
                else
                {
                    doorExit.Interact();
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

    private IInteractable GetInteractable()
    {
        if (GameManager.Instance.Player != null) return GameManager.Instance.Player.GetComponent<PlayerInteraction>().CurrentInteractable;
        else return null;
    }

    public void CancelInteraction()
    {
        if (!IsInteracting) return;
        IsInteracting = false;
    }
}