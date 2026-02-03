using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

// =============================================================================
// FruitType - Types of collectible products in the game.
// FruitType - Các loại sản phẩm có thể thu hoạch trong game.
// =============================================================================
public enum FruitType 
{ 
    Coconut,  // Coconut / Dừa
    Durian,   // Durian / Sầu riêng
    Fish,     // Fish / Cá
    Shrimp,   // Shrimp / Tôm
    Rice      // Rice / Lúa
}

// =============================================================================
// David_Fruit - Handles collectible fruit/product behavior and scoring.
// David_Fruit - Xử lý hành vi thu hoạch trái cây/sản phẩm và tính điểm.
// 
// Attach this to any collectible prefab (fruits, fish, rice, etc.).
// When player's bag touches this object, it calculates score and disappears.
// 
// Gắn script này vào bất kỳ prefab có thể thu hoạch (trái cây, cá, lúa, v.v.).
// Khi túi của người chơi chạm vào object này, nó tính điểm và biến mất.
// 
// Score is calculated based on: Zone (Fresh/Salt) × Season (Rainy/Dry)
// Điểm được tính dựa trên: Vùng (Ngọt/Lợ) × Mùa (Mưa/Khô)
// 
// NEW: Auto-pull grab - When grabbed, automatically flies to hand in 0.2s
// MỚI: Auto-pull grab - Khi grab, tự động bay vào tay trong 0.2s
// =============================================================================
public class David_Fruit : MonoBehaviour
{
    [Header("Fruit Type / Loại sản phẩm")]
    // -------------------------------------------------------------------------
    // Select the type of product this object represents.
    // Chọn loại sản phẩm mà object này đại diện.
    // -------------------------------------------------------------------------
    public FruitType fruitType = FruitType.Coconut;
    
    [Header("Zone Source / Nguồn xác định Vùng")]
    [Tooltip("Drag the FarmArea here to determine Fresh/Salt zone. Auto-finds if empty.")]
    // -------------------------------------------------------------------------
    // Reference to parent FarmArea - determines if this is Fresh or Salt water zone.
    // Tham chiếu đến FarmArea cha - xác định đây là vùng nước Ngọt hay Lợ.
    // -------------------------------------------------------------------------
    public FarmArea ownerArea;
    
    [Header("Harvest Bag Tag / Tag túi thu hoạch")]
    // -------------------------------------------------------------------------
    // Tag of the player's collection bag. Default is "Bag".
    // Tag của túi thu hoạch của người chơi. Mặc định là "Bag".
    // -------------------------------------------------------------------------
    public string bagTag = "Bag";

    [Header("Other Config / Cấu hình khác")]
    [Tooltip("true: Destroy on collect. false: Deactivate only (for respawn).")]
    // -------------------------------------------------------------------------
    // If true, object is destroyed after collection.
    // If false, object is just deactivated (can be respawned by TreeSpawner).
    // Nếu true, object bị hủy sau khi thu hoạch.
    // Nếu false, object chỉ bị ẩn (có thể respawn bởi TreeSpawner).
    // -------------------------------------------------------------------------
    public bool destroyOnCollect = true;

    [Header("Audio (Optional)")]
    // -------------------------------------------------------------------------
    // Sound to play when collected. Leave empty for no sound.
    // Âm thanh phát khi thu hoạch. Để trống nếu không cần âm thanh.
    // -------------------------------------------------------------------------
    public AudioClip collectSound;

    [Header("Tree Harvesting / Thu hoạch trên cây")]
    // -------------------------------------------------------------------------
    // For fruits on trees (Coconut, Durian).
    // Cho trái cây trên cây (Dừa, Sầu riêng).
    // -------------------------------------------------------------------------
    [Tooltip("True nếu quả đang trên cây (chưa rụng)")]
    public bool isOnTree = false;
    
    [Tooltip("True nếu quả có thể thu hoạch (đã rụng xuống đất)")]
    public bool canCollect = true;
    
    [Tooltip("Âm thanh khi rơi từ cây")]
    public AudioClip dropSound;
    
    [Header("Grab Settings / Cài đặt cầm")]
    [Tooltip("Offset vị trí khi cầm - Dừa cần xa hơn vì to, Sầu riêng gần hơn")]
    public Vector3 grabOffset = new Vector3(0f, -0.3f, 0.3f); // Default cho dừa
    
    [Tooltip("Scale khi đang cầm (1 = giữ nguyên) - Sầu riêng nên để 1.0")]
    [Range(0.1f, 1f)]
    public float grabScale = 0.5f; // Default cho dừa
    
    [Tooltip("Nếu true, sẽ tự động điều chỉnh offset/scale theo loại trái cây")]
    public bool autoAdjustByFruitType = true;
    
    // Store original scale for restore
    private Vector3 _originalScale;
    
    // Store original position for reset
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Transform _originalParent;
    
    // -------------------------------------------------------------------------
    // Prevents double-collection in same frame.
    // Ngăn thu hoạch hai lần trong cùng một frame.
    // -------------------------------------------------------------------------
    private bool _collected = false;
    
    // =========================================================================
    // Start - Auto-find parent FarmArea if not assigned in Inspector.
    // Start - Tự động tìm FarmArea cha nếu chưa gán trong Inspector.
    // =========================================================================
    private void Start()
    {
        // Auto-find parent FarmArea if not assigned.
        // Tự động tìm FarmArea cha nếu chưa gán.
        if (ownerArea == null)
        {
            ownerArea = GetComponentInParent<FarmArea>();
        }
        
        // Save original transform for ResetToTree
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        _originalParent = transform.parent;
        
        // Setup XR Grab Interactable for instant grab
        SetupInstantGrab();
        
        // Auto-setup tree fruits
        if (fruitType == FruitType.Coconut || fruitType == FruitType.Durian)
        {
            // If parent has tree script, assume on tree
            if (GetComponentInParent<David_CoconutTree>() != null || 
                GetComponentInParent<David_DurianTree>() != null)
            {
                isOnTree = true;
                canCollect = false;
                
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    Debug.Log($"[David_Fruit] {fruitType} Rigidbody set to kinematic (on tree)");
                }
            }
        }
    }
    
    // =========================================================================
    // SetupInstantGrab - Configure XR Grab for instant snap to hand.
    // SetupInstantGrab - Cấu hình XR Grab để snap ngay lập tức vào tay.
    // =========================================================================
    private bool _isInstantGrabSetup = false;
    
    private void OnEnable()
    {
        // Reset collection state when re-enabled (respawn)
        _collected = false;
        
        // Re-setup when enabled (for coconuts that start disabled on tree)
        if (!_isInstantGrabSetup)
        {
            SetupInstantGrab();
        }
    }
    
    /// <summary>
    /// Call this after dropping from tree to setup instant grab.
    /// Gọi method này sau khi rơi khỏi cây để setup instant grab.
    /// </summary>
    public void SetupAfterDrop()
    {
        // Reset flag so setup runs again
        _isInstantGrabSetup = false;
        SetupInstantGrab();
    }
    
    private void SetupInstantGrab()
    {
        var grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null && grabInteractable.enabled)
        {
            // REMOVE existing listeners first to prevent duplicates!
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
            
            // Only setup once to avoid duplicate event subscriptions
            if (_isInstantGrabSetup) return;
            _isInstantGrabSetup = true;
            
            // INSTANT SNAP TO HAND - Not to ray hit point!
            grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grabInteractable.attachEaseInTime = 0f;
            
            // IMPORTANT: Disable dynamic attach so object snaps to HAND, not ray hit point
            grabInteractable.useDynamicAttach = false;
            
            // CRITICAL: Don't retain parent! This prevents re-parenting to tree on grab!
            grabInteractable.retainTransformParent = false;
            
            // Subscribe to grab/release events
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            
            Debug.Log($"[David_Fruit] Instant grab configured for {gameObject.name}");
        }
    }
    
    private void OnDestroy()
    {
        var grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
    
    // =========================================================================
    // OnGrabbed - Called when grabbed, teleport to hand immediately!
    // OnGrabbed - Gọi khi grab, dịch chuyển về tay ngay lập tức!
    // =========================================================================
    
    // Track grab state for LateUpdate
    private Transform _currentGrabTarget;
    private XRGrabInteractable _currentGrabInteractable;
    
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log($"[David_Fruit] ===== OnGrabbed START for {gameObject.name} =====");
        
        // Mark as no longer on tree
        isOnTree = false;
        canCollect = true;
        
        // CRITICAL: DETACH FROM ANY PARENT FIRST!
        if (transform.parent != null)
        {
            Debug.Log($"[David_Fruit] Detaching from parent: {transform.parent.name}");
            transform.SetParent(null);
        }
        
        // MANUAL TELEPORT TO HAND - Force snap regardless of XR settings!
        if (args.interactorObject is XRBaseInteractor interactor)
        {
            // USE INTERACTOR.TRANSFORM - This is the CONTROLLER, not ray hit point!
            _currentGrabTarget = interactor.transform;
            _currentGrabInteractable = GetComponent<XRGrabInteractable>();
            
            // DISABLE ALL XR TRACKING AND CONTROL!
            if (_currentGrabInteractable != null)
            {
                _currentGrabInteractable.trackPosition = false;
                _currentGrabInteractable.trackRotation = false;
                _currentGrabInteractable.retainTransformParent = false;
            }
            
            // MAKE RIGIDBODY KINEMATIC - Stop physics from interfering!
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // FORCE DETACH AGAIN after XR might have re-parented!
            transform.SetParent(null);
            
            // DETERMINE ACTUAL OFFSET AND SCALE based on fruit type
            Vector3 actualOffset = grabOffset;
            float actualScale = grabScale;
            
            if (autoAdjustByFruitType)
            {
                switch (fruitType)
                {
                    case FruitType.Durian:
                        actualOffset = new Vector3(0f, -0.1f, 0.15f);
                        actualScale = 1f;
                        break;
                    case FruitType.Coconut:
                        actualOffset = grabOffset;
                        actualScale = grabScale;
                        break;
                    default:
                        actualOffset = Vector3.zero;
                        actualScale = 1f;
                        break;
                }
            }
            
            // SAVE ORIGINAL SCALE and apply grab scale
            _originalScale = transform.localScale;
            transform.localScale = _originalScale * actualScale;
            
            // TELEPORT to CONTROLLER position with OFFSET!
            Vector3 offsetPosition = _currentGrabTarget.position + _currentGrabTarget.TransformDirection(actualOffset);
            transform.position = offsetPosition;
            transform.rotation = _currentGrabTarget.rotation;
            
            Debug.Log($"[David_Fruit] Grabbed {fruitType}. Scale: {actualScale}, Offset: {actualOffset}");
        }
        else
        {
            Debug.Log($"[David_Fruit] Grabbed {gameObject.name} (no teleport - unknown interactor)");
        }
    }
    
    // =========================================================================
    // LateUpdate - Force position to hand AFTER XR Update runs!
    // LateUpdate - Giữ vị trí tại tay SAU KHI XR Update chạy!
    // =========================================================================
    private void LateUpdate()
    {
        // Fail-safe: If interactable says NOT selected, but we still have a target -> Clear it to preventing sticking!
        if (_currentGrabTarget != null && _currentGrabInteractable != null && !_currentGrabInteractable.isSelected)
        {
            Debug.LogWarning($"[David_Fruit] {gameObject.name} STUCK STATE DETECTED! Force releasing.");
            OnReleased(new SelectExitEventArgs()); // Simulate release
            return;
        }

        // If grabbed, force position to hand
        if (_currentGrabTarget != null)
        {
            // FORCE: Keep detached from any parent!
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            
            // DETERMINE ACTUAL OFFSET based on fruit type (same logic as OnGrabbed)
            Vector3 actualOffset = grabOffset;
            if (autoAdjustByFruitType)
            {
                actualOffset = fruitType switch
                {
                    FruitType.Durian => new Vector3(0f, -0.1f, 0.15f),
                    FruitType.Coconut => grabOffset,
                    _ => Vector3.zero
                };
            }
            
            // FORCE: Position at controller WITH OFFSET
            Vector3 offsetPosition = _currentGrabTarget.position + _currentGrabTarget.TransformDirection(actualOffset);
            transform.position = offsetPosition;
            transform.rotation = _currentGrabTarget.rotation;
        }
    }
    
    // =========================================================================
    // OnReleased - Called when released, restore XR tracking.
    // OnReleased - Gọi khi thả, khôi phục XR tracking.
    // =========================================================================
    private void OnReleased(SelectExitEventArgs args)
    {
        // Restore XR tracking
        if (_currentGrabInteractable != null)
        {
            _currentGrabInteractable.trackPosition = true;
            _currentGrabInteractable.trackRotation = true;
        }
        
        // RESTORE ORIGINAL SCALE
        if (_originalScale != Vector3.zero)
        {
            transform.localScale = _originalScale;
        }
        
        // RESTORE RIGIDBODY - Enable physics again
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        _currentGrabTarget = null;
        Debug.Log($"[David_Fruit] Released {gameObject.name}");
    }
    
    // =========================================================================
    // OnTriggerEnter - Detects when player's bag touches this fruit (trigger mode).
    // OnTriggerEnter - Phát hiện khi túi người chơi chạm trái cây này (chế độ trigger).
    // =========================================================================
    private void OnTriggerEnter(Collider other)
    {
        // Already collected - ignore.
        // Đã thu hoạch rồi - bỏ qua.
        if (_collected) return;
        
        // Only react to objects with bag tag.
        // Chỉ phản ứng với object có tag túi.
        if (!other.CompareTag(bagTag)) return;
        
        Debug.Log($"[David_Fruit] OnTriggerEnter with bag: {other.name} on {gameObject.name}");
        TryCollect();
    }
    
    // =========================================================================
    // OnCollisionEnter - Detects when player's bag touches this fruit (collision mode).
    // OnCollisionEnter - Phát hiện khi túi chạm trái cây này (chế độ collision).
    // =========================================================================
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(bagTag)) return;
        TryCollect();
    }
    
    // =========================================================================
    // TryCollect - Validates collection conditions before actually collecting.
    // TryCollect - Kiểm tra điều kiện thu hoạch trước khi thực sự thu hoạch.
    // 
    // Checks: Is game active? Is this fruit harvestable in current season?
    // Kiểm tra: Game có đang chạy không? Trái này có thể thu hoạch trong mùa hiện tại không?
    // =========================================================================
    private void TryCollect()
    {
        // Can't collect while being grabbed - must release first!
        // Không thể thu hoạch khi đang cầm - phải thả ra trước!
        if (_currentGrabTarget != null)
        {
            Debug.Log($"[David_Fruit] {fruitType} đang được cầm, không thể bỏ vào bag");
            return;
        }
        
        // Check if fruit can be collected (for tree fruits).
        // Kiểm tra xem quả có thể thu hoạch không (cho quả trên cây).
        if (!canCollect)
        {
            Debug.Log($"[David_Fruit] {fruitType} chưa thể thu hoạch (còn trên cây)");
            return;
        }
        
        // Game must be active to collect.
        // Game phải đang hoạt động mới thu hoạch được.
        if (!RulesoftheGame_VU2_1.GameActive)
        {
            Debug.Log("[David_Fruit] Game chưa bắt đầu!");
            return;
        }
        
        // SPECIAL RULE: Durian can only be harvested in rainy season.
        // Durian wilts and dies in dry season (high salinity).
        // QUY TẮC ĐẶC BIỆT: Sầu riêng chỉ hái được vào mùa mưa.
        // Sầu riêng héo và chết trong mùa khô (độ mặn cao).
        if (fruitType == FruitType.Durian)
        {
            if (RulesoftheGame_VU2_1.Saltwater_Intrusion >= 1f)
            {
                Debug.Log("[David_Fruit] Sầu riêng bị héo - không thể thu hoạch mùa khô!");
                return;
            }
        }
        
        CollectFruit();
    }
    
    // =========================================================================
    // GetTableScore - Returns score based on Zone × Season lookup table.
    // GetTableScore - Trả về điểm dựa trên bảng tra cứu Vùng × Mùa.
    // 
    // SCORE TABLE (updated with production-based values):
    // BẢNG ĐIỂM (cập nhật với giá trị dựa trên sản lượng):
    // 
    // | Type     | Fresh+Rainy | Fresh+Dry | Salt+Rainy | Salt+Dry |
    // |----------|-------------|-----------|------------|----------|
    // | Durian   | 100         | 80        | 60         | -40      |
    // | Coconut  | 100         | 80        | 60         | 50       |
    // | Fish     | 10          | 20        | 30         | 40       |
    // | Shrimp   | 20          | 20        | 20         | 20       |
    // | Rice     | 60          | -20       | 40         | 20       |
    // 
    // Negative scores = crop failure (plant dies, loss of investment).
    // Điểm âm = thất bại mùa vụ (cây chết, mất vốn đầu tư).
    // =========================================================================
    private int GetTableScore()
    {
        // Determine zone type (default to Fresh if no FarmArea).
        // Xác định loại vùng (mặc định là Ngọt nếu không có FarmArea).
        bool isFresh = true;
        if (ownerArea != null)
        {
            isFresh = (ownerArea.waterType == WaterType.Fresh);
        }
        
        // Determine season (Rainy = low salinity, Dry = high salinity).
        // Xác định mùa (Mưa = độ mặn thấp, Khô = độ mặn cao).
        bool isRainy = (RulesoftheGame_VU2_1.Saltwater_Intrusion < 1f);
        
        switch (fruitType)
        {
            case FruitType.Durian:
                // Durian: Best in fresh water, fails badly in salt + dry.
                // Sầu riêng: Tốt nhất ở nước ngọt, thất bại nặng ở lợ + khô.
                // Production: 20 tons/ha, each tree = 5 ha.
                // Sản lượng: 20 tấn/ha, mỗi cây = 5 ha.
                if (isFresh) return isRainy ? 100 : 80;
                else         return isRainy ? 60 : -40;
                
            case FruitType.Coconut:
                // Coconut: Tolerates salt better than durian.
                // Dừa: Chịu mặn tốt hơn sầu riêng.
                // Production: 20 tons/ha, each tree = 5 ha.
                // Sản lượng: 20 tấn/ha, mỗi cây = 5 ha.
                if (isFresh) return isRainy ? 100 : 80;
                else         return isRainy ? 60 : 50;
                
            case FruitType.Fish:
                // Fish: Prefers brackish/salt water, especially in dry season.
                // Cá: Thích nước lợ/mặn, đặc biệt trong mùa khô.
                if (isFresh) return isRainy ? 10 : 20;
                else         return isRainy ? 30 : 40;
            
            case FruitType.Shrimp:
                // Shrimp: Consistent yield regardless of conditions.
                // Tôm: Năng suất ổn định bất kể điều kiện.
                // Production: 2 tons/ha, each unit = 10 ha.
                // Sản lượng: 2 tấn/ha, mỗi con = 10 ha.
                if (isFresh) return isRainy ? 20 : 20;
                else         return isRainy ? 20 : 20;

            case FruitType.Rice:
                // Rice: Needs fresh water, fails in fresh + dry (drought).
                // Lúa: Cần nước ngọt, thất bại ở ngọt + khô (hạn hán).
                // Production: 6 tons/ha, each plant = 10 ha.
                // Sản lượng: 6 tấn/ha, mỗi cây = 10 ha.
                if (isFresh) return isRainy ? 60 : -20;
                else         return isRainy ? 40 : 20;

            default:
                return 1;
        }
    }
    
    // =========================================================================
    // CollectFruit - Executes the collection: score, sound, and removal.
    // CollectFruit - Thực hiện thu hoạch: tính điểm, âm thanh, và xóa object.
    // =========================================================================
    private void CollectFruit()
    {
        // Mark as collected to prevent double-collection.
        // Đánh dấu đã thu hoạch để tránh thu hoạch hai lần.
        _collected = true;
        
        // Calculate score from lookup table.
        // Tính điểm từ bảng tra cứu.
        int points = GetTableScore();
        
        // Add score to GameManager.
        // Cộng điểm vào GameManager.
        var gm = Thuan_23127_GameManager.Instance;
        if (gm != null)
        {
            gm.AddScore(points);
        }
        
        // Play collection sound effect.
        // Phát âm thanh thu hoạch.
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        // Get display name for debug log.
        // Lấy tên hiển thị cho debug log.
        string fruitName = fruitType switch
        {
            FruitType.Coconut => "Dừa",
            FruitType.Durian => "Sầu riêng",
            FruitType.Fish => "Cá",
            FruitType.Shrimp => "Tôm",
            FruitType.Rice => "Lúa",
            _ => "Unknown"
        };
        bool isFresh = ownerArea != null && ownerArea.waterType == WaterType.Fresh;
        bool isRainy = RulesoftheGame_VU2_1.Saltwater_Intrusion < 1f;
        
        // Log harvest result for debugging.
        // Ghi log kết quả thu hoạch để debug.
        Debug.Log($"[David_Fruit] Thu hoạch {fruitName} " +
                  $"[Vùng {(isFresh ? "Ngọt" : "Lợ")} + Mùa {(isRainy ? "Mưa" : "Khô")}] " +
                  $"+{points} điểm!");
        
        // Remove object from scene.
        // Xóa object khỏi scene.
        if (destroyOnCollect)
        {
            // Permanent removal.
            // Xóa vĩnh viễn.
            Destroy(gameObject);
        }
        else
        {
            // Temporary hide (for respawn by TreeSpawner).
            // Ẩn tạm thời (để TreeSpawner respawn).
            gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // ResetToTree - Resets fruit back to tree position (for respawning).
    // ResetToTree - Reset quả về vị trí cây (để respawn).
    // =========================================================================
    public void ResetToTree()
    {
        // Reset position and parent
        if (_originalParent != null)
        {
            transform.SetParent(_originalParent);
        }
        transform.position = _originalPosition;
        transform.rotation = _originalRotation;

        // Reset physics
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Reset state
        isOnTree = true;
        canCollect = false;
        _collected = false;
        
        gameObject.SetActive(true);
        
        Debug.Log($"[David_Fruit] {fruitType} reset về cây");
    }
}
