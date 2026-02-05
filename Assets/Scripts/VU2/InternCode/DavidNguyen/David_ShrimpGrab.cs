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
        
        // Save original scale for restoration
        _originalScale = transform.localScale;
        
        // Try to find swimming AI component (Thuan's shrimp AI)
        _swimmingAI = GetComponent("Thuan_23127_ShrimpAI") as MonoBehaviour;
        
        SetupInstantGrab();
    }
    
    private void SetupInstantGrab()
    {
        if (_grabInteractable == null) return;
        
        // CRITICAL: Configure for instant teleport
        _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        _grabInteractable.attachEaseInTime = 0f;
        _grabInteractable.useDynamicAttach = false;
        _grabInteractable.retainTransformParent = false;
        _grabInteractable.throwOnDetach = false;
        
        // Disable XR tracking - WE control position manually
        _grabInteractable.trackPosition = false;
        _grabInteractable.trackRotation = false;
        
        // Remove any grab transformers
        _grabInteractable.startingSingleGrabTransformers.Clear();
        _grabInteractable.startingMultipleGrabTransformers.Clear();
        
        // Subscribe to events
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
        
        // Get controller transform
        if (args.interactorObject is XRBaseInteractor interactor)
        {
            _currentHandTransform = interactor.transform;
            _isGrabbed = true;
            
            // DISABLE swimming AI when grabbed
            if (_swimmingAI != null)
            {
                _swimmingAI.enabled = false;
            }
            
            // Detach from any parent
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
        else
        {
            Debug.LogError($"[ShrimpGrab] No XRBaseInteractor found!");
        }
    }
    
    private void OnReleased(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        _currentHandTransform = null;
        
        // Restore original scale
        transform.localScale = _originalScale;
        
        // RE-ENABLE swimming AI when released
        if (_swimmingAI != null)
        {
            _swimmingAI.enabled = true;
        }
        
        // Re-enable physics
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
        // If grabbed, FORCE position to hand with offset (overrides any XR system)
        if (_isGrabbed && _currentHandTransform != null)
        {
            // FORCE scale back to grab size (in case other components try to change it)
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
        
        // Don't collect while grabbed - must release first!
        if (_isGrabbed) return;
        
        CollectShrimp();
    }
    
    private void CollectShrimp()
    {
        _collected = true;
        
        // Add points using GameManager
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
