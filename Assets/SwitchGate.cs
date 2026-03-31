using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Điều khiển cổng bằng tay cầm VR: nhấn Grab vào Toggle → đổi trạng thái cổng (đóng/mở).
/// Control gate via VR hand: press Grab on Toggle → toggle gate state (close/open).
///
/// Setup trong Unity Inspector:
///   1. Thêm XRGrabInteractable + Rigidbody (isKinematic=true) vào object Toggle
///   2. Thêm Collider (Box/Sphere) vào Toggle để tay có thể grab
///   3. Kéo XRGrabInteractable của Toggle vào trường "Toggle Grab"
///   4. Kéo Animator (chứa animation Switch/Gate) vào trường "Anim"
///   5. (Tuỳ chọn) Kéo Collider trên cánh cổng vào "Gate Blocker" để chặn quái
/// </summary>
public class SwitchGate : MonoBehaviour
{
    [Header("Animator")]
    public Animator anim;

    [Header("XR Toggle - Kéo XRGrabInteractable của Toggle vào đây")]
    public XRGrabInteractable toggleGrab;

    [Header("Collider chặn quái khi cổng đóng (tuỳ chọn)")]
    [Tooltip("Collider trên cánh cổng - bật khi đóng để chặn nước mặn đi qua")]
    public Collider gateBlocker;

    private bool isClosed = false;
    private Vector3 toggleFixedLocalPos;
    private Quaternion toggleFixedLocalRot;

    void Start()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (toggleGrab != null)
        {
            // Lưu vị trí gốc để cố định Toggle
            toggleFixedLocalPos = toggleGrab.transform.localPosition;
            toggleFixedLocalRot = toggleGrab.transform.localRotation;

            // Không cho XR tự di chuyển Toggle - tránh bị lấy model lên tay
            toggleGrab.trackPosition = false;
            toggleGrab.trackRotation = false;

            // Khoá Rigidbody hoàn toàn
            Rigidbody rb = toggleGrab.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }

            toggleGrab.selectEntered.AddListener(OnGrab);
        }

        // Khởi tạo cổng mở
        SetGateState(false);
    }

    void OnDestroy()
    {
        if (toggleGrab != null)
        {
            toggleGrab.selectEntered.RemoveListener(OnGrab);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Nhấn Grab → đổi trạng thái cổng
        SetGateState(!isClosed);

        // Ép Toggle về đúng vị trí gốc (phòng trường hợp bị trượt)
        toggleGrab.transform.localPosition = toggleFixedLocalPos;
        toggleGrab.transform.localRotation = toggleFixedLocalRot;
    }

    void LateUpdate()
    {
        // Luôn cố định Toggle tại vị trí gốc mỗi frame
        if (toggleGrab != null)
        {
            toggleGrab.transform.localPosition = toggleFixedLocalPos;
            toggleGrab.transform.localRotation = toggleFixedLocalRot;
        }
    }

    private void SetGateState(bool closed)
    {
        isClosed = closed;

        if (anim != null)
        {
            if (closed)
            {
                anim.Play("Switch_ON", -1, 0f);
                anim.Play("PFB_Gate2_OFF", -1, 0f);
            }
            else
            {
                anim.Play("Switch_OFF", -1, 0f);
                anim.Play("PFB_Gate2_ON", -1, 0f);
            }
        }

        if (gateBlocker != null)
            gateBlocker.enabled = closed;
    }
}
