using UnityEngine;

public class UI_Minigame : UI_Base
{
    [SerializeField] private RectTransform barRect;
    [SerializeField] private RectTransform lineRect;

    [SerializeField] private float rotaionSpeed = 200f;

    public void SetBarRect()
    {
        float randomRotationZ = Random.Range(0f, 360f);
        barRect.rotation = Quaternion.Euler(0f, 0f, randomRotationZ);
    }

    public void ResetLine()
    {
        lineRect.localRotation = Quaternion.identity;
    }

    public void RotateLine()
    {
        lineRect.Rotate(0, 0, -rotaionSpeed * Time.deltaTime);
    }

    public bool IsWithinTargetZone()
    {
        float barAngle = barRect.localRotation.eulerAngles.z;
        float lineAngle = lineRect.localRotation.eulerAngles.z;

        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(lineAngle, barAngle));

        return angleDifference <= 45f;
    }
}
