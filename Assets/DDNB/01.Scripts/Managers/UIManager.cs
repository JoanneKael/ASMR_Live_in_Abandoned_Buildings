using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private Dictionary<System.Type, Component> _uiCache = new Dictionary<System.Type, Component>();

    private Transform _uiRoot;
    private Transform UIRoot => _uiRoot ? _uiRoot : (_uiRoot = GameObject.Find("@UIRoot")?.transform);


    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// 제네릭 타입의 UI를 찾아서 활성화/비활성화합니다.
    /// </summary>
    public T ShowUI<T>() where T : Component
    {
        var type = typeof(T);

        if (!_uiCache.TryGetValue(type, out Component component))
        {
            component = UIRoot.GetComponentInChildren<T>(true);
            if (component != null) _uiCache[type] = component;
        }

        if (component is T ui)
        {
            if (!component.gameObject.activeSelf) ui.gameObject.SetActive(true);
            return ui;
        }

        Debug.LogWarning($"{typeof(T).Name}을(를) 찾을 수 없습니다.");
        return null;
    }
}