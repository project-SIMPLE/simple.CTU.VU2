using UnityEngine;

// =============================================================================
// David_Rice - Individual rice stem that can be harvested.
// David_Rice - Cây lúa riêng lẻ có thể thu hoạch.
// Sickle hits rice → Score calculated → Rice hidden → Respawn after delay
// =============================================================================
public class David_Rice : MonoBehaviour
{
    [Header("Rice Config / Cấu hình lúa")]
    
    [Tooltip("Điểm cơ bản khi thu hoạch")]
    public int baseScore = 10;
    
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

    // State
    private bool _harvested = false;

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
        if (riceVisual == null)
        {
            riceVisual = gameObject;
        }
    }

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

        // Add to game score
        if (RulesoftheGame_VU2_1.GameActive)
        {
            var gm = Thuan_23127_GameManager.Instance;
            if (gm != null)
            {
                gm.AddScore(score);
            }
            Debug.Log($"[David_Rice] Thu hoạch! +{score} điểm");
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
    /// </summary>
    public void Respawn()
    {
        _harvested = false;
        canHarvest = true;

        if (riceVisual != null)
        {
            riceVisual.SetActive(true);
        }

        Debug.Log($"[David_Rice] Respawned: {name}");
    }
}
