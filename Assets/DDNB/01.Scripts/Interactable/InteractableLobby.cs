using UnityEngine;

public class InteractableLobby : InteractableObject
{
    private void Reset()
    {
        inputMode = InteractInputMode.Tap;
    }

    public override void Interact()
    {
        GameManager.Instance.Player.ChangePlayerState(PlayerState.Lobby);

        UIManager.Instance.ShowUI<UI_Lobby>();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
