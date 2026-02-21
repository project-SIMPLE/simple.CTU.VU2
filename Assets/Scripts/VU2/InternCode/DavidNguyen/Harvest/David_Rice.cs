using UnityEngine;

// =============================================================================
// David_Rice - Individual rice stem that can be harvested.
// David_Rice - Cây lúa riêng lẻ có thể thu hoạch.
// Sickle hits rice → Score calculated → Rice hidden → Respawn after delay
// When affected by salinity (dry season) → Shrinks + tilts 45° to show stress
// Khi chịu mặn (mùa khô) → Thu nhỏ + nghiêng 45° để thể hiện cây yếu
// =============================================================================
public class David_Rice : MonoBehaviour
{
    [Header("Rice Config / Cấu hình lúa")]
    
    [Tooltip("Điểm cơ bản khi thu hoạch")]
    public int baseScore = 10;
    
    [Tooltip("Icon cho lúa (hiển thị trong bảng điểm)")]
    public Sprite riceIcon;
    
    [Tooltip("Có thể thu hoạch không?")]
    public bool canHarvest = true;

    [Header("Zone Reference")]
    [Tooltip("Vùng FarmArea chứa lúa này")]
    public FarmArea ownerArea;

    [Header("Visual")]
    [Tooltip("GameObject hiển thị lúa (ẩn khi thu hoạch)")]
    public GameObject riceVisual;

    [Header("Audio")]
    public AudioClip harvestSound;

    // =========================================================================
    // SALINITY VISUAL EFFECTS / HIỆU ỨNG HÌNH ẢNH KHI CHỊU MẶN
    // =========================================================================

    [Header("Salinity Wilt Effect / Hiệu ứng héo khi mặn")]

    [Tooltip("Tỷ lệ thu nhỏ khi chịu mặn (0.6 = còn 60% kích thước gốc)\n" +
             "Scale ratio when affected by salinity")]
    [Range(0.3f, 1f)]
    public float wiltScale = 0.6f;

    [Tooltip("Góc nghiêng (độ) khi chịu mặn\n" +
             "Tilt angle (degrees) when affected by salinity")]
    [Range(0f, 90f)]
    public float wiltTiltAngle = 45f;

    [Tooltip("Tốc độ chuyển đổi hiệu ứng (cao = nhanh hơn)\n" +
             "Transition speed for wilt effect (higher = faster)")]
    [Range(0.5f, 10f)]
    public float wiltTransitionSpeed = 2f;

    // State
    private bool _harvested = false;
    
    // Salinity state / Trạng thái mặn
    private bool _isWilted = false;
    private Vector3 _initialScale;
    private Quaternion _initialRotation;
    private Vector3 _targetScale;
    private Quaternion _targetRotation;
    private bool _isTransitioning = false;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        // Cache initial transform values before any modification
        // Lưu giá trị transform ban đầu trước khi thay đổi
        _initialScale = transform.localScale;
        _initialRotation = transform.localRotation;
        _targetScale = _initialScale;
        _targetRotation = _initialRotation;
    }

    private void Start()
    {
        // Auto-find FarmArea if not assigned
        if (ownerArea == null)
        {
            ownerArea = GetComponentInParent<FarmArea>();
        }

        // Auto-find visual if not assigned
        if (riceVisual == null)
        {
            riceVisual = gameObject;
        }

        // Check current season state on start
        // Kiểm tra trạng thái mùa hiện tại khi khởi động
        CheckCurrentSeason();
    }

    private void OnEnable()
    {
        // Subscribe to season change event
        // Đăng ký lắng nghe sự kiện đổi mùa
        RulesoftheGame_VU2_1.OnPhaseChanged += OnSeasonChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        // Hủy đăng ký để tránh rò rỉ bộ nhớ
        RulesoftheGame_VU2_1.OnPhaseChanged -= OnSeasonChanged;
    }

    private void Update()
    {
        // Smoothly transition scale and rotation toward target
        // Chuyển đổi mượt scale và rotation về giá trị mục tiêu
        if (_isTransitioning)
        {
            float speed = wiltTransitionSpeed * Time.deltaTime;

            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, speed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _targetRotation, speed);

            // Stop transitioning when close enough
            // Dừng chuyển đổi khi đã đủ gần
            bool scaleReached = Vector3.Distance(transform.localScale, _targetScale) < 0.01f;
            bool rotReached = Quaternion.Angle(transform.localRotation, _targetRotation) < 0.5f;

            if (scaleReached && rotReached)
            {
                transform.localScale = _targetScale;
                transform.localRotation = _targetRotation;
                _isTransitioning = false;
            }
        }
    }

    // =========================================================================
    // SALINITY RESPONSE / PHẢN ỨNG VỚI ĐỘ MẶN
    // =========================================================================

    /// <summary>
    /// Check current season and apply wilt if needed.
    /// Kiểm tra mùa hiện tại và áp hiệu ứng héo nếu cần.
    /// </summary>
    private void CheckCurrentSeason()
    {
        bool isDry = RulesoftheGame_VU2_1.Saltwater_Intrusion >= 1f;

        if (isDry && !_isWilted)
            ApplyWilt();
        else if (!isDry && _isWilted)
            ClearWilt();
    }

    /// <summary>
    /// Called when season changes via OnPhaseChanged event.
    /// Được gọi khi mùa thay đổi qua event OnPhaseChanged.
    /// </summary>
    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        bool isDry = (newPhase == SeasonPhase.Dry);

        if (isDry && !_isWilted)
        {
            ApplyWilt();
        }
        else if (!isDry && _isWilted)
        {
            ClearWilt();
        }
    }

    /// <summary>
    /// Apply wilt effect: shrink + tilt 45° to simulate salinity stress.
    /// Áp hiệu ứng héo: thu nhỏ + nghiêng 45° mô phỏng cây chịu mặn.
    /// </summary>
    public void ApplyWilt()
    {
        if (_isWilted) return;
        _isWilted = true;

        // Target: shrink to wiltScale and tilt by wiltTiltAngle degrees on Z axis
        // Mục tiêu: thu nhỏ theo wiltScale và nghiêng wiltTiltAngle độ trên trục Z
        _targetScale = _initialScale * wiltScale;
        _targetRotation = _initialRotation * Quaternion.Euler(0f, 0f, wiltTiltAngle);
        _isTransitioning = true;
    }

    /// <summary>
    /// Clear wilt effect: restore original scale and rotation.
    /// Xóa hiệu ứng héo: khôi phục scale và rotation gốc.
    /// </summary>
    public void ClearWilt()
    {
        if (!_isWilted) return;
        _isWilted = false;

        // Restore original transform values
        // Khôi phục giá trị transform gốc
        _targetScale = _initialScale;
        _targetRotation = _initialRotation;
        _isTransitioning = true;
    }

    /// <summary>
    /// Whether the rice is currently wilted from salinity.
    /// Lúa có đang bị héo do mặn không.
    /// </summary>
    public bool IsWilted => _isWilted;

    // =========================================================================
    // HARVESTING
    // =========================================================================

    /// <summary>
    /// Checks if this rice can be harvested.
    /// Kiểm tra xem lúa này có thể thu hoạch không.
    /// </summary>
    public bool CanHarvest()
    {
        return canHarvest && !_harvested;
    }

    /// <summary>
    /// Harvests this rice, calculates score.
    /// Thu hoạch lúa này, tính điểm.
    /// </summary>
    public void Harvest()
    {
        if (!CanHarvest()) return;

        _harvested = true;
        canHarvest = false;

        // Calculate score using game rules
        int score = CalculateScore();
        
        // Track in SeasonalSummary
        var summary = Thuan_23127_SeasonalSummary.Instance;
        if (summary != null && riceIcon != null)
        {
            summary.TrackDirect("Rice", riceIcon, score);
        }

        // Add to game score
        if (RulesoftheGame_VU2_1.GameActive)
        {
            var gm = Thuan_23127_GameManager.Instance;
            if (gm != null)
            {
                gm.AddScore(score);
            }
        }

        // Play sound
        if (harvestSound != null)
        {
            AudioSource.PlayClipAtPoint(harvestSound, transform.position);
        }

        // Hide visual
        if (riceVisual != null)
        {
            riceVisual.SetActive(false);
        }
    }

    /// <summary>
    /// Calculates harvest score based on zone and season.
    /// Tính điểm thu hoạch dựa trên vùng và mùa.
    /// </summary>
    private int CalculateScore()
    {
        int score = baseScore;
        
        // Apply zone multiplier
        if (ownerArea != null)
        {
            bool isFresh = (ownerArea.waterType == WaterType.Fresh);
            
            // Fresh water zone bonus in rainy season
            if (isFresh && RulesoftheGame_VU2_1.Saltwater_Intrusion < 0.5f)
            {
                score = (int)(score * 1.5f);
            }
            // Salt water zone bonus in dry season
            else if (!isFresh && RulesoftheGame_VU2_1.Saltwater_Intrusion >= 0.5f)
            {
                score = (int)(score * 1.3f);
            }
        }

        return score;
    }

    /// <summary>
    /// Respawns this rice for harvesting.
    /// Respawn lúa này để thu hoạch.
    /// Also restores wilt state based on current season.
    /// Đồng thời khôi phục trạng thái héo theo mùa hiện tại.
    /// </summary>
    public void Respawn()
    {
        _harvested = false;
        canHarvest = true;

        if (riceVisual != null)
        {
            riceVisual.SetActive(true);
        }

        // Re-check season after respawn
        // Kiểm tra lại mùa sau khi respawn
        CheckCurrentSeason();
    }

    // Context menu for testing in Editor
    // Menu ngữ cảnh để test trong Editor
    [ContextMenu("Test: Apply Wilt")]
    private void TestApplyWilt() => ApplyWilt();

    [ContextMenu("Test: Clear Wilt")]
    private void TestClearWilt() => ClearWilt();
}
