using UnityEngine;

public class PlayerRotation : MonoBehaviour
{
    [SerializeField] private Transform cameraRig;
    [SerializeField] private float sensitivity = 30.0f;

    private float verticalRotation = 0f;

    private void Update()
    {
        Vector2 mouseDelta = InputManager.Instance.MouseDelta;
        transform.Rotate(Vector3.up * mouseDelta.x * sensitivity * Time.deltaTime);

        verticalRotation -= mouseDelta.y * sensitivity * Time.deltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        cameraRig.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}