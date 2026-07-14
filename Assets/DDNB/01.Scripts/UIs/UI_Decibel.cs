using UnityEngine;
using UnityEngine.UI;

public class UI_Decibel : MonoBehaviour
{
    [SerializeField] private Image image;

    private void Start()
    {
        if (MICManager.Instance != null)
            MICManager.Instance.OnVolumeChanged += RefreshDecibelImage;
    }

    private void OnDisable()
    {
        if (MICManager.Instance != null)
            MICManager.Instance.OnVolumeChanged -= RefreshDecibelImage;
    }

    public void RefreshDecibelImage(float normalizedVolume)
    {
        image.fillAmount = Mathf.Lerp(image.fillAmount, normalizedVolume, Time.deltaTime * 10f);
    }
}