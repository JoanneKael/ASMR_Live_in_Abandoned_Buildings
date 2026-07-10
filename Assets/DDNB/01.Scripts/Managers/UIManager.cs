using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private Transform HUD;
    [HideInInspector] public UI_Status ui_Status;


    private void Start()
    {
        HUD = GameObject.Find("HUD").transform;
        ui_Status = HUD.GetComponentInChildren<UI_Status>();
    }
}