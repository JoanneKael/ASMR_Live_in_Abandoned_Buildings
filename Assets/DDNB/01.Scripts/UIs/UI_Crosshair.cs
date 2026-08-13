using UnityEngine;

/// <summary>
/// 조준 대상에 따라 크로스헤어 아이콘을 스왑합니다.
/// 자식: crosshair(기본) / door / hide / asmr / monitor
/// </summary>
public class UI_Crosshair : MonoBehaviour
{
    public static UI_Crosshair Instance { get; private set; }

    public enum IconType
    {
        Default,
        Asmr,
        Door,
        Hide,
        Monitor
    }

    [SerializeField] private GameObject imageDefault;
    [SerializeField] private GameObject imageDoor;
    [SerializeField] private GameObject imageHide;
    [SerializeField] private GameObject imageAsmr;
    [SerializeField] private GameObject imageMonitor;

    private IconType currentIcon = IconType.Default;

    private void Awake()
    {
        Instance = this;
        AutoBindIfNeeded();
        SetIcon(IconType.Default);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void AutoBindIfNeeded()
    {
        if (imageDefault == null)
            imageDefault = FindChild("crosshair");
        if (imageDoor == null)
            imageDoor = FindChild("door");
        if (imageHide == null)
            imageHide = FindChild("hide");
        if (imageAsmr == null)
            imageAsmr = FindChild("asmr");
        if (imageMonitor == null)
            imageMonitor = FindChild("monitor");
    }

    private GameObject FindChild(string childName)
    {
        Transform t = transform.Find(childName);
        if (t != null) return t.gameObject;

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != transform && child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    /// <summary>상호작용 포커스에 맞춰 아이콘 변경. null이면 기본.</summary>
    public void SetFromInteractable(IInteractable target)
    {
        SetIcon(ResolveIcon(target));
    }

    public void SetIcon(IconType type)
    {
        AutoBindIfNeeded();

        if (currentIcon == type
            && IsActive(imageDefault) == (type == IconType.Default)
            && IsActive(imageDoor) == (type == IconType.Door)
            && IsActive(imageHide) == (type == IconType.Hide)
            && IsActive(imageAsmr) == (type == IconType.Asmr)
            && IsActive(imageMonitor) == (type == IconType.Monitor))
        {
            return;
        }

        currentIcon = type;

        SetActiveSafe(imageDefault, type == IconType.Default);
        SetActiveSafe(imageDoor, type == IconType.Door);
        SetActiveSafe(imageHide, type == IconType.Hide);
        SetActiveSafe(imageAsmr, type == IconType.Asmr);
        SetActiveSafe(imageMonitor, type == IconType.Monitor);
    }

    public static IconType ResolveIcon(IInteractable target)
    {
        if (target == null)
            return IconType.Default;

        if (target is InteractableASMR asmr)
            return asmr.isASMRCompleted ? IconType.Default : IconType.Asmr;

        // 로비 PC
        if (target is InteractableLobby)
            return IconType.Monitor;

        // 탈출문: 준비/미션 미완료면 기본 크로스헤어 유지
        if (target is InteractableExitDoor)
        {
            if (!CanUseExitDoorIcon())
                return IconType.Default;
            return IconType.Door;
        }

        if (target is InteractableDoor)
            return IconType.Door;

        if (target is InteractableHide)
            return IconType.Hide;

        return IconType.Default;
    }

    /// <summary>
    /// 로비: 맵·난이도 확인 후(AreYouReady).
    /// 인게임: 미션 금액 달성 후.
    /// </summary>
    public static bool CanUseExitDoorIcon()
    {
        if (GameManager.Instance == null)
            return false;

        bool inLobby = NewSceneManager.Instance != null
            && NewSceneManager.Instance.IsCurrentSceneLobby();

        if (inLobby)
            return GameManager.Instance.AreYouReady;

        return GameManager.Instance.AllMissionCompleted;
    }

    private static bool IsActive(GameObject go) => go != null && go.activeSelf;

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}
