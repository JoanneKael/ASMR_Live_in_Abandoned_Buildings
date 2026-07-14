using System;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private LayerMask interactableLayer;
    public IInteractable CurrentInteractable { get; private set; }

    private void Start()
    {
        InputManager.Instance.OnASMRPerformed += HandleASMR;
    }

    private void Update()
    {
        CheckInteractable();
    }

    private void CheckInteractable()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            IInteractable hitInteractable = hit.collider.GetComponent<IInteractable>();

            if (hitInteractable == CurrentInteractable) return;

            CurrentInteractable = hitInteractable;
            Debug.Log($"currentInteractable : {CurrentInteractable.Data.itemName}");
        }
        else
        {
            if(CurrentInteractable != null) CurrentInteractable = null;
        }
    }

    private void HandleASMR()
    {
        // TODO
        // UI_Minigame의 Image_Bar 와 Image_JudgeLine이 일치하는지 판단
        // 일치 O : 성공
        // 일치 X : 실패 → asmr 중단, 소음 발생, CurrentState을 Idle로
    }

    private void OnDisable()
    {
        InputManager.Instance.OnASMRPerformed -= HandleASMR;
    }
}