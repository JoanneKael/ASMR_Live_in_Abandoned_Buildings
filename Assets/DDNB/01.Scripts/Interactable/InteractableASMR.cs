using UnityEngine;

public class InteractableASMR : InteractableObject
{
    public override void Interact()
    {
        GameManager.Instance.StartASMR(this);
    }
}