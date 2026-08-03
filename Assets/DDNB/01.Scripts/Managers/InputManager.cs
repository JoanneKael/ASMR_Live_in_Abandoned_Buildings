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

    public event Action OnInteractStarted;
    public event Action OnInteractCanceled;
    public event Action OnASMRPerformed;
    public event Action OnFlashlightPerformed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void OnMove(InputAction.CallbackContext context)
        => MoveValue = context.ReadValue<Vector2>();

    public void OnLook(InputAction.CallbackContext context)
        => MouseDelta = context.ReadValue<Vector2>();

    public void OnRun(InputAction.CallbackContext context)
        => IsRunning = context.ReadValueAsButton();

    public void OnCrouch(InputAction.CallbackContext context)
        => IsCrouching = context.ReadValueAsButton();

    public void OnInteraction(InputAction.CallbackContext context)
    {
        if (context.started) OnInteractStarted?.Invoke();
        if (context.canceled) OnInteractCanceled?.Invoke();
    }

    public void OnASMR(InputAction.CallbackContext context)
    {
        if (context.performed) OnASMRPerformed?.Invoke();
    }

    public void OnFlashlight(InputAction.CallbackContext context)
    {
        if (context.performed) OnFlashlightPerformed?.Invoke();
    }
}
