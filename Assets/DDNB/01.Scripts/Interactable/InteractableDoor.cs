using System.Collections;
using UnityEngine;

/// <summary>
/// 인게임에서 열리고 닫히는 문 (탭 자동 개폐)
/// Y축만 사용, +방향(closed → open)으로 열림
/// </summary>
public class InteractableDoor : InteractableObject
{
    [Header("Door Angles (Y)")]
    [SerializeField] private float closedAngle = 0f;
    [SerializeField] private float openAngle = 90f;

    // --- 홀드&드래그용 (나중에 재활성화 가능) ---
    // [Header("Drag")]
    // [SerializeField] private float rotationSpeed = 2.0f;
    // [SerializeField] private float snapThreshold = 30f;

    private float currentAngle;
    private bool isOpened;

    /// <summary>문이 열려 있는지</summary>
    public bool IsOpened => isOpened;

    private void Reset()
    {
        // inputMode = InteractInputMode.TapAndDrag;
        inputMode = InteractInputMode.Tap;
    }

    private void Awake()
    {
        // inputMode = InteractInputMode.TapAndDrag;
        inputMode = InteractInputMode.Tap;
    }

    private void Start()
    {
        currentAngle = closedAngle;
        isOpened = false;
        transform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }

    public override void Interact()
    {
        StopAllCoroutines();

        float targetAngle = isOpened ? closedAngle : openAngle;
        isOpened = !isOpened;

        StartCoroutine(RotateDoor(targetAngle));

        // 탭 개폐 소음 (임시 비활성)
        // if (NoiseManager.Instance != null)
        // {
        //     NoiseManager.Instance.ReportNoise(
        //         NoiseSource.DoorTap,
        //         transform.position,
        //         bypassCooldown: true);
        // }
    }

    /// <summary>유닛 AI가 닫힌 문을 열 때 사용 (소음 없음)</summary>
    public void OpenForAi()
    {
        if (isOpened) return;

        StopAllCoroutines();
        isOpened = true;
        StartCoroutine(RotateDoor(openAngle));
    }

    /*
    // --- 홀드&드래그 개폐 (나중에 재사용) ---
    public override void BeginHold()
    {
        StopAllCoroutines();
    }

    public override void TickHold(Vector2 lookDelta)
    {
        float delta = lookDelta.y * rotationSpeed;
        currentAngle = Mathf.Clamp(currentAngle + delta, closedAngle, openAngle);
        transform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }

    public override void EndHold(bool wasHeld)
    {
        if (!wasHeld) return;
        FinalizeDrag();
    }

    public override void CancelHold()
    {
        FinalizeDrag();
    }

    private void FinalizeDrag()
    {
        float targetAngle;

        if (currentAngle >= openAngle - snapThreshold)
            targetAngle = openAngle;
        else if (currentAngle <= closedAngle + snapThreshold)
            targetAngle = closedAngle;
        else
            targetAngle = isOpened ? openAngle : closedAngle;

        isOpened = Mathf.Approximately(targetAngle, openAngle);

        StopAllCoroutines();
        StartCoroutine(RotateDoor(targetAngle));
    }
    */

    private IEnumerator RotateDoor(float targetAngle)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        float startAngle = currentAngle;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentAngle = Mathf.Lerp(startAngle, targetAngle, elapsed / duration);
            transform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
            yield return null;
        }

        currentAngle = targetAngle;
        transform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }
}
