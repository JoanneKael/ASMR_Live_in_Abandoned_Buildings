using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private const string PopupUIResourcePath = "PopupUI/";

    private readonly Dictionary<System.Type, Component> _uiCache = new Dictionary<System.Type, Component>();

    private Transform _uiRoot;
    private Transform UIRoot => _uiRoot ? _uiRoot : (_uiRoot = GameObject.Find("@UIRoot")?.transform);

    private Transform _popupRoot;
    private Transform PopupRoot =>
        _popupRoot ? _popupRoot : (_popupRoot = UIRoot != null ? UIRoot.Find("Popup") : null);

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnPausePerformed += HandlePausePerformed;
    }

    private void Start()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnPausePerformed -= HandlePausePerformed;
            InputManager.Instance.OnPausePerformed += HandlePausePerformed;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnPausePerformed -= HandlePausePerformed;
    }

    /// <summary>
    /// Title 씬이 아닐 때 ESC로 UI_Pause 표시.
    /// 이미 활성화되어 있으면 재호출하지 않음.
    /// </summary>
    private void HandlePausePerformed()
    {
        if (IsTitleScene()) return;
        if (IsUIActive<UI_Pause>()) return;
        ShowUI<UI_Pause>();
    }

    private static bool IsTitleScene()
    {
        return SceneManager.GetActiveScene().name == "Title";
    }

    /// <summary>
    /// UI 활성화. 하이라키에 있으면 사용, 없으면 Resources/PopupUI/{타입명} 에서 Popup 아래로 생성.
    /// </summary>
    public T ShowUI<T>() where T : Component
    {
        var type = typeof(T);

        if (!_uiCache.TryGetValue(type, out Component component) || component == null)
        {
            component = UIRoot != null ? UIRoot.GetComponentInChildren<T>(true) : null;

            if (component == null)
                component = TryCreateFromResources<T>();

            if (component != null)
                _uiCache[type] = component;
        }

        if (component is T ui)
        {
            if (!component.gameObject.activeSelf)
                ui.gameObject.SetActive(true);
            // 타이틀에서 먼저 연 UI가 Pause 뒤에 가리지 않도록 맨 앞으로
            ui.transform.SetAsLastSibling();
            return ui;
        }

        Debug.LogWarning($"[UIManager] {typeof(T).Name}을(를) 찾을 수 없습니다. Resources/{PopupUIResourcePath}{typeof(T).Name} Prefab을 확인하세요.");
        return null;
    }

    private T TryCreateFromResources<T>() where T : Component
    {
        if (PopupRoot == null)
        {
            Debug.LogWarning("[UIManager] PopupRoot(@UIRoot/Popup)가 없습니다.");
            return null;
        }

        string path = PopupUIResourcePath + typeof(T).Name;
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, PopupRoot, false);
        instance.name = typeof(T).Name;

        T component = instance.GetComponent<T>();
        if (component == null)
        {
            Debug.LogError($"[UIManager] Prefab '{path}'에 {typeof(T).Name} 컴포넌트가 없습니다.");
            Object.Destroy(instance);
            return null;
        }

        instance.SetActive(false);
        return component;
    }

    public bool IsUIActive<T>() where T : Component
    {
        var type = typeof(T);

        if (_uiCache.TryGetValue(type, out Component component) && component != null)
            return component.gameObject.activeInHierarchy;

        if (UIRoot == null) return false;

        Component found = UIRoot.GetComponentInChildren<T>(true);
        if (found == null) return false;

        _uiCache[type] = found;
        return found.gameObject.activeInHierarchy;
    }

    public void HideUI<T>() where T : Component
    {
        if (!_uiCache.TryGetValue(typeof(T), out Component component) || component == null)
        {
            component = UIRoot != null ? UIRoot.GetComponentInChildren<T>(true) : null;
            if (component == null) return;
            _uiCache[typeof(T)] = component;
        }

        if (component is UI_Base uiBase) uiBase.CloseUI();
        else component.gameObject.SetActive(false);
    }
}
