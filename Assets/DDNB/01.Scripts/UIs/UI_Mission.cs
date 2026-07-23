using TMPro;
using UnityEngine;

public class UI_Mission : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentGold;
    [SerializeField] private TextMeshProUGUI missionGold;

    public void SettingMissionGold(float amount)
    {
        missionGold.text = amount.ToString();
    }

    public void RefreshUI(float amount)
    {
        currentGold.text = amount.ToString();
    }
}
