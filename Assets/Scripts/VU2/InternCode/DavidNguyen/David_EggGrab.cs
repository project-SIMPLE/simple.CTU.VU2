using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// David_EggGrab - Dedicated instant grab script for eggs ONLY
// Simple, focused implementation without tree/fruit complexity
// =============================================================================
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class David_EggGrab : MonoBehaviour
{
    [Header("Egg Config / Cấu hình trứng")]
    [SerializeField] private int pointValue = 3;
    [SerializeField] private Sprite eggIcon;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private string bagTag = "Bag";
    
    [Header("Grab Settings / Cài đặt cầm")]
    [SerializeField] private Vector3 grabOffset = new Vector3(0, -0.3f, 0.3f);
    [SerializeField] private float grabScale = 0.5f;
    
    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rb;
    private Transform _currentHandTransform;
    private bool _isGrabbed = false;
    private bool _collected = false;
    private Vector3 _originalScale;

    
    // =========================================================================
    // Unity Lifecycle
    // =========================================================================
    
    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();
        
        // Save original scale for restoration
        _originalScale = transform.localScale;
        
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
            transform.localScale = _originalScale * grabScale;
            
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
        
        // Restore original scale
        transform.localScale = _originalScale;
        
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
        
        CollectEgg();
    }
    
    private void CollectEgg()
    {
        _collected = true;
        
        // Track in SimpleScoreTracker
        var tracker = Thuan_23127_SimpleScoreTracker.Instance;
        if (tracker != null)
        {
            tracker.Track("Egg", eggIcon, pointValue);
        }
        
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
        
        // Destroy egg
        Destroy(gameObject);
    }
}
