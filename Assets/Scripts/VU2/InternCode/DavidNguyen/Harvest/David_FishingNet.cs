using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// David_FishingNet - Fishing net tool for catching fish/shrimp.
// David_FishingNet - Vợt dùng để bắt cá/tôm.
// 
// Player grabs net → Scoops in water → Fish/Shrimp caught → Score added
// Player cầm vợt → Vớt trong nước → Cá/Tôm bị bắt → Cộng điểm
// =============================================================================
[RequireComponent(typeof(Rigidbody))]
public class David_FishingNet : MonoBehaviour
{
    [Header("Net Config / Cấu hình vợt")]
    
    [Tooltip("Tốc độ tối thiểu để bắt được (m/s)")]
    public float minCatchSpeed = 0.5f;
    
    [Tooltip("Tags của sinh vật có thể bắt")]
    public string[] creatureTags = { "Fish", "Shrimp" };

    [Header("Audio")]
    public AudioClip catchSound;
    public AudioClip scoopSound;

    [Header("VFX")]
    public GameObject catchVFX;
    public GameObject waterSplashVFX;

    // References
    private David_FishPond _pond;
    private Rigidbody _rb;
    private XRGrabInteractable _grabInteractable;
    private Vector3 _lastPosition;
    private float _currentSpeed;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void Start()
    {
        _lastPosition = transform.position;
    }

    private void Update()
    {
        // Calculate scoop speed
        _currentSpeed = (transform.position - _lastPosition).magnitude / Time.deltaTime;
        _lastPosition = transform.position;
    }

    // =========================================================================
    // COLLISION DETECTION
    // =========================================================================

    private void OnTriggerEnter(Collider other)
    {
        TryCatchCreature(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCatchCreature(collision.gameObject);
    }

    /// <summary>
    /// Attempts to catch creature if conditions are met.
    /// Cố gắng bắt sinh vật nếu đủ điều kiện.
    /// </summary>
    private void TryCatchCreature(GameObject target)
    {
        // Check if target has creature tag
        bool hasValidTag = false;
        foreach (var tag in creatureTags)
        {
            if (target.CompareTag(tag))
            {
                hasValidTag = true;
                break;
            }
        }

        // Also check for component
        var creature = target.GetComponent<David_WaterCreature>();
        if (creature == null)
        {
            creature = target.GetComponentInParent<David_WaterCreature>();
        }

        if (creature == null && !hasValidTag) return;

        if (creature != null)
        {
            CatchCreature(creature);
        }
    }

    /// <summary>
    /// Catches the creature.
    /// Bắt sinh vật.
    /// </summary>
    private void CatchCreature(David_WaterCreature creature)
    {
        // Check speed threshold (can be lower than sickle since scooping is gentler)
        if (_currentSpeed < minCatchSpeed)
        {
            Debug.Log($"[David_FishingNet] Vớt quá chậm ({_currentSpeed:F2} < {minCatchSpeed})");
            return;
        }

        // Check if creature can be caught
        if (!creature.CanCatch())
        {
            return;
        }

        // Notify pond if assigned
        if (_pond != null)
        {
            _pond.OnCreatureCaught(creature);
        }
        else
        {
            // Direct catch
            creature.Catch();
        }

        // Play effects
        PlayCatchEffects(creature.transform.position);

        Debug.Log($"[David_FishingNet] Bắt được: {creature.name} (speed: {_currentSpeed:F2})");
    }

    // =========================================================================
    // EFFECTS
    // =========================================================================

    private void PlayCatchEffects(Vector3 position)
    {
        // Sound
        if (catchSound != null)
        {
            AudioSource.PlayClipAtPoint(catchSound, position);
        }

        // VFX
        if (catchVFX != null)
        {
            var vfx = Instantiate(catchVFX, position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // Water splash
        if (waterSplashVFX != null)
        {
            var splash = Instantiate(waterSplashVFX, position, Quaternion.identity);
            Destroy(splash, 2f);
        }
    }

    // =========================================================================
    // PUBLIC METHODS
    // =========================================================================

    /// <summary>
    /// Sets the pond this net belongs to.
    /// Đặt ao mà vợt này thuộc về.
    /// </summary>
    public void SetPond(David_FishPond pond)
    {
        _pond = pond;
    }

    /// <summary>
    /// Gets current scoop speed.
    /// Lấy tốc độ vớt hiện tại.
    /// </summary>
    public float GetScoopSpeed()
    {
        return _currentSpeed;
    }
}
