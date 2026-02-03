using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// David_CoconutTree - Coconut tree that drops fruits when knocked.
// David_CoconutTree - Cây dừa rụng quả khi bị gõ.
// =============================================================================
public class David_CoconutTree : MonoBehaviour
{
    public enum InteractionType
    {
        SimpleKnock,   // Gõ bằng tay (Physic Trigger)
        XR_Click,      // Dùng XR Ray/Direct Interactor bấm (Select)
        XR_GrabShake   // Dùng XR Grab để nắm và rung
    }

    [Header("Interaction Mode")]
    public InteractionType interactionMode = InteractionType.XR_Click;

    [Header("Coconut Tree Config / Cấu hình cây dừa")]
    
    [Tooltip("Thời gian cooldown giữa mỗi lần gõ (giây)")]
    public float knockCooldown = 1.0f; // Giảm xuống cho dễ test
    
    [Tooltip("Delay ngẫu nhiên min khi dừa rơi")]
    public float minFallDelay = 0.3f;
    
    [Tooltip("Delay ngẫu nhiên max khi dừa rơi")]
    public float maxFallDelay = 0.8f;
    
    [Tooltip("Thời gian cây rung (animation)")]
    public float shakeDuration = 0.5f;
    
    [Tooltip("Cường độ rung cây")]
    public float shakeIntensity = 0.1f;

    [Header("Audio")]
    public AudioClip knockSound;
    public AudioClip fallSound;

    [Header("References")]
    [Tooltip("Kéo tất cả David_Fruit (dừa) con của cây vào đây")]
    public David_Fruit[] coconuts;
    
    [Tooltip("Transform gốc cây để rung")]
    public Transform treeTransform;
    
    [Tooltip("Collider để detect knock (Optional nếu dùng XR Click/Grab)")]
    public Collider knockZoneCollider;

    [Header("Keyboard Testing")]
    public KeyCode testKnockKey = KeyCode.E;
    public bool enableKeyboardTesting = true;

    // Internal state
    private float _nextKnockTime = 0f;
    private Vector3 _originalPosition;
    private bool _isShaking = false;
    private David_TreeWiltController _wiltController;
    private XRBaseInteractable _currentInteractable;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Start()
    {
        // 1. AUTO-FIND COCONUTS FROM CHILDREN
        coconuts = GetComponentsInChildren<David_Fruit>(true);
        
        // 2. SETUP TREE TRANSFORM
        if (treeTransform == null)
        {
            treeTransform = transform;
        }
        _originalPosition = treeTransform.localPosition;

        // 3. SETUP COCONUTS
        foreach (var coconut in coconuts)
        {
            if (coconut != null) SetupCoconutOnTree(coconut);
        }
        
        // 4. WILT CONTROLLER
        _wiltController = GetComponent<David_TreeWiltController>() ?? GetComponentInParent<David_TreeWiltController>();
        
        // 5. SETUP INTERACTION BASED ON MODE
        SetupInteraction();
    }
    
    private void SetupInteraction()
    {
        // Remove old interactables if switching modes (simple cleanup)
        // Note: In a real project, be careful removing components dynamically.
        
        switch (interactionMode)
        {
            case InteractionType.SimpleKnock:
                // Cần KnockZoneCollider (Trigger)
                if (knockZoneCollider == null)
                    Debug.LogWarning("[David_CoconutTree] SimpleKnock mode cần KnockZoneCollider!");
                break;

            case InteractionType.XR_Click:
                SetupXRSimple();
                break;

            case InteractionType.XR_GrabShake:
                SetupXRGrab();
                break;
        }
    }

    // =========================================================================
    // XR INTERACTABLE SETUP - Allows VR controller to shake tree
    // =========================================================================
    private XRSimpleInteractable _xrInteractable;
    private bool _xrListenerAdded = false;

    [ContextMenu("Setup XR Components (Click Me)")]
    public void ManualSetupXR()
    {
        // 1. Ensure Collider exists
        var col = GetComponent<Collider>();
        if (col == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false; // Recommend solid collider for Raycast
            Debug.Log("[David_CoconutTree] Added BoxCollider (IsTrigger=false).");
        }
        else if (col.isTrigger && interactionMode != InteractionType.SimpleKnock)
        {
            Debug.LogWarning("[David_CoconutTree] Warning: BoxCollider is Trigger. Ray Interactor might ignore it. Consider unchecking IsTrigger.");
        }

        // 2. Setup based on mode
        SetupInteraction();
        Debug.Log($"[David_CoconutTree] Setup complete for mode: {interactionMode}");
    }
    
    private void SetupXRSimple()
    {
        var simple = GetComponent<XRSimpleInteractable>();
        if (simple == null) simple = gameObject.AddComponent<XRSimpleInteractable>();
        
        _currentInteractable = simple;
        
        // Setup Event
        simple.selectEntered.RemoveListener(OnXRSelect);
        simple.selectEntered.AddListener(OnXRSelect);
    }

    private void SetupXRGrab()
    {
        var grab = GetComponent<XRGrabInteractable>();
        if (grab == null) grab = gameObject.AddComponent<XRGrabInteractable>();
        
        _currentInteractable = grab;
        
        // Cấu hình Grab để KHÔNG di chuyển cây
        grab.trackPosition = false;
        grab.trackRotation = false;
        grab.throwOnDetach = false;
        
        // Event
        grab.selectEntered.RemoveListener(OnXRSelect);
        grab.selectEntered.AddListener(OnXRSelect);
    }
    
    private void OnDestroy()
    {
        if (_currentInteractable != null)
        {
            _currentInteractable.selectEntered.RemoveListener(OnXRSelect);
        }
    }
    
    private void OnXRSelect(SelectEnterEventArgs args)
    {
        TryKnockTree();
    }

    private void Update()
    {
        // Keyboard testing - Only allow if player is LOOKING AT the tree (Raycast)
        if (enableKeyboardTesting && Input.GetKeyDown(testKnockKey))
        {
             if (Camera.main == null) return;

             Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
             RaycastHit hit;
             
             // Raycast to check if looking at this tree
             // Note: Tree must have a collider (Mesh or Box) for this to work
             if (Physics.Raycast(ray, out hit, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
             {
                 bool isHit = hit.collider.gameObject == gameObject || 
                              hit.collider.transform.IsChildOf(transform) ||
                              (knockZoneCollider != null && hit.collider == knockZoneCollider);
                              
                 if (isHit)
                 {
                     TryKnockTree();
                 }
             }
        }
    }

    // =========================================================================
    // TRIGGER DETECTION (ONLY FOR SimpleKnock MODE)
    // ==========================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (interactionMode != InteractionType.SimpleKnock) return;

        if (IsHandOrPlayer(other))
        {
            // Auto knock on enter? Or just register?
            // For now, let's auto-knock to simulate physical hit
            TryKnockTree();
        }
    }
    
    private bool IsHandOrPlayer(Collider other)
    {
        string name = other.name.ToLower();
        return other.CompareTag("Player") || 
               other.CompareTag("Hand") || 
               name.Contains("hand") || 
               name.Contains("controller");
    }

    // =========================================================================
    // COCONUT SETUP
    // ==========================================================================
    
    private void SetupCoconutOnTree(David_Fruit coconut)
    {
        coconut.isOnTree = true;
        coconut.canCollect = false;
        
        var rb = coconut.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Disable XR Grab while on tree
        var grabInteractable = coconut.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null) grabInteractable.enabled = false;
    }

    // =========================================================================
    // KNOCK TREE
    // =========================================================================

    public void TryKnockTree()
    {
        if (_wiltController != null && _wiltController.IsWilted) return;
        
        if (Time.time < _nextKnockTime) return;

        KnockTree();
        _nextKnockTime = Time.time + knockCooldown;
    }

    public void KnockTree()
    {
        if (knockSound != null) AudioSource.PlayClipAtPoint(knockSound, transform.position);

        if (!_isShaking) StartCoroutine(ShakeTree());

        // Find coconuts
        List<David_Fruit> coconutsOnTree = new List<David_Fruit>();
        foreach (var coconut in coconuts)
        {
            if (coconut != null && coconut.isOnTree) coconutsOnTree.Add(coconut);
        }
        
        if (coconutsOnTree.Count > 0)
        {
            int randomIndex = Random.Range(0, coconutsOnTree.Count);
            StartCoroutine(DropCoconutAfterDelay(coconutsOnTree[randomIndex], Random.Range(minFallDelay, maxFallDelay)));
        }
    }

    // =========================================================================
    // SHAKE ANIMATION
    // =========================================================================

    private IEnumerator ShakeTree()
    {
        _isShaking = true;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-shakeIntensity, shakeIntensity);
            float z = Random.Range(-shakeIntensity, shakeIntensity);
            treeTransform.localPosition = _originalPosition + new Vector3(x, 0, z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        treeTransform.localPosition = _originalPosition;
        _isShaking = false;
    }

    // =========================================================================
    // DROP LOGIC
    // =========================================================================

    private IEnumerator DropCoconutAfterDelay(David_Fruit coconut, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (coconut == null) yield break;

        coconut.isOnTree = false;
        coconut.canCollect = true;
        coconut.transform.SetParent(null); // Detach
        
        var rb = coconut.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
        }
        
        // Require XR Grab to pick up
        var grabInteractable = coconut.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
            coconut.SetupAfterDrop();
        }

        if (fallSound != null) AudioSource.PlayClipAtPoint(fallSound, coconut.transform.position);
    }
    
    // =========================================================================
    // UTILITY
    // =========================================================================
    
    public void ResetAllCoconuts()
    {
        foreach (var coconut in coconuts)
        {
            if (coconut != null)
            {
                coconut.ResetToTree();
                SetupCoconutOnTree(coconut);
            }
        }
        _nextKnockTime = 0f;
    }
}
