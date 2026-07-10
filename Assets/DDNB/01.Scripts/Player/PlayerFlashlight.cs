using System;
using System.Collections;
using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
    private Light flashlight;
    private Coroutine turnOffCoroutine;

    private void Start()
    {
        flashlight = GetComponent<Light>();

        InputManager.Instance.OnFlashlightPerformed += ToggleLight;
    }

    private void ToggleLight()
    {
        bool isNowOn = !flashlight.enabled;
        flashlight.enabled = isNowOn;

        // 토글 사운드

        if (isNowOn)
        {
            if (turnOffCoroutine != null) StopCoroutine(turnOffCoroutine);
            turnOffCoroutine = StartCoroutine(IE_TurnOffAfterDelay(60f));
        }
        else
        {
            if(turnOffCoroutine != null) StopCoroutine(turnOffCoroutine);
        }
    }

    private IEnumerator IE_TurnOffAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        flashlight.enabled = false;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnFlashlightPerformed -= ToggleLight;
    }
}