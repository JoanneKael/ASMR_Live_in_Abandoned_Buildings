using System.Collections;
using UnityEngine;

public class InteractableDraggableDoor : InteractableDoor
{
    private float currentAngle;
    [SerializeField] private float rotationSpeed = 2.0f;

    private void Start()
    {
        currentAngle = transform.localRotation.eulerAngles.y;

        if (currentAngle > 180f) currentAngle -= 360f;
    }

    public override void Interact()
    {
        StopAllCoroutines();

        float targetAngle = Data.isOpened ? 0f : -90f;
        Data.isOpened = !Data.isOpened;

        StartCoroutine(RotateDoor(targetAngle));
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

            transform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);

            yield return null;
        }
    }

    public void HoldInteract(Vector2 mouseDelta)
    {
        float delta = -mouseDelta.y * rotationSpeed;
        currentAngle = Mathf.Clamp(currentAngle + delta, -90f, 0f);

        transform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }

    public void FinalizeInteraction()
    {
        float targetAngle = (currentAngle < -45f) ? -90f : 0f;
        Data.isOpened = (targetAngle == -90f);

        StopAllCoroutines();
        StartCoroutine(RotateDoor(targetAngle));
    }
}