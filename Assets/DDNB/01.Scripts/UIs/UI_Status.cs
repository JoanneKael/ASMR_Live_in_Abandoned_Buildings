using UnityEngine;
using TMPro;

public class UI_Status : MonoBehaviour
{
    //[SerializeField] TextMeshProUGUI txtHealth;
    [SerializeField] TextMeshProUGUI txtStamina;


    //public void RefreshHealthUI(float current, float max)
    //{
    //    txtHealth.text = $"{(int)current} / {max}";
    //}

    public void RefreshStaminaUI(float current, float max)
    {
        txtStamina.text = $"{(int)current} / {max}";
    }
}
