using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// BarrackRefillGrab — Player walks up to the water pump and uses the GRIP/GRAB
// gesture (same as shrimp grab) for 5 seconds to add 1 water drop. Capped at
// Barrack.MaxHealth (20).
//
// BarrackRefillGrab — Người chơi tới gần máy bơm rồi bóp GRIP/GRAB
// (giống thao tác bắt tôm) trong 5 giây để cộng thêm 1 giọt nước.
// Tối đa = Barrack.MaxHealth (20).
//
// USAGE: Attach to the WaterPump prefab (same GameObject as Barrack).
//        The prefab already carries an XRSimpleInteractable / XRBaseInteractable
//        — this script subscribes to its select events.
// SỬ DỤNG: Gắn vào prefab WaterPump (cùng GameObject với Barrack).
//          Prefab đã có XRSimpleInteractable / XRBaseInteractable — script
//          chỉ đăng ký lắng nghe sự kiện select của component đó.
// =============================================================================
[RequireComponent(typeof(Barrack))]
public class BarrackRefillGrab : MonoBehaviour
{
    [Header("Refill Settings / Cài đặt nạp nước")]
    [Tooltip("Thời gian giữ grab để cộng 1 đợt nước (giây).\nHold time per refill tick (seconds).")]
    [SerializeField] private float refillDuration = 5f;

    [Tooltip("Lượng nước cộng thêm mỗi lần hoàn thành 5s grab.\nWater added per completed grab cycle.")]
    [SerializeField] private int refillAmount = 1;

    [Tooltip("Tự lặp lại chu kỳ nếu người chơi vẫn đang giữ grab. Bật = bóp giữ liên tục sẽ +1 mỗi 5s.\nLoop while still grabbed: holding grab keeps adding +1 every 5s.")]
    [SerializeField] private bool repeatWhileHeld = true;

    [Header("Feedback / Phản hồi")]
    [SerializeField] private AudioClip refillStartSound;
    [SerializeField] private AudioClip refillCompleteSound;
    [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.4f;

    [Tooltip("(Tùy chọn) Interactable lắng nghe. Nếu bỏ trống, tự lấy XRBaseInteractable trên cùng GameObject.\n(Optional) Interactable to listen on. If empty, auto-find XRBaseInteractable on this GameObject.")]
    [SerializeField] private XRBaseInteractable interactable;

    // Runtime state
    private Barrack _barrack;
    private XRBaseController _currentController;
    private Coroutine _refillCoroutine;
    private bool _isGrabbed;
    private float _progress01;

    /// <summary>EN: Refill progress 0-1, useful for UI. VI: Tiến trình nạp 0-1, dùng cho UI.</summary>
    public float Progress01 => _progress01;
    public bool IsRefilling => _isGrabbed;

    private void Awake()
    {
        _barrack = GetComponent<Barrack>();
        if (interactable == null)
            interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnGrabbed);
            interactable.selectExited.AddListener(OnReleased);
        }
        else
        {
            Debug.LogWarning($"[BarrackRefillGrab] {name} không tìm thấy XRBaseInteractable — sẽ không nạp nước được.");
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnGrabbed);
            interactable.selectExited.RemoveListener(OnReleased);
        }
        StopRefill();
    }

    // =========================================================================
    // GRAB EVENTS
    // =========================================================================
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (_barrack == null || _barrack.IsDead()) return;

        // Lấy controller để rung tay khi đang nạp.
        if (args.interactorObject is XRBaseInteractor interactor)
        {
            _currentController = interactor.GetComponentInParent<XRBaseController>();
        }

        _isGrabbed = true;
        if (refillStartSound != null)
            AudioSource.PlayClipAtPoint(refillStartSound, transform.position);

        if (_refillCoroutine != null) StopCoroutine(_refillCoroutine);
        _refillCoroutine = StartCoroutine(RefillRoutine());
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        StopRefill();
    }

    private void StopRefill()
    {
        _isGrabbed = false;
        _progress01 = 0f;
        _currentController = null;
        if (_refillCoroutine != null)
        {
            StopCoroutine(_refillCoroutine);
            _refillCoroutine = null;
        }
    }

    // =========================================================================
    // REFILL ROUTINE
    // =========================================================================
    private IEnumerator RefillRoutine()
    {
        do
        {
            // Đã đầy bể → ngừng.
            if (_barrack.Health >= _barrack.MaxHealth)
                break;

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, refillDuration);

            while (elapsed < duration)
            {
                if (!_isGrabbed) yield break;
                elapsed += Time.deltaTime;
                _progress01 = Mathf.Clamp01(elapsed / duration);

                // Haptic giảm dần (giống shrimp grab).
                if (_currentController != null)
                {
                    float amp = hapticAmplitude * (1f - _progress01 * 0.5f);
                    _currentController.SendHapticImpulse(amp, Time.deltaTime);
                }
                yield return null;
            }

            // Hoàn thành 1 chu kỳ → cộng nước.
            int added = _barrack.Refill(refillAmount);
            if (added > 0 && refillCompleteSound != null)
                AudioSource.PlayClipAtPoint(refillCompleteSound, transform.position);

            _progress01 = 0f;
        }
        while (repeatWhileHeld && _isGrabbed && _barrack != null && !_barrack.IsDead() && _barrack.Health < _barrack.MaxHealth);

        _refillCoroutine = null;
    }
}
