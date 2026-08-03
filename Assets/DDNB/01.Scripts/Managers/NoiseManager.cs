using System;
using UnityEngine;

/// <summary>
/// 소음 UI 표시와 AI 이벤트(전역 쿨다운)를 전담합니다.
/// </summary>
public class NoiseManager : MonoBehaviour
{
    public static NoiseManager Instance { get; private set; }

    [Header("AI Event Cooldown")]
    [SerializeField] private float eventCooldown = 0.75f;

    [Header("UI - Mic Mapping")]
    [SerializeField] private float quietDb = -60f;
    [SerializeField] private float loudDb = 0f;

    [Header("UI - Running")]
    [SerializeField] private float runningUiFill = 0.75f;

    [Header("UI - Pulse")]
    [SerializeField] private float doorPulseFill = 0.85f;
    [SerializeField] private float doorPulseDuration = 0.2f;
    [SerializeField] private float asmrFailPulseFill = 0.9f;
    [SerializeField] private float asmrFailPulseDuration = 0.2f;

    /// <summary>UI용 0~1 볼륨</summary>
    public event Action<float> OnVolumeChanged;

    /// <summary>AI/게임용 소음 이벤트 (위치, 소스)</summary>
    public event Action<Vector3, NoiseSource> OnNoiseDetected;

    private float nextEventAllowedTime;
    private float pulseEndTime;
    private float pulseFill;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    private void Update()
    {
        float display = GetMicLevel01();

        Player player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player != null && player.CurrentState == PlayerState.Running)
        {
            display = Mathf.Max(display, runningUiFill);
            ReportNoise(NoiseSource.Running, player.transform.position, bypassCooldown: false);
        }

        if (Time.time < pulseEndTime)
            display = Mathf.Max(display, pulseFill);

        OnVolumeChanged?.Invoke(display);
    }

    /// <summary>
    /// 소음 보고. UI는 소스별로 반영하고, AI 이벤트는 전역 쿨다운(또는 bypass)으로 제어합니다.
    /// </summary>
    public void ReportNoise(NoiseSource source, Vector3 worldPosition, bool bypassCooldown)
    {
        ApplyUiForSource(source);

        if (!bypassCooldown && Time.time < nextEventAllowedTime)
            return;

        nextEventAllowedTime = Time.time + eventCooldown;
        OnNoiseDetected?.Invoke(worldPosition, source);

        if (StageManager.Instance != null)
            StageManager.Instance.NotifyUnitHeardNoise(worldPosition);
    }

    private void ApplyUiForSource(NoiseSource source)
    {
        switch (source)
        {
            case NoiseSource.DoorTap:
                StartPulse(doorPulseFill, doorPulseDuration);
                break;
            case NoiseSource.ASMRFail:
                StartPulse(asmrFailPulseFill, asmrFailPulseDuration);
                break;
        }
    }

    private void StartPulse(float fill, float duration)
    {
        pulseFill = fill;
        pulseEndTime = Time.time + duration;
    }

    private float GetMicLevel01()
    {
        if (MICManager.Instance == null) return 0f;
        return Mathf.Clamp01(Mathf.InverseLerp(quietDb, loudDb, MICManager.Instance.CurrentDB));
    }
}
