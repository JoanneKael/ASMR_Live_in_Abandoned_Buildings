using UnityEngine;

public class InteractableObject : MonoBehaviour, IInteractable
{
    public InteractableData data;

    public InteractableData Data => data;

    public void HoldInteract(float mouseDelta)
    {
        // 문 회전
    }

    public void Interact()
    {

    }
}