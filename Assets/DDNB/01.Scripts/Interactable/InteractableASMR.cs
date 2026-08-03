using UnityEngine;

public class InteractableASMR : InteractableObject
{
    [Header("ASMR")]
    public bool isASMRCompleted = false;
    public float currentASMRProgress = 0f;

    private void Reset()
    {
        inputMode = InteractInputMode.Tap;
    }

    public override void Interact()
    {
        if (GameManager.Instance.Player.CurrentState == PlayerState.ASMR)
        {
            ASMRManager.Instance.EndASMR();
            return;
        }

        ASMRManager.Instance.StartASMR(this);
    }
}