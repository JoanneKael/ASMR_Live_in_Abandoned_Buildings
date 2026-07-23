using UnityEngine;
using UnityEngine.UI;

public class UI_ExitTimer : UI_Base
{
    [SerializeField] private Image imageTimer;

    public void FillTimerImage(float amount)
    {
        imageTimer.fillAmount = amount;
    }
}   