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
    [Header("Coconut Tree Config / Cấu hình cây dừa")]
    
    [Tooltip("Thời gian cooldown giữa mỗi lần gõ (giây)")]
    public float knockCooldown = 30f;
    
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
    
    [Tooltip("Collider để detect knock (phải là Trigger, bao phủ thân cây)")]
    public Collider knockZoneCollider;

    [Header("Keyboard Testing / Test bằng bàn phím")]
    [Tooltip("Phím để test gõ cây")]
    public KeyCode testKnockKey = KeyCode.E;
    
    [Tooltip("Bật test bằng bàn phím?")]
    public bool enableKeyboardTesting = true;

    [Header("XR Direct Knock (Optional)")]
    [Tooltip("Cho phép trỏ vào thân cây + bóp trigger để gõ")]
    public bool enableXRKnock = true;
    
    [Tooltip("Tag của player hand/controller")]
    public string handTag = "Hand";

    // Internal state
    private float _nextKnockTime = 0f;
    private Vector3 _originalPosition;
    private bool _isShaking = false;
    private bool _handInKnockZone = false;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Start()
    {
        // Auto-find coconuts if not assigned
        if (coconuts == null || coconuts.Length == 0)
        {
            coconuts = GetComponentsInChildren<David_Fruit>(true);
            Debug.Log($"[David_CoconutTree] Tìm thấy {coconuts.Length} quả dừa");
        }

        // Cache original position for shake animation
        if (treeTransform != null)
        {
            _originalPosition = treeTransform.localPosition;
        }
        else
        {
            treeTransform = transform;
            _originalPosition = transform.localPosition;
        }

        // Setup all coconuts - Disable XR Grab while on tree
        foreach (var coconut in coconuts)
        {
            if (coconut != null)
            {
                SetupCoconutOnTree(coconut);
            }
        }
        
        // Auto-find knock zone if not assigned
        if (knockZoneCollider == null)
        {
            // Try to find a trigger collider on this object
            var colliders = GetComponents<Collider>();
            foreach (var col in colliders)
            {
                if (col.isTrigger)
                {
                    knockZoneCollider = col;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        // Keyboard testing
        if (enableKeyboardTesting && Input.GetKeyDown(testKnockKey))
        {
            Debug.Log($"[David_CoconutTree] Keyboard test: {testKnockKey}");
            TryKnockTree();
        }
    }

    // =========================================================================
    // TRIGGER DETECTION FOR KNOCK ZONE
    // ==========================================================================

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's a hand/controller entering the knock zone
        if (IsHandOrPlayer(other))
        {
            _handInKnockZone = true;
            Debug.Log("[David_CoconutTree] Hand entered knock zone");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // If hand is in zone and trigger is pressed, knock the tree
        if (!enableXRKnock) return;
        
        if (IsHandOrPlayer(other))
        {
            // Check for grip or trigger input
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Mouse0))
            {
                TryKnockTree();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsHandOrPlayer(other))
        {
            _handInKnockZone = false;
            Debug.Log("[David_CoconutTree] Hand exited knock zone");
        }
    }
    
    /// <summary>
    /// Checks if collider is hand or player (safe tag check + name fallback).
    /// </summary>
    private bool IsHandOrPlayer(Collider other)
    {
        // Try tag check (safely)
        try
        {
            if (other.CompareTag(handTag)) return true;
            if (other.CompareTag("Player")) return true;
        }
        catch (System.Exception)
        {
            // Tag not defined - use name fallback
        }
        
        // Fallback: check by name
        string name = other.name.ToLower();
        return name.Contains("hand") || 
               name.Contains("controller") || 
               name.Contains("player");
    }

    // =========================================================================
    // COCONUT SETUP
    // ==========================================================================
    
    private void SetupCoconutOnTree(David_Fruit coconut)
    {
        coconut.isOnTree = true;
        coconut.canCollect = false;
        
        // Make kinematic while on tree
        var rb = coconut.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // IMPORTANT: Disable XR Grab while on tree to prevent grab conflicts
        var grabInteractable = coconut.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
            Debug.Log($"[David_CoconutTree] Disabled XRGrabInteractable on {coconut.name}");
        }
    }

    // =========================================================================
    // KNOCK TREE - Main harvesting logic
    // GÕ CÂY - Logic thu hoạch chính
    // =========================================================================

    /// <summary>
    /// Attempts to knock tree (checks cooldown).
    /// Cố gắng gõ cây (kiểm tra cooldown).
    /// </summary>
    public void TryKnockTree()
    {
        // Check cooldown
        if (Time.time < _nextKnockTime)
        {
            float remaining = _nextKnockTime - Time.time;
            Debug.Log($"[David_CoconutTree] Cooldown còn {remaining:F1}s");
            return;
        }

        // Knock the tree!
        KnockTree();
        
        // Set cooldown
        _nextKnockTime = Time.time + knockCooldown;
    }

    /// <summary>
    /// Knocks the tree, causing ONE coconut to fall.
    /// Gõ cây, làm MỘT quả dừa rơi.
    /// </summary>
    public void KnockTree()
    {
        // Play knock sound
        if (knockSound != null)
        {
            AudioSource.PlayClipAtPoint(knockSound, transform.position);
        }

        // Shake tree animation
        if (!_isShaking)
        {
            StartCoroutine(ShakeTree());
        }

        // Find coconuts still on tree
        List<David_Fruit> coconutsOnTree = new List<David_Fruit>();
        foreach (var coconut in coconuts)
        {
            if (coconut != null && coconut.isOnTree)
            {
                coconutsOnTree.Add(coconut);
            }
        }
        
        // Drop only ONE random coconut
        if (coconutsOnTree.Count > 0)
        {
            int randomIndex = Random.Range(0, coconutsOnTree.Count);
            David_Fruit selectedCoconut = coconutsOnTree[randomIndex];
            float delay = Random.Range(minFallDelay, maxFallDelay);
            StartCoroutine(DropCoconutAfterDelay(selectedCoconut, delay));
            
            Debug.Log($"[David_CoconutTree] Đã gõ cây! 1 quả dừa rơi (còn {coconutsOnTree.Count - 1} quả)");
        }
        else
        {
            Debug.Log("[David_CoconutTree] Không còn dừa trên cây!");
        }
    }

    // =========================================================================
    // SHAKE ANIMATION
    // ANIMATION RUNG CÂY
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

        // Reset to original position
        treeTransform.localPosition = _originalPosition;
        _isShaking = false;
    }

    // =========================================================================
    // DROP COCONUT
    // RƠI DỪA
    // =========================================================================

    private IEnumerator DropCoconutAfterDelay(David_Fruit coconut, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (coconut == null) yield break;

        // Update fruit state
        coconut.isOnTree = false;
        coconut.canCollect = true;

        // Detach from tree FIRST
        coconut.transform.SetParent(null);
        
        // Enable physics
        var rb = coconut.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            
            // Add slight random force
            rb.AddForce(Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
        }
        
        // NOW enable XR Grab Interactable so player can pick up the fallen coconut
        var grabInteractable = coconut.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
            Debug.Log($"[David_CoconutTree] Enabled XRGrabInteractable on {coconut.name}");
        }

        // Play fall sound
        if (fallSound != null)
        {
            AudioSource.PlayClipAtPoint(fallSound, coconut.transform.position);
        }

        Debug.Log($"[David_CoconutTree] Dừa rơi: {coconut.name}");
    }

    // =========================================================================
    // UTILITY
    // =========================================================================

    /// <summary>
    /// Gets count of coconuts still on tree.
    /// Lấy số dừa còn trên cây.
    /// </summary>
    public int GetCoconutsOnTree()
    {
        int count = 0;
        foreach (var coconut in coconuts)
        {
            if (coconut != null && coconut.isOnTree)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Resets all coconuts back to tree (for respawning).
    /// Reset tất cả dừa về cây (để respawn).
    /// </summary>
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
        _nextKnockTime = 0f; // Reset cooldown too
        Debug.Log("[David_CoconutTree] Đã reset tất cả dừa về cây");
    }

    // =========================================================================
    // PUBLIC METHOD FOR EXTERNAL CALLS
    // ==========================================================================
    
    /// <summary>
    /// Call this from XR Interactable events or UI buttons to knock tree.
    /// Gọi method này từ XR Interactable events hoặc UI buttons để gõ cây.
    /// </summary>
    public void OnKnockTreeInteraction()
    {
        TryKnockTree();
    }
}
