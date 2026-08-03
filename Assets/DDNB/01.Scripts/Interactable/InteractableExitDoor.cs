using System.Collections;
using UnityEngine;

/// <summary>
/// 홀드로 씬 이동하는 탈출 문
/// </summary>
public class InteractableExitDoor : InteractableObject
{
    private bool isCompleted = false;
    private bool isHoldStarted = false;
    private Coroutine holdCoroutine;
    private UI_ExitTimer ui;

    private void Reset()
    {
        inputMode = InteractInputMode.Hold;
    }

    private void Awake()
    {
        inputMode = InteractInputMode.Hold;
    }

    public override void Interact() { }

    public override void BeginHold()
    {
        if (isHoldStarted || isCompleted) return;
        if (!GameManager.Instance.AreYouReady && !GameManager.Instance.AllMissionCompleted) return;

        isHoldStarted = true;
        holdCoroutine = StartCoroutine(IE_Hold());
    }

    public override void TickHold(Vector2 lookDelta)
    {
        // 타이머는 코루틴에서 처리
    }

    public override void EndHold(bool wasHeld)
    {
        if (isCompleted) return;
        CancelHold();
    }

    public override void CancelHold()
    {
        if (isCompleted) return;

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }

        isHoldStarted = false;
        if (ui != null) ui.CloseUI();
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

        holdCoroutine = null;
        isCompleted = true;
        isHoldStarted = false;
        if (ui != null) ui.CloseUI();

        // 홀드 입력 상태를 먼저 해제하고 씬 이동
        ForceCancelPlayerHold();
        GameManager.Instance.ChangeScene();
    }

    private void ForceCancelPlayerHold()
    {
        Player player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player == null) return;

        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (interaction != null) interaction.CancelInteraction();
    }

    private void OnDisable()
    {
        CancelHold();
        ForceCancelPlayerHold();
    }
}
