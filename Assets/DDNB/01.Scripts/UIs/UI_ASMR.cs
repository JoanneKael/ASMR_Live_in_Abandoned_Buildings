using UnityEngine;
using UnityEngine.UI;

public class UI_ASMR : MonoBehaviour
{
    public Slider slider;

    private void Start()
    {
        ResetSlider();
        gameObject.SetActive(false);
    }

    public void SetFillAmount(float amount)
    {
        slider.value = amount;
    }

    public void ResetSlider()
    {
        slider.value = 0;
    }
}
