using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3f, -5f);
    public float smoothSpeed = 5f;
    public float mouseSensitivity = 3f;

    float currentYaw = 0f;
    float currentPitch = 0f;

    public float pitchMin = -30f;
    public float pitchMax = 30f;

    void LateUpdate()
    {
        ApplyCamera();
    }

    public void SetLookInput(float mouseX, float mouseY)
    {
        currentYaw += mouseX * mouseSensitivity;
        currentPitch -= mouseY * mouseSensitivity;
        currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);

        ApplyCamera();
    }

    void ApplyCamera()
    {
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 desiredPosition = target.position + rotation * offset;

        transform.position = desiredPosition;
        transform.LookAt(target);

        // 캐릭터는 yaw만 따라가게
        target.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }

}
