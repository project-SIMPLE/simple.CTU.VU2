using UnityEngine;

public class CanvasFollower : MonoBehaviour
{
    [SerializeField] Vector3 distanceFromCamera = new Vector3(0, 0, 6);
    [SerializeField] float smoothSpeed = 8.0f;

    Transform cameraTransform;

    void OnEnable()
    {
        cameraTransform = Camera.main.transform;

        Vector3 targetPosition = cameraTransform.position + (cameraTransform.forward * distanceFromCamera.z)
         + (cameraTransform.up * distanceFromCamera.y) + (cameraTransform.right * distanceFromCamera.x);
        
        transform.position = targetPosition;

        float cameraYaw = cameraTransform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0, cameraYaw, 0);
    }

    void LateUpdate()
    {
        Vector3 targetPosition = cameraTransform.position + (cameraTransform.forward * distanceFromCamera.z)
         + (cameraTransform.up * distanceFromCamera.y) + (cameraTransform.right * distanceFromCamera.x);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        float cameraYaw = cameraTransform.eulerAngles.y;
        float cameraPitch = cameraTransform.eulerAngles.x;
        Quaternion targetRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}