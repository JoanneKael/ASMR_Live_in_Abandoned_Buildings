using System.Collections;
using UnityEngine;

/// <summary>
/// 홀드로 씬 이동하는 탈출 문.
/// UI 클릭 이펙트는 없고, 완료 시 exitdoor_lock → exitdoor_open을 연속 재생합니다 (2D SFX_UI).
/// </summary>
public class InteractableExitDoor : InteractableObject
{
    [Header("Audio (2D UI)")]
    [SerializeField] private AudioClip lockClip;
    [SerializeField] private AudioClip openClip;
    [Tooltip("Resources/Sounds/SFX/exitdoor")]
    [SerializeField] private string sfxFolder = "exitdoor";

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
        CacheDefaultClips();
    }

    public override void Interact() { }

    public override void BeginHold()
    {
        if (isHoldStarted || isCompleted) return;

        if (NewSceneManager.Instance.IsCurrentSceneLobby())
        {
            if (!GameManager.Instance.AreYouReady) return;
        }
        else
        {
            if (!GameManager.Instance.AllMissionCompleted) return;
        }

        isHoldStarted = true;
        // UI 이펙트 없음 — 완료 시 lock→open만 재생
        holdCoroutine = StartCoroutine(IE_Hold());
    }

    public override void TickHold(Vector2 lookDelta) { }

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

        ForceCancelPlayerHold();

        // lock 끝난 뒤 open 시작 → DDOL 소스라 씬 전환 후에도 open이 이어질 수 있음
        yield return PlayExitDoorSequence();

        GameManager.Instance.ChangeScene();
    }

    private IEnumerator PlayExitDoorSequence()
    {
        CacheDefaultClips();
        if (SoundManager.Instance == null) yield break;

        if (lockClip != null)
        {
            SoundManager.Instance.PlayUiOneShot(lockClip);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, lockClip.length));
        }

        if (openClip != null)
            SoundManager.Instance.PlayUiOneShot(openClip);
    }

    private void CacheDefaultClips()
    {
        if (SoundManager.Instance == null) return;

        string folder = string.IsNullOrEmpty(sfxFolder)
            ? SoundManager.PathSfxExitDoor
            : $"{SoundManager.SfxResourcesRoot}/{sfxFolder}";

        if (lockClip == null)
            lockClip = SoundManager.Instance.GetNamedClip(folder, "exitdoor_lock");
        if (openClip == null)
            openClip = SoundManager.Instance.GetNamedClip(folder, "exitdoor_open");
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
