using UnityEngine;
using UnityEngine.UI;

public class UI_Decibel : MonoBehaviour
{
    [SerializeField] private Image image;

    private void Start()
    {
        if (NoiseManager.Instance != null)
            NoiseManager.Instance.OnVolumeChanged += RefreshDecibelImage;
    }

    private void OnDisable()
    {
        if (NoiseManager.Instance != null)
            NoiseManager.Instance.OnVolumeChanged -= RefreshDecibelImage;
    }

    public void RefreshDecibelImage(float normalizedVolume)
    {
        image.fillAmount = Mathf.Lerp(image.fillAmount, normalizedVolume, Time.deltaTime * 10f);
    }
}
