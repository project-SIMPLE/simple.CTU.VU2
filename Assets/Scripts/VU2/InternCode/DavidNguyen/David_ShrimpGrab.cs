using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// David_ShrimpGrab - Dedicated instant grab script for shrimp ONLY
// Combines swimming AI behavior with instant grab teleport
// =============================================================================
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class David_ShrimpGrab : MonoBehaviour
{
    [Header("Shrimp Config / Cấu hình tôm")]
    [SerializeField] private int pointValue = 20;
    [SerializeField] private Sprite shrimpIcon;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private string bagTag = "Bag";
    
    [Header("Grab Settings / Cài đặt cầm")]
    [SerializeField] private Vector3 grabOffset = new Vector3(0, -0.3f, 0.2f);
    [SerializeField] private float grabScale = 0.5f;

    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rb;
    private Transform _currentHandTransform;
    private bool _isGrabbed = false;
    private bool _collected = false;
    private Vector3 _originalScale;

    
    // Swimming AI reference (if exists)
    private MonoBehaviour _swimmingAI;
    
    // =========================================================================
    // Unity Lifecycle
    // =========================================================================
    
    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();
        
        _originalScale = transform.localScale;
        
        _swimmingAI = GetComponent("Thuan_23127_ShrimpAI") as MonoBehaviour;
        
        SetupInstantGrab();
    }
    
    private void SetupInstantGrab()
    {
        if (_grabInteractable == null) return;
        _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        _grabInteractable.attachEaseInTime = 0f;
        _grabInteractable.useDynamicAttach = false;
        _grabInteractable.retainTransformParent = false;
        _grabInteractable.throwOnDetach = false;
        _grabInteractable.trackPosition = false;
        _grabInteractable.trackRotation = false;
        _grabInteractable.startingSingleGrabTransformers.Clear();
        _grabInteractable.startingMultipleGrabTransformers.Clear();
        _grabInteractable.selectEntered.AddListener(OnGrabbed);
        _grabInteractable.selectExited.AddListener(OnReleased);
    }
    
    private void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
    
    // =========================================================================
    // Grab Logic - Instant teleport to hand
    // =========================================================================
    
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRBaseInteractor interactor)
        {
            _currentHandTransform = interactor.transform;
            _isGrabbed = true;
            
            if (_swimmingAI != null)
            {
                _swimmingAI.enabled = false;
            }
            
            transform.SetParent(null);
            
            // Make kinematic
            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            
            // Apply grab scale (make smaller for natural holding)
            Vector3 newScale = _originalScale * grabScale;
            transform.localScale = newScale;
            
            // TELEPORT TO HAND with OFFSET
            Vector3 offsetPosition = _currentHandTransform.position + _currentHandTransform.TransformDirection(grabOffset);
            transform.position = offsetPosition;
            transform.rotation = _currentHandTransform.rotation;
        }
    }
    
    private void OnReleased(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        _currentHandTransform = null;
        
        transform.localScale = _originalScale;
        
        if (_swimmingAI != null)
        {
            _swimmingAI.enabled = true;
        }
        
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
        }
    }
    
    // =========================================================================
    // LateUpdate - FORCE stick to hand position
    // =========================================================================
    
    private void LateUpdate()
    {
        if (_isGrabbed && _currentHandTransform != null)
        {
            transform.localScale = _originalScale * grabScale;
            
            Vector3 offsetPosition = _currentHandTransform.position + _currentHandTransform.TransformDirection(grabOffset);
            transform.position = offsetPosition;
            transform.rotation = _currentHandTransform.rotation;
        }
    }
    
    // =========================================================================
    // Collection Logic - Detect bag collision
    // =========================================================================
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(bagTag)) return;
        if (_collected) return;
        if (!RulesoftheGame_VU2_1.GameActive) return;
        
        if (_isGrabbed) return;
        
        CollectShrimp();
    }
    
    private void CollectShrimp()
    {
        if (_collected) return;
        _collected = true;
        
        // Track in SeasonalSummary
        var summary = Thuan_23127_SeasonalSummary.Instance;
        if (summary != null && shrimpIcon != null)
        {
            summary.TrackDirect("Shrimp", shrimpIcon, pointValue);
        }
        
        // Add score to GameManager
        var gm = Thuan_23127_GameManager.Instance;
        if (gm != null)
        {
            gm.AddScore(pointValue);
        }
        
        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        // Destroy shrimp
        Destroy(gameObject);
    }
}
