using UnityEngine;

public abstract class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("InteractableData")]
    public InteractableData data;

    [Header("Input")]
    [SerializeField] protected InteractInputMode inputMode = InteractInputMode.Tap;

    public InteractableData Data => data;
    public InteractInputMode InputMode => inputMode;

    public abstract void Interact();

    public virtual void BeginHold() { }

    public virtual void TickHold(Vector2 lookDelta) { }

    public virtual void EndHold(bool wasHeld) { }

    public virtual void CancelHold() { }
}
