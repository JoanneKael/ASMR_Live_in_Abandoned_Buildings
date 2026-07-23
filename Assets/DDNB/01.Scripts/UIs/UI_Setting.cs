using UnityEngine;
using UnityEngine.UI;

public class UI_Setting : UI_Base
{
    [SerializeField] private Button button_Back;

    void Start()
    {
        button_Back.onClick.AddListener(CloseUI);
    }

    private void OnDisable()
    {
        button_Back.onClick.RemoveAllListeners();
    }
}
