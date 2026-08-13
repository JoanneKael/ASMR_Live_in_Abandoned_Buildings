using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerRotation : MonoBehaviour
{
    [SerializeField] private Transform cameraRig;
    [SerializeField] private float sensitivity = 30.0f;

    private float verticalRotation = 0f;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.Player == null) return;

        Player player = GameManager.Instance.Player;
        if (player.CurrentState == PlayerState.Interacting
            || player.CurrentState == PlayerState.Lobby
            || player.CurrentState == PlayerState.Hidden
            || player.CurrentState == PlayerState.Stunned
            || player.IsHideTransitioning
            || (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked))
            return;

        float sens = GetSensitivity();
        Vector2 mouseDelta = InputManager.Instance.MouseDelta;
        transform.Rotate(Vector3.up * mouseDelta.x * sens * Time.deltaTime);

        verticalRotation -= mouseDelta.y * sens * Time.deltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        cameraRig.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    /// <summary>플레이어 정면(Yaw)과 카메라 피치를 목표를 바라보도록 duration 동안 보간</summary>
    public async UniTask FaceTargetAsync(Vector3 targetWorldPos, float duration, CancellationToken token)
    {
        Vector3 eyePos = cameraRig != null ? cameraRig.position : transform.position + Vector3.up * 1.2f;

        Vector3 flatDir = targetWorldPos - transform.position;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f) return;

        Quaternion startYaw = transform.rotation;
        Quaternion endYaw = Quaternion.LookRotation(flatDir.normalized, Vector3.up);

        Vector3 toTarget = targetWorldPos - eyePos;
        float horizontal = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
        float targetPitch = -Mathf.Atan2(toTarget.y, Mathf.Max(0.01f, horizontal)) * Mathf.Rad2Deg;
        targetPitch = Mathf.Clamp(targetPitch, -80f, 80f);

        float startPitch = verticalRotation;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            token.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            transform.rotation = Quaternion.Slerp(startYaw, endYaw, t);
            verticalRotation = Mathf.Lerp(startPitch, targetPitch, t);
            if (cameraRig != null)
                cameraRig.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        transform.rotation = endYaw;
        verticalRotation = targetPitch;
        if (cameraRig != null)
            cameraRig.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    /// <summary>연출 후 카메라 피치를 PlayerRotation 상태와 맞춤</summary>
    public void SetVerticalRotation(float pitch)
    {
        verticalRotation = Mathf.Clamp(pitch, -80f, 80f);
        if (cameraRig != null)
            cameraRig.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private float GetSensitivity()
    {
        // 슬라이더 1.5 = 인스펙터 sensitivity 그대로, 0~3 배율
        float multiplier = SettingManager.DefaultMouseSensitivity;
        if (SettingManager.Instance != null)
            multiplier = SettingManager.Instance.MouseSensitivity;

        return sensitivity * (multiplier / SettingManager.DefaultMouseSensitivity);
    }
}
