using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 팝업 — Resume / Setting / Quit.
/// 활성화 시 입력·인게임 시계·timeScale을 멈추고 커서를 표시합니다.
/// </summary>
public class UI_Pause : UI_Base
{
    private const string QuitTextLobby = "게임 끝내기";
    private const string QuitTextInGame = "로비로 돌아가기";

    [Header("Buttons")]
    private Button button_Resume;
    private Button button_Setting;
    private Button button_Quit;

    [Header("Quit Label (Button_Quit 자식 TMP)")]
     private TextMeshProUGUI quitLabel;

    private bool isPaused;

    private void Awake()
    {
        AutoBindIfNeeded();
    }

    private void OnEnable()
    {
        AutoBindIfNeeded();
        BindButtons();
        EnterPause();
    }

    private void OnDisable()
    {
        UnbindButtons();

        // CloseUI로 정상 Resume한 뒤가 아니라, 강제로 꺼진 경우에만 복구
        if (isPaused)
            ExitPause(restoreGameplay: true);
    }

    private void LateUpdate()
    {
        if (!isPaused) return;

        // 클릭으로 커서가 다시 잠기지 않도록 매 프레임 유지
        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible)
            Cursor.visible = true;
    }

    private void AutoBindIfNeeded()
    {
        if (button_Resume == null)
            button_Resume = FindButton("Button_Resume");
        if (button_Setting == null)
            button_Setting = FindButton("Button_Setting");
        if (button_Quit == null)
            button_Quit = FindButton("Button_Quit");

        if (quitLabel == null && button_Quit != null)
            quitLabel = button_Quit.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private Button FindButton(string childName)
    {
        Transform t = transform.Find(childName);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private void BindButtons()
    {
        if (button_Resume != null)
        {
            button_Resume.onClick.RemoveListener(OnClickResume);
            button_Resume.onClick.AddListener(OnClickResume);
        }

        if (button_Setting != null)
        {
            button_Setting.onClick.RemoveListener(OnClickSetting);
            button_Setting.onClick.AddListener(OnClickSetting);
        }

        if (button_Quit != null)
        {
            button_Quit.onClick.RemoveListener(OnClickQuit);
            button_Quit.onClick.AddListener(OnClickQuit);
        }
    }

    private void UnbindButtons()
    {
        if (button_Resume != null) button_Resume.onClick.RemoveListener(OnClickResume);
        if (button_Setting != null) button_Setting.onClick.RemoveListener(OnClickSetting);
        if (button_Quit != null) button_Quit.onClick.RemoveListener(OnClickQuit);
    }

    private void EnterPause()
    {
        isPaused = true;

        RefreshQuitLabel();

        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputBlocked(true);

        if (GameManager.Instance != null)
            GameManager.Instance.PauseIngameClock();

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <param name="restoreGameplay">true면 입력·시계·커서·timeScale 복구</param>
    private void ExitPause(bool restoreGameplay)
    {
        isPaused = false;

        if (!restoreGameplay) return;

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.ResumeIngameClock();

        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputBlocked(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void RefreshQuitLabel()
    {
        if (quitLabel == null) return;

        bool isLobby = NewSceneManager.Instance != null
            && NewSceneManager.Instance.IsCurrentSceneLobby();

        quitLabel.text = isLobby ? QuitTextLobby : QuitTextInGame;
    }

    private void OnClickResume()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsUIActive<UI_Setting>())
            UIManager.Instance.HideUI<UI_Setting>();

        ExitPause(restoreGameplay: true);
        CloseUI();
    }

    private void OnClickSetting()
    {
        // 일시정지 상태 유지 (입력 차단·timeScale·커서 그대로)
        if (UIManager.Instance != null)
            UIManager.Instance.ShowUI<UI_Setting>();
    }

    private void OnClickQuit()
    {
        bool isLobby = NewSceneManager.Instance != null
            && NewSceneManager.Instance.IsCurrentSceneLobby();

        if (isLobby)
        {
            QuitApplication();
            return;
        }

        // 인게임 → 로비 (씬 로드 전 timeScale 복구)
        isPaused = false;
        Time.timeScale = 1f;

        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputBlocked(false);

        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.IsUIActive<UI_Setting>())
                UIManager.Instance.HideUI<UI_Setting>();
        }

        CloseUI();
        NewSceneManager.Instance.GoToLobby();
    }

    private static void QuitApplication()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
