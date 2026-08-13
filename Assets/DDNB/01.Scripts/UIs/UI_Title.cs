using UnityEngine;
using UnityEngine.UI;

public class UI_Title : UI_Base
{
    [SerializeField] private Button button_Start;
    [SerializeField] private Button button_Setting;
    [SerializeField] private Button button_Quit;

    void Start()
    {
        button_Start.onClick.AddListener(GoToLobby);
        button_Setting.onClick.AddListener(() => UIManager.Instance.ShowUI<UI_Setting>());
        button_Quit.onClick.AddListener(QuitGame);
        ApplyMenuCursor();
    }

    private void OnEnable()
    {
        ApplyMenuCursor();
    }

    private void LateUpdate()
    {
        // Game 뷰 빈 곳 클릭 시 Cursor Lock이 다시 걸리지 않도록 유지
        ApplyMenuCursor();
    }

    private static void ApplyMenuCursor()
    {
        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible)
            Cursor.visible = true;
    }

    private void GoToLobby()
    {
        CloseUI();
        NewSceneManager.Instance.GoToLobby();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisable()
    {
        button_Start.onClick.RemoveAllListeners();
        button_Setting.onClick.RemoveAllListeners();
        button_Quit.onClick.RemoveAllListeners();
    }
}
