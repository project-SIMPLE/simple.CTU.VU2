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
    
    [Header("Auto-Pull Grab / Grab tự động kéo")]
    [Tooltip("Bật auto-pull khi grab")]
    public bool enableAutoPull = true;
    
    [Tooltip("Thời gian kéo về tay (giây)")]
    public float pullDuration = 0.2f;
    
    // XR Grab reference
    private XRGrabInteractable _grabInteractable;
    private Transform _grabTarget;
    private Coroutine _pullCoroutine;
    private bool _isBeingPulled = false;
    
    // Store original XR settings
    private float _originalAttachEaseInTime;
    private XRBaseInteractable.MovementType _originalMovementType;
    
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
        
        // Setup XR Grab Interactable for auto-pull
        SetupAutoPull();
        
        // Auto-setup tree fruits
        if (fruitType == FruitType.Coconut || fruitType == FruitType.Durian)
        {
            // If parent has tree script, assume on tree
            if (GetComponentInParent<David_CoconutTree>() != null || 
                GetComponentInParent<David_DurianTree>() != null)
            {
                isOnTree = true;
                canCollect = false;
                
                // IMPORTANT: Ensure Rigidbody is kinematic so fruit doesn't fall!
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
    // SetupAutoPull - Subscribe to XR Grab events for auto-pull feature.
    // SetupAutoPull - Đăng ký XR Grab events cho tính năng auto-pull.
    // =========================================================================
    private void SetupAutoPull()
    {
        if (!enableAutoPull) return;
        
        _grabInteractable = GetComponent<XRGrabInteractable>();
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnGrabbed);
            _grabInteractable.selectExited.AddListener(OnReleased);
            Debug.Log($"[David_Fruit] Auto-pull enabled for {gameObject.name}");
        }
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
    // OnGrabbed - Called when grabbed, TELEPORT to hand immediately!
    // OnGrabbed - Gọi khi grab, DỊCH CHUYỂN đến tay ngay lập tức!
    // =========================================================================
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Mark as no longer on tree
        isOnTree = false;
        canCollect = true;
        
        if (!enableAutoPull) return;
        
        // Get target position from interactor
        Transform target = null;
        if (args.interactorObject is XRBaseInteractor interactor)
        {
            target = interactor.attachTransform ?? interactor.transform;
            _grabTarget = target;
        }
        else
        {
            target = args.interactorObject.transform;
            _grabTarget = target;
        }
        
        if (target != null && _grabInteractable != null)
        {
            // DISABLE XR TRACKING - We control position!
            _grabInteractable.trackPosition = false;
            _grabInteractable.trackRotation = false;
            _wasTrackingDisabled = true;
            
            // DIRECT TELEPORT - Move object to hand immediately!
            transform.position = target.position;
            transform.rotation = target.rotation;
            
            // Stop any physics movement
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Start force teleport timer - will override XR in LateUpdate
            _forceTeleportEndTime = Time.time + pullDuration;
            
            Debug.Log($"[David_Fruit] TELEPORTING {gameObject.name} for {pullDuration}s");
        }
    }
    
    // Timer for force teleport
    private float _forceTeleportEndTime = 0f;
    private bool _wasTrackingDisabled = false;
    
    // =========================================================================
    // LateUpdate - Force position to hand AFTER XR Update runs
    // LateUpdate - Đặt vị trí về tay SAU KHI XR Update chạy
    // =========================================================================
    private void LateUpdate()
    {
        // Force position during teleport window
        if (Time.time < _forceTeleportEndTime && _grabTarget != null && enableAutoPull)
        {
            transform.position = _grabTarget.position;
            transform.rotation = _grabTarget.rotation;
        }
        // Re-enable tracking when teleport window ends
        else if (_wasTrackingDisabled && _grabInteractable != null)
        {
            _grabInteractable.trackPosition = true;
            _grabInteractable.trackRotation = true;
            _wasTrackingDisabled = false;
            Debug.Log($"[David_Fruit] XR tracking restored for {gameObject.name}");
        }
    }
    
    // =========================================================================
    // OnReleased - Called when released, stops pull.
    // OnReleased - Gọi khi thả, dừng pull.
    // =========================================================================
    private void OnReleased(SelectExitEventArgs args)
    {
        _grabTarget = null;
        _forceTeleportEndTime = 0f;  // Stop force teleport
        
        // Restore tracking on release
        if (_grabInteractable != null)
        {
            _grabInteractable.trackPosition = true;
            _grabInteractable.trackRotation = true;
        }
        _wasTrackingDisabled = false;
        
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
    // OnEnable - Resets collection state when object is re-enabled (respawn).
    // OnEnable - Reset trạng thái thu hoạch khi object được bật lại (respawn).
    // =========================================================================
    private void OnEnable()
    {
        _collected = false;
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
