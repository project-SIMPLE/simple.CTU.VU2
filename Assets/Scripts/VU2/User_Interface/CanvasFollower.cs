using UnityEngine;

public class CanvasFollower : MonoBehaviour
{
    [SerializeField] Vector3 distanceFromCamera = new Vector3(0, 0, 6);
    [SerializeField] float smoothSpeed = 8.0f;

    [Tooltip("Kéo camera của XR rig vào đây. Nếu để trống sẽ tự tìm trong parent hierarchy.")]
    [SerializeField] Camera targetCamera;

    [Tooltip("Tắt blocksRaycasts trên tất cả Canvas con để XR ray không bị chặn.")]
    [SerializeField] bool disableBlocksRaycasts = true;

    Transform cameraTransform;

    void OnEnable()
    {
        // Ưu tiên camera được gán thủ công, sau đó tìm trong parent hierarchy,
        // cuối cùng mới dùng Camera.main (tránh lấy nhầm camera của XR rig khác)
        if (targetCamera != null)
        {
            cameraTransform = targetCamera.transform;
        }
        else
        {
            cameraTransform = FindCameraInParentHierarchy();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null) return;

        if (disableBlocksRaycasts)
            DisableRaycastBlockingOnAllCanvases();

        Vector3 targetPosition = cameraTransform.position + (cameraTransform.forward * distanceFromCamera.z)
         + (cameraTransform.up * distanceFromCamera.y) + (cameraTransform.right * distanceFromCamera.x);
        
        transform.position = targetPosition;

        float cameraYaw = cameraTransform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0, cameraYaw, 0);
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 targetPosition = cameraTransform.position + (cameraTransform.forward * distanceFromCamera.z)
         + (cameraTransform.up * distanceFromCamera.y) + (cameraTransform.right * distanceFromCamera.x);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        float cameraYaw = cameraTransform.eulerAngles.y;
        float cameraPitch = cameraTransform.eulerAngles.x;
        Quaternion targetRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    /// <summary>
    /// Tìm Camera trong cây cha (parent hierarchy) thay vì dùng Camera.main.
    /// Đi từ parent lên root, tìm Camera trong children của từng cấp.
    /// </summary>
    private Transform FindCameraInParentHierarchy()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            Camera cam = current.GetComponentInChildren<Camera>();
            if (cam != null) return cam.transform;
            current = current.parent;
        }
        return null;
    }

    /// <summary>
    /// Tắt blocksRaycasts trên chính object này VÀ tất cả Canvas con.
    /// Mỗi Canvas tạo scope raycast riêng, nên CanvasGroup cha không ảnh hưởng con.
    /// Phải thêm CanvasGroup cho từng Canvas con để XR ray xuyên qua.
    /// </summary>
    private void DisableRaycastBlockingOnAllCanvases()
    {
        // Tắt trên chính object này
        SetBlocksRaycasts(gameObject, false);

        // Tắt trên tất cả Canvas con (mỗi Canvas có scope raycast riêng)
        foreach (var canvas in GetComponentsInChildren<Canvas>(true))
        {
            SetBlocksRaycasts(canvas.gameObject, false);
        }
    }

    private void SetBlocksRaycasts(GameObject target, bool value)
    {
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = target.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = value;
    }
}