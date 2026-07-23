using System.Collections;
using UnityEngine;

public class InteractableDoor : InteractableObject
{
    private bool isCompleted = false;
    private bool isHoldStarted = false;
    private Coroutine holdCoroutine;

    private UI_ExitTimer ui;

    public override void Interact() { }

    public override void HoldInteract()
    {
        if (isHoldStarted) return;

        isHoldStarted = true;
        holdCoroutine = StartCoroutine(IE_Hold());
    }

    private IEnumerator IE_Hold()
    {
        ui = UIManager.Instance.ShowUI<UI_ExitTimer>();

        float timer = 0f;
        float duration = 2f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            ui.FillTimerImage(timer / duration);
            yield return null;
        }

        Debug.Log("이동 성공");

        holdCoroutine = null;
        isCompleted = true;
        if (ui != null) ui.CloseUI();

        GameManager.Instance.ChangeScene();
    }

    public void StopHold()
    {
        Debug.Log("StopHold()");
        if (isCompleted) return;

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }

        isHoldStarted = false;
        if(ui != null) ui.CloseUI();
    }

    private void OnDisable()
    {
        StopHold();

        InputManager.Instance.CancelInteraction();
    }
}