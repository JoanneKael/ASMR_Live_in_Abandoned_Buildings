using UnityEngine;

public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("ASMR")]
    public bool isASMRCompleted = false;
    public float currentASMRProgress = 0f;

    [Header("InteractableData")]
    public InteractableData data;

    public InteractableData Data => data;

    public void HoldInteract(float mouseDelta)
    {
        // 문 회전
        if (data.objectType != ObjectType.Door) return;
    }

    public void Interact()
    {
        switch (data.objectType)
        {
            case ObjectType.Door:RapidDoorControl();
                break;
            case ObjectType.ASMR:
                GameManager.Instance.StartASMR(this);
                break;
            //case ObjectType.Consumable_HP:
            //    break;
            //case ObjectType.Consumable_SP:
            //    break;
            //case ObjectType.Key:
            //    break;
        }
    }

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