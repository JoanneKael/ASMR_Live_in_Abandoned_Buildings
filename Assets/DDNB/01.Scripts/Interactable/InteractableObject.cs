using UnityEngine;
public abstract class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("ASMR")]
    public bool isASMRCompleted = false;
    public float currentASMRProgress = 0f;

    [Header("InteractableData")]
    public InteractableData data;

    public InteractableData Data => data;

    public virtual void HoldInteract(){}

    public abstract void Interact();

    private void RapidDoorControl()
    {
        if (data.isOpened)
        {
            Debug.Log("문 닫힘!!");
            data.isOpened = false;
        }
        else
        {
            Debug.Log("문 열림!!");
            data.isOpened = true;
        }

        Debug.Log("@@@@@ 소음 발생 @@@@@");
    }
}