using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 기절 / 넉아웃 연출 (카메라 + UI).
/// GameManager가 판정 후 PlayStun / PlayKnockOut을 호출합니다.
/// </summary>
public class PlayerHitPresenter : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraRig;
    [SerializeField] private float standCameraY = 1.2f;
    [SerializeField] private float sitCameraY = 0.5f;
    [SerializeField] private float stunSitPitch = 18f;

    [Header("KnockOut Fall (누운 상태)")]
    [Tooltip("바닥에 누웠을 때 눈 높이")]
    [SerializeField] private float knockoutLieCameraY = 0.18f;
    [Tooltip("천장 쪽을 보도록 뒤로 젖힌 피치 (음수 = 상향)")]
    [SerializeField] private float knockoutFallPitch = -90f;
    [Tooltip("뒤로 넘어지며 머리가 밀리는 로컬 Z")]
    [SerializeField] private float knockoutFallBackZ = -0.65f;

    [Header("Stun Timing")]
    [SerializeField] private float stunCloseDuration = 1.0f;
    [SerializeField] private float stunHoldDuration = 1.2f;
    [SerializeField] private float stunOpenDuration = 1.0f;

    [Header("KnockOut Timing")]
    [SerializeField] private float knockoutCloseDuration = 1.75f;
    [SerializeField] private float knockoutOpenDuration = 1.0f;

    private float cameraPitch;

    private void Awake()
    {
        if (cameraRig == null && transform.childCount > 0)
            cameraRig = transform.GetChild(0);
    }

    /// <summary>단순 기절: 자리에서 주저앉으며 암전 → 일어나며 회복 (카운트 유지)</summary>
    public void PlayStun()
    {
        PlayStunAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// 넉아웃: 눈 감기+넘어짐 → onEyesClosed(스폰 이동 등) → 눈 뜨기 → onComplete
    /// </summary>
    public void PlayKnockOut(Action onEyesClosed, Action onComplete)
    {
        PlayKnockOutAsync(onEyesClosed, onComplete, this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>카메라/피치를 기본 상태로 즉시 리셋 (눈 감긴 채 스폰 이동 직후)</summary>
    public void ResetPresentationCamera()
    {
        RestoreCameraImmediate();
    }

    private async UniTaskVoid PlayStunAsync(CancellationToken token)
    {
        Player player = GameManager.Instance != null ? GameManager.Instance.Player : GetComponent<Player>();

        try
        {
            BeginPresentation(player);

            PlayerAudio audio = GetComponent<PlayerAudio>();
            if (audio == null)
                audio = gameObject.AddComponent<PlayerAudio>();
            audio.PlayStunned();

            UI_Stunned stunnedUI = UIManager.Instance != null
                ? UIManager.Instance.ShowUI<UI_Stunned>()
                : null;

            UniTask vignetteIn = stunnedUI != null
                ? stunnedUI.PlayCloseInAsync(stunCloseDuration, token)
                : UniTask.Delay(TimeSpan.FromSeconds(stunCloseDuration), cancellationToken: token);

            UniTask sit = AnimateCameraAsync(sitCameraY, stunSitPitch, stunCloseDuration, token);
            await UniTask.WhenAll(vignetteIn, sit);

            await UniTask.Delay(TimeSpan.FromSeconds(stunHoldDuration), cancellationToken: token);

            UniTask vignetteOut = stunnedUI != null
                ? stunnedUI.PlayOpenOutAsync(stunOpenDuration, token)
                : UniTask.Delay(TimeSpan.FromSeconds(stunOpenDuration), cancellationToken: token);

            UniTask stand = AnimateCameraAsync(standCameraY, 0f, stunOpenDuration, token);
            await UniTask.WhenAll(vignetteOut, stand);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            EndPresentation(player, restoreCamera: true);
        }
    }

    private async UniTaskVoid PlayKnockOutAsync(Action onEyesClosed, Action onComplete, CancellationToken token)
    {
        Player player = GameManager.Instance != null ? GameManager.Instance.Player : GetComponent<Player>();
        bool gameOver = false;

        try
        {
            BeginPresentation(player);

            PlayerAudio audio = GetComponent<PlayerAudio>();
            if (audio == null)
                audio = gameObject.AddComponent<PlayerAudio>();
            audio.PlayKnockout();

            UI_KnockOut knockUI = UIManager.Instance != null
                ? UIManager.Instance.ShowUI<UI_KnockOut>()
                : null;

            UniTask lids = knockUI != null
                ? knockUI.PlayCloseEyesAsync(knockoutCloseDuration, token)
                : UniTask.Delay(TimeSpan.FromSeconds(knockoutCloseDuration), cancellationToken: token);

            // 뒤로 꽈당 → 바닥에 완전히 누운 시점(천장 응시)
            UniTask fall = AnimateCameraAsync(
                knockoutLieCameraY,
                knockoutFallPitch,
                knockoutCloseDuration,
                token,
                knockoutFallBackZ);
            await UniTask.WhenAll(lids, fall);

            // 눈이 완전히 감긴 뒤: 일수 감소·스폰 등 (게임오버여도 결과 UI는 아직 안 염)
            onEyesClosed?.Invoke();

            gameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
            if (gameOver)
            {
                // 넘어지기 연출까지 끝난 뒤 KnockOut 끄고 → 결과 연출 연결
                if (knockUI != null)
                    knockUI.CloseUI();

                if (GameManager.Instance != null)
                    GameManager.Instance.PresentGameOverResult();
                return;
            }

            // 스폰에서 눈 뜨기 + 일어서기
            UniTask open = knockUI != null
                ? knockUI.PlayOpenEyesAsync(knockoutOpenDuration, token)
                : UniTask.Delay(TimeSpan.FromSeconds(knockoutOpenDuration), cancellationToken: token);

            UniTask stand = AnimateCameraAsync(standCameraY, 0f, knockoutOpenDuration, token);
            await UniTask.WhenAll(open, stand);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (!gameOver)
                EndPresentation(player, restoreCamera: true);

            onComplete?.Invoke();
        }
    }

    private void BeginPresentation(Player player)
    {
        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputBlocked(true);

        if (player != null)
            player.ChangePlayerState(PlayerState.Stunned);
    }

    private void EndPresentation(Player player, bool restoreCamera)
    {
        if (restoreCamera)
            RestoreCameraImmediate();

        if (player != null && player.CurrentState == PlayerState.Stunned)
            player.ChangePlayerState(PlayerState.Idle);

        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputBlocked(false);
    }

    /// <param name="targetLocalZ">null이면 현재 Z 유지. 넉아웃 넘어짐 시 뒤로 밀기용.</param>
    private async UniTask AnimateCameraAsync(
        float targetY,
        float targetPitch,
        float duration,
        CancellationToken token,
        float? targetLocalZ = null)
    {
        if (cameraRig == null) return;

        Vector3 startPos = cameraRig.localPosition;
        float endZ = targetLocalZ ?? startPos.z;
        Vector3 endPos = new Vector3(startPos.x, targetY, endZ);
        float startPitch = cameraPitch;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            token.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            cameraRig.localPosition = Vector3.Lerp(startPos, endPos, t);
            cameraPitch = Mathf.Lerp(startPitch, targetPitch, t);
            ApplyCameraPitch();

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        cameraRig.localPosition = endPos;
        cameraPitch = targetPitch;
        ApplyCameraPitch();
    }

    private void ApplyCameraPitch()
    {
        if (cameraRig == null) return;
        cameraRig.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void RestoreCameraImmediate()
    {
        if (cameraRig == null) return;
        Vector3 p = cameraRig.localPosition;
        // 일어선 기본 포즈로 복구 (넘어짐 Z 오프셋 포함 초기화)
        cameraRig.localPosition = new Vector3(p.x, standCameraY, 0f);
        cameraPitch = 0f;
        cameraRig.localRotation = Quaternion.identity;

        PlayerRotation rotation = GetComponent<PlayerRotation>();
        if (rotation != null)
            rotation.SetVerticalRotation(0f);
    }
}
