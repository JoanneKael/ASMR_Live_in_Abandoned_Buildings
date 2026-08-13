using System;
using System.Collections;
using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
    [SerializeField] private float autoOffDelay = 60f;
    [Tooltip("끈 뒤 다시 켤 수 있기까지 대기 시간(초)")]
    [SerializeField] private float turnOnCooldown = 1.5f;

    private Light flashlight;
    private Coroutine turnOffCoroutine;
    private float nextAllowedOnTime;

    public bool IsOn => flashlight != null && flashlight.enabled;

    /// <summary>꺼져 있고 쿨타임이 지났을 때만 켤 수 있음</summary>
    public bool CanTurnOn => flashlight != null && !flashlight.enabled && Time.time >= nextAllowedOnTime;

    private void Start()
    {
        flashlight = GetComponent<Light>();

        if (InputManager.Instance != null)
            InputManager.Instance.OnFlashlightPerformed += ToggleLight;
    }

    private void ToggleLight()
    {
        if (flashlight == null) return;

        // 켜기 시도인데 쿨타임이면 무시
        if (!flashlight.enabled && !CanTurnOn)
            return;

        bool isNowOn = !flashlight.enabled;
        flashlight.enabled = isNowOn;

        if (isNowOn)
        {
            if (turnOffCoroutine != null) StopCoroutine(turnOffCoroutine);
            turnOffCoroutine = StartCoroutine(IE_TurnOffAfterDelay(autoOffDelay));
        }
        else
        {
            if (turnOffCoroutine != null) StopCoroutine(turnOffCoroutine);
            nextAllowedOnTime = Time.time + turnOnCooldown;
        }
    }

    private IEnumerator IE_TurnOffAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 꺼지기 전 점멸 (꺼짐→켜짐 반복 후 최종 소등)
        float[] flickerIntervals = { 0.08f, 0.12f, 0.06f, 0.15f, 0.05f, 0.1f, 0.04f, 0.18f, 0.05f };
        for (int i = 0; i < flickerIntervals.Length; i++)
        {
            flashlight.enabled = !flashlight.enabled;
            yield return new WaitForSeconds(flickerIntervals[i]);
        }

        flashlight.enabled = false;
        nextAllowedOnTime = Time.time + turnOnCooldown;
        turnOffCoroutine = null;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnFlashlightPerformed -= ToggleLight;
    }
}
