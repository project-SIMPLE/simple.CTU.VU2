using UnityEngine;

// =============================================================================
// CreatureType - Types of water creatures.
// CreatureType - Các loại sinh vật nước.
// =============================================================================
public enum CreatureType
{
    Fish,   // Cá
    Shrimp  // Tôm
}

// =============================================================================
// David_WaterCreature - Fish or shrimp that can be caught with net.
// David_WaterCreature - Cá hoặc tôm có thể bắt bằng vợt.
// 
// Net hits creature → Score calculated → Creature hidden → Respawn after delay
// Vợt chạm sinh vật → Tính điểm → Sinh vật ẩn → Respawn sau delay
// =============================================================================
public class David_WaterCreature : MonoBehaviour
{
    [Header("Creature Config / Cấu hình sinh vật")]
    
    [Tooltip("Loại sinh vật")]
    public CreatureType creatureType = CreatureType.Fish;
    
    [Tooltip("Điểm cơ bản khi bắt được")]
    public int baseScore = 15;
    
    [Tooltip("Có thể bắt được không?")]
    public bool canCatch = true;

    [Header("Zone Reference")]
    [Tooltip("Vùng FarmArea chứa sinh vật này")]
    public FarmArea ownerArea;

    [Header("Visual")]
    [Tooltip("GameObject hiển thị sinh vật (ẩn khi bị bắt)")]
    public GameObject creatureVisual;

    [Header("Movement (Optional)")]
    [Tooltip("Sinh vật có di chuyển không?")]
    public bool enableMovement = true;
    
    [Tooltip("Tốc độ bơi")]
    public float swimSpeed = 1f;
    
    [Tooltip("Phạm vi bơi (từ vị trí gốc)")]
    public float swimRange = 2f;

    [Header("Audio")]
    public AudioClip catchSound;

    // State
    private bool _caught = false;
    private Vector3 _originPosition;
    private Vector3 _targetPosition;
    private float _moveTimer;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Start()
    {
        // Auto-find FarmArea if not assigned
        if (ownerArea == null)
        {
            ownerArea = GetComponentInParent<FarmArea>();
        }

        // Auto-find visual if not assigned
        if (creatureVisual == null)
        {
            creatureVisual = gameObject;
        }

        // Store origin for swimming
        _originPosition = transform.position;
        PickNewTarget();
    }

    private void Update()
    {
        if (!enableMovement || _caught) return;
        
        // Simple swim movement
        MoveTowardsTarget();
    }

    // =========================================================================
    // MOVEMENT
    // =========================================================================

    private void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, 
            _targetPosition, 
            swimSpeed * Time.deltaTime
        );

        // Look at target
        Vector3 direction = _targetPosition - transform.position;
        if (direction.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 2f
            );
        }

        // Pick new target when reached
        if (Vector3.Distance(transform.position, _targetPosition) < 0.2f)
        {
            _moveTimer -= Time.deltaTime;
            if (_moveTimer <= 0)
            {
                PickNewTarget();
            }
        }
    }

    private void PickNewTarget()
    {
        // Random position within swim range
        Vector2 randomCircle = Random.insideUnitCircle * swimRange;
        _targetPosition = _originPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Random wait time at each point
        _moveTimer = Random.Range(0.5f, 2f);
    }

    // =========================================================================
    // CATCHING
    // =========================================================================

    /// <summary>
    /// Checks if this creature can be caught.
    /// Kiểm tra xem sinh vật này có thể bắt không.
    /// </summary>
    public bool CanCatch()
    {
        return canCatch && !_caught;
    }

    /// <summary>
    /// Catches this creature, calculates score.
    /// Bắt sinh vật này, tính điểm.
    /// </summary>
    public void Catch()
    {
        if (!CanCatch()) return;

        _caught = true;
        canCatch = false;

        // Calculate score using game rules
        int score = CalculateScore();

        // Add to game score
        if (RulesoftheGame_VU2_1.GameActive)
        {
            var gm = Thuan_23127_GameManager.Instance;
            if (gm != null)
            {
                gm.AddScore(score);
            }
            Debug.Log($"[David_WaterCreature] Bắt được {creatureType}! +{score} điểm");
        }

        // Play sound
        if (catchSound != null)
        {
            AudioSource.PlayClipAtPoint(catchSound, transform.position);
        }

        // Hide visual
        if (creatureVisual != null)
        {
            creatureVisual.SetActive(false);
        }
    }

    /// <summary>
    /// Calculates catch score based on zone, season, and creature type.
    /// Tính điểm bắt dựa trên vùng, mùa, và loại sinh vật.
    /// </summary>
    private int CalculateScore()
    {
        int score = baseScore;
        
        // Apply zone/season multipliers
        if (ownerArea != null)
        {
            bool isFresh = (ownerArea.waterType == WaterType.Fresh);
            
            // Fish: better in fresh water during rainy season
            if (creatureType == CreatureType.Fish)
            {
                if (isFresh && RulesoftheGame_VU2_1.Saltwater_Intrusion < 0.5f)
                {
                    score = (int)(score * 1.5f);
                }
            }
            // Shrimp: better in salt/brackish water during dry season
            else if (creatureType == CreatureType.Shrimp)
            {
                if (!isFresh && RulesoftheGame_VU2_1.Saltwater_Intrusion >= 0.5f)
                {
                    score = (int)(score * 1.8f);
                }
            }
        }

        return score;
    }

    /// <summary>
    /// Respawns this creature for catching.
    /// Respawn sinh vật này để bắt.
    /// </summary>
    public void Respawn()
    {
        _caught = false;
        canCatch = true;

        // Reset position
        transform.position = _originPosition;
        PickNewTarget();

        if (creatureVisual != null)
        {
            creatureVisual.SetActive(true);
        }

        Debug.Log($"[David_WaterCreature] Respawned: {name}");
    }
}
