using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Điều khiển cổng bằng tay cầm VR: nhấn Grab vào Toggle → đổi trạng thái cổng (đóng/mở).
/// Chỉ đặt script này trên PFB_Switch (object chứa Toggle). KHÔNG cần đặt trên PFB_Gate_G2.
/// Script tự tìm PFB_Gate_G2 trong scene để điều khiển animation cổng.
///
/// Setup trong Unity Inspector:
///   1. Toggle cần có Collider (Box/Sphere) để tay detect được
///   2. Kéo Animator của Switch vào trường "Switch Anim" (tự tìm nếu bỏ trống)
///   3. (Tuỳ chọn) Kéo Animator của Gate vào "Gate Anim" (tự tìm PFB_Gate_G2 trong scene)
///   4. (Tuỳ chọn) Kéo Collider trên cánh cổng vào "Gate Blocker" để chặn quái
/// </summary>
public class SwitchGate : MonoBehaviour
{
    [Header("Animator")]
    [Tooltip("Animator của Switch (chứa Switch_ON / Switch_OFF) - tự lấy trên object này nếu bỏ trống")]
    public Animator switchAnim;

    [Tooltip("Animator của Gate (chứa PFB_Gate2_ON / PFB_Gate2_OFF) - tự tìm PFB_Gate_G2 trong scene nếu bỏ trống")]
    public Animator gateAnim;

    [Header("Toggle Object (tự tìm child 'Toggle' nếu bỏ trống)")]
    public Transform toggleObject;

    [Header("Collider chặn quái khi cổng đóng (tuỳ chọn)")]
    [Tooltip("Collider trên cánh cổng - bật khi đóng để chặn nước mặn đi qua")]
    public Collider gateBlocker;

    private bool isClosed = false;
    private XRBaseInteractable interactable;

    void Start()
    {
        // Tìm Switch Animator trên chính object này
        if (switchAnim == null)
            switchAnim = GetComponent<Animator>();

        // Tìm Gate Animator - ưu tiên theo tên PFB_Gate_G2 trong scene
        if (gateAnim == null)
        {
            GameObject gateObj = GameObject.Find("PFB_Gate_G2");
            if (gateObj != null)
                gateAnim = gateObj.GetComponent<Animator>();
        }

        if (switchAnim != null)
            Debug.Log($"[SwitchGate] Switch Animator: {switchAnim.gameObject.name}", this);
        if (gateAnim != null)
            Debug.Log($"[SwitchGate] Gate Animator: {gateAnim.gameObject.name}", this);
        else
            Debug.LogWarning("[SwitchGate] Không tìm thấy Gate Animator (PFB_Gate_G2)!", this);

        // Tự tìm Toggle nếu chưa gán
        if (toggleObject == null)
            toggleObject = transform.Find("Toggle");
        if (toggleObject == null)
            toggleObject = FindChildRecursive(transform, "Toggle");

        if (toggleObject != null)
        {
            // Ưu tiên dùng XRGrabInteractable có sẵn nhưng tắt di chuyển
            XRGrabInteractable grab = toggleObject.GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                grab.trackPosition = false;
                grab.trackRotation = false;
                grab.throwOnDetach = false;
                grab.movementType = XRGrabInteractable.MovementType.Instantaneous;
                interactable = grab;
            }
            else
            {
                // Fallback: dùng XRSimpleInteractable
                XRSimpleInteractable simple = toggleObject.GetComponent<XRSimpleInteractable>();
                if (simple == null)
                    simple = toggleObject.gameObject.AddComponent<XRSimpleInteractable>();
                interactable = simple;
            }

            interactable.selectEntered.AddListener(OnGrab);

            // Khoá Rigidbody hoàn toàn
            Rigidbody rb = toggleObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }
        else
        {
            Debug.LogWarning($"[SwitchGate] Không tìm thấy Toggle trên {gameObject.name}!", this);
        }

        // Khởi tạo cổng mở
        SetGateState(false);
    }

    void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnGrab);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Nhấn Grab → đổi trạng thái cổng
        SetGateState(!isClosed);
    }

    private void SetGateState(bool closed)
    {
        isClosed = closed;

        if (switchAnim != null)
            switchAnim.Play(closed ? "Switch_ON" : "Switch_OFF", -1, 0f);

        if (gateAnim != null)
            gateAnim.Play(closed ? "PFB_Gate2_OFF" : "PFB_Gate2_ON", -1, 0f);

        if (gateBlocker != null)
            gateBlocker.enabled = closed;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
