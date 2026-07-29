using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewSceneManager : MonoBehaviour
{
    public static NewSceneManager Instance;
    private string lobbySceneName = "Lobby";

    private void Awake()
    {
        Instance = this;
    }

    public void ChangeScene(StageData data)
    {
        string sceneName = data.sceneName;
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);

        if (sceneIndex != -1)
        {
            StartCoroutine(IE_LoadSceneAsync(sceneIndex, () =>
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowUI<HUD>().Init(false);
                }
            }));
        }
        else
        {
            Debug.LogError($"[NewSceneManager] 빌드 설정(Build Settings)에 등록되지 않은 씬 이름입니다: {sceneName}");
        }
    }

    public void GoToLobby()
    {
        int lobbyIndex = SceneUtility.GetBuildIndexByScenePath(lobbySceneName);

        if (lobbyIndex != -1)
        {
            StartCoroutine(IE_LoadSceneAsync(lobbyIndex, () =>
            {
                UIManager.Instance.ShowUI<HUD>().Init(true);
            }));
        }
        else
        {
            Debug.LogError($"[NewSceneManager] 빌드 설정에 로비 씬이 등록되지 않았습니다: {lobbySceneName}");
        }
    }

    private IEnumerator IE_LoadSceneAsync(int sceneIndex, Action onLoaded = null)
    {
        // 비동기 씬 로드 시작
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

        var ui = UIManager.Instance.ShowUI<UI_Loading>();

        // 씬이 완전히 로드될 때까지 대기
        while (!asyncLoad.isDone)
        {
            // 여기서 asyncLoad.progress를 이용해 로딩바 UI를 갱신
            yield return null;
        }

        yield return null;

        GameManager.Instance.Init();

        ui.CloseUI();

        onLoaded?.Invoke();
    }

    public bool IsCurrentSceneLobby()
    {
        return SceneManager.GetActiveScene().name == lobbySceneName;
    }
}