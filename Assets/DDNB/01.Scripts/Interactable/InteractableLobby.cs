using UnityEngine;

public class InteractableLobby : InteractableObject
{
    public override void Interact()
    {
        GameManager.Instance.Player.ChangePlayerState(PlayerState.Lobby);

        UI_Lobby ui = UIManager.Instance.ShowUI<UI_Lobby>();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}