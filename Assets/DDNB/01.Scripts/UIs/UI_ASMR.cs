using UnityEngine;
using UnityEngine.UI;

public class UI_ASMR : UI_Base
{
    public Slider slider;

    private void Start()
    {
        ResetSlider();
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
