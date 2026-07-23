using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private LayerMask interactableLayer;
    public IInteractable CurrentInteractable { get; private set; }

    private void Update()
    {
        if (GameManager.Instance.Player == null) return;
        if (GameManager.Instance.Player.CurrentState == PlayerState.Interacting || GameManager.Instance.Player.CurrentState == PlayerState.ASMR || GameManager.Instance.Player.CurrentState == PlayerState.Lobby) return;

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
            if (CurrentInteractable != null) CurrentInteractable = null;
        }
    }
}