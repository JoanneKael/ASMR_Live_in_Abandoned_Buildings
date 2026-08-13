using System.Collections;
using UnityEngine;

/// <summary>
/// 인게임에서 열리고 닫히는 문 (탭 자동 개폐).
/// InteractionVolume에 두고, 실제 회전은 doorMesh(SM_Door)에 적용합니다.
/// 문 사운드는 3D SFX — 열기/닫기 클립을 분리 재생합니다.
/// </summary>
public class InteractableDoor : InteractableObject
{
    [Header("Door Mesh")]
    [Tooltip("실제로 회전하는 문 메쉬 (예: SM_Door_Inside_A1)")]
    [SerializeField] private Transform doorMesh;

    [Header("Door Angles (Y)")]
    [SerializeField] private float closedAngle = 0f;
    [SerializeField] private float openAngle = 90f;

    [Header("Audio (3D SFX)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [Tooltip("Resources/Sounds/SFX/door — door_open / door_close")]
    [SerializeField] private string sfxFolder = "door";

    private float currentAngle;
    private bool isOpened;

    public bool IsOpened => isOpened;
    public Transform DoorMesh => doorMesh != null ? doorMesh : transform;

    private void Reset()
    {
        inputMode = InteractInputMode.Tap;
    }

    private void Awake()
    {
        inputMode = InteractInputMode.Tap;

        if (doorMesh == null)
        {
            Transform parent = transform.parent;
            if (parent != null)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child == transform) continue;
                    if (child.name.StartsWith("SM_Door") || child.GetComponent<MeshFilter>() != null)
                    {
                        doorMesh = child;
                        break;
                    }
                }
            }
        }

        EnsureAudioSource();
        CacheDefaultClips();
    }

    private void Start()
    {
        currentAngle = closedAngle;
        isOpened = false;
        ApplyDoorRotation(currentAngle);
    }

    public override void Interact()
    {
        StopAllCoroutines();

        bool willOpen = !isOpened;
        float targetAngle = willOpen ? openAngle : closedAngle;
        isOpened = willOpen;

        PlayDoorSound(willOpen);
        StartCoroutine(RotateDoor(targetAngle));
    }

    /// <summary>유닛 AI가 닫힌 문을 열 때 사용 (같은 3D 문 사운드)</summary>
    public void OpenForAi()
    {
        if (isOpened) return;

        StopAllCoroutines();
        isOpened = true;
        PlayDoorSound(opening: true);
        StartCoroutine(RotateDoor(openAngle));
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (SoundManager.Instance != null)
            SoundManager.Instance.ConfigureWorldSfxSource(audioSource);
        else
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 12f;
            audioSource.dopplerLevel = 0f;
        }
    }

    private void CacheDefaultClips()
    {
        if (SoundManager.Instance == null) return;

        string folder = string.IsNullOrEmpty(sfxFolder)
            ? SoundManager.PathSfxDoor
            : $"{SoundManager.SfxResourcesRoot}/{sfxFolder}";

        if (openClip == null)
            openClip = SoundManager.Instance.GetNamedClip(folder, "door_open");
        if (closeClip == null)
            closeClip = SoundManager.Instance.GetNamedClip(folder, "door_close");
    }

    private void PlayDoorSound(bool opening)
    {
        EnsureAudioSource();
        CacheDefaultClips();
        if (audioSource == null) return;

        AudioClip clip = opening ? openClip : closeClip;
        if (clip == null) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayOneShot(audioSource, clip);
        else
            audioSource.PlayOneShot(clip);
    }

    private IEnumerator RotateDoor(float targetAngle)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        float startAngle = currentAngle;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentAngle = Mathf.Lerp(startAngle, targetAngle, elapsed / duration);
            ApplyDoorRotation(currentAngle);
            yield return null;
        }

        currentAngle = targetAngle;
        ApplyDoorRotation(currentAngle);
    }

    private void ApplyDoorRotation(float angleY)
    {
        DoorMesh.localRotation = Quaternion.Euler(0f, angleY, 0f);
    }
}
