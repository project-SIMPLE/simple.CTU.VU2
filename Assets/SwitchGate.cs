using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Điều khiển cổng bằng tay cầm VR: kéo Toggle xuống → cổng đóng, kéo lên → cổng mở.
/// Control gate via VR hand: pull Toggle down → gate closes, push up → gate opens.
///
/// Setup trong Unity Inspector:
///   1. Thêm XRGrabInteractable + Rigidbody (isKinematic=true) vào object Toggle
///   2. Thêm Collider (Box/Sphere) vào Toggle để tay có thể grab
///   3. Kéo XRGrabInteractable của Toggle vào trường "Toggle Grab"
///   4. Kéo Animator (chứa animation Switch/Gate) vào trường "Anim"
///   5. (Tuỳ chọn) Kéo Collider trên cánh cổng vào "Gate Blocker" để chặn quái
///   6. Chỉnh toggleUpY / toggleDownY cho phù hợp vị trí Toggle trong prefab
/// </summary>
public class SwitchGate : MonoBehaviour
{
    [Header("Animator")]
    public Animator anim;

    [Header("XR Toggle - Kéo XRGrabInteractable của Toggle vào đây")]
    public XRGrabInteractable toggleGrab;

    [Header("Phạm vi di chuyển Toggle (Local Y)")]
    [Tooltip("Local Y khi Toggle ở vị trí TRÊN (cổng mở)")]
    public float toggleUpY = 0f;
    [Tooltip("Local Y khi Toggle ở vị trí DƯỚI (cổng đóng)")]
    public float toggleDownY = -0.15f;
    [Tooltip("Ngưỡng kích hoạt (0-1): kéo xuống bao nhiêu thì đóng cổng")]
    [Range(0f, 1f)]
    public float switchThreshold = 0.5f;

    [Header("Collider chặn quái khi cổng đóng (tuỳ chọn)")]
    [Tooltip("Collider trên cánh cổng - bật khi đóng để chặn nước mặn đi qua")]
    public Collider gateBlocker;

    private bool isClosed = false;
    private bool isGrabbed = false;
    private Transform toggleTransform;
    private Vector3 toggleInitLocalPos;
    private float grabStartHandLocalY;
    private float grabStartToggleY;
    private Transform interactorTransform;

    void Start()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (toggleGrab != null)
        {
            toggleTransform = toggleGrab.transform;
            toggleInitLocalPos = toggleTransform.localPosition;

            // Không cho XR tự di chuyển Toggle - ta tự kiểm soát vị trí
            toggleGrab.trackPosition = false;
            toggleGrab.trackRotation = false;

            toggleGrab.selectEntered.AddListener(OnGrab);
            toggleGrab.selectExited.AddListener(OnRelease);
        }

        // Khởi tạo cổng mở
        SetGateState(false);
    }

    void OnDestroy()
    {
        if (toggleGrab != null)
        {
            toggleGrab.selectEntered.RemoveListener(OnGrab);
            toggleGrab.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        interactorTransform = (args.interactorObject as Component)?.transform;

        // Ghi nhớ vị trí tay và Toggle lúc bắt đầu grab
        grabStartHandLocalY = GetHandLocalY();
        grabStartToggleY = toggleTransform.localPosition.y;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        interactorTransform = null;

        // Snap Toggle về vị trí gần nhất (trên hoặc dưới)
        float norm = GetNormalizedPosition();
        SetToggleY(norm >= switchThreshold ? toggleDownY : toggleUpY);
    }

    void Update()
    {
        if (toggleTransform == null) return;

        // Khi đang grab: di chuyển Toggle theo tay, chỉ cho phép trục Y
        if (isGrabbed && interactorTransform != null)
        {
            float handDeltaY = GetHandLocalY() - grabStartHandLocalY;
            float targetY = Mathf.Clamp(grabStartToggleY + handDeltaY, toggleDownY, toggleUpY);
            SetToggleY(targetY);
        }

        // Kiểm tra thay đổi trạng thái cổng
        float norm = GetNormalizedPosition();
        if (!isClosed && norm >= switchThreshold)
            SetGateState(true);
        else if (isClosed && norm < switchThreshold)
            SetGateState(false);
    }

    /// <summary>Lấy vị trí Y của tay trong không gian local của Toggle parent.</summary>
    private float GetHandLocalY()
    {
        if (interactorTransform == null || toggleTransform.parent == null) return 0f;
        return toggleTransform.parent.InverseTransformPoint(interactorTransform.position).y;
    }

    /// <summary>0 = trên (mở), 1 = dưới (đóng)</summary>
    private float GetNormalizedPosition()
    {
        return Mathf.InverseLerp(toggleUpY, toggleDownY, toggleTransform.localPosition.y);
    }

    private void SetToggleY(float y)
    {
        Vector3 pos = toggleInitLocalPos;
        pos.y = y;
        toggleTransform.localPosition = pos;
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
