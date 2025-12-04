using UnityEngine;

public class CanvasFollower : MonoBehaviour
{
    [SerializeField] float distanceFromCamera = 2.0f;
    [SerializeField] float smoothSpeed = 8.0f;

    Transform cameraTransform;

    void OnEnable()
    {
        cameraTransform = Camera.main.transform;

        Vector3 targetPosition = cameraTransform.position + (cameraTransform.forward * distanceFromCamera);
        transform.position = transform.position + targetPosition;
    }

    void LateUpdate()
    {
        Vector3 targetPosition = cameraTransform.position + (cameraTransform.forward * distanceFromCamera);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        float cameraYaw = cameraTransform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0, cameraYaw, 0);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}