using UnityEngine;

// =============================================================================
// David_Egg - Simple collectible egg that awards points when collected.
// David_Egg - Trứng có thể thu hoạch đơn giản, cộng điểm khi được nhặt.
// 
// This script is attached to egg prefabs. Unlike fruits, eggs:
// - Award a fixed point value (default 3)
// - Can be collected in any season
// - Have no zone or salinity restrictions
// 
// Script này được gắn vào prefab trứng. Khác với trái cây, trứng:
// - Cộng điểm cố định (mặc định 3)
// - Có thể thu hoạch bất kỳ mùa nào
// - Không có hạn chế vùng hoặc độ mặn
// =============================================================================
public class David_Egg : MonoBehaviour
{
    // =========================================================================
    // SCORING
    // ĐIỂM SỐ
    // =========================================================================
    [Header("Score / Điểm số")]
    [Tooltip("Points awarded when collected (default: 3)")]
    // Fixed point value for this egg.
    // Giá trị điểm cố định cho trứng này.
    public int pointValue = 3;
    
    // =========================================================================
    // COLLECTION DETECTION
    // PHÁT HIỆN THU HOẠCH
    // =========================================================================
    [Header("Harvest Bag Tag / Tag túi thu hoạch")]
    // Tag of the player's collection bag.
    // Tag của túi thu hoạch của người chơi.
    public string bagTag = "Bag";
    
    // =========================================================================
    // AUDIO
    // ÂM THANH
    // =========================================================================
    [Header("Audio (Optional)")]
    // Sound effect played when collected.
    // Hiệu ứng âm thanh khi được nhặt.
    public AudioClip collectSound;
    
    // =========================================================================
    // OnTriggerEnter - Detects collection via trigger collider.
    // OnTriggerEnter - Phát hiện thu hoạch qua trigger collider.
    // 
    // Used when the egg's collider has "Is Trigger" checked.
    // Dùng khi collider của trứng đã check "Is Trigger".
    // =========================================================================
    private void OnTriggerEnter(Collider other)
    {
        // Only react to objects with bag tag.
        // Chỉ phản ứng với object có tag túi.
        if (!other.CompareTag(bagTag)) return;
        
        CollectEgg();
    }
    
    // =========================================================================
    // OnCollisionEnter - Detects collection via physics collision.
    // OnCollisionEnter - Phát hiện thu hoạch qua va chạm vật lý.
    // 
    // Used when the egg's collider has "Is Trigger" unchecked.
    // Dùng khi collider của trứng không check "Is Trigger".
    // =========================================================================
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(bagTag)) return;
        
        CollectEgg();
    }
    
    // =========================================================================
    // CollectEgg - Processes egg collection: adds score and destroys object.
    // CollectEgg - Xử lý thu hoạch trứng: cộng điểm và hủy object.
    // =========================================================================
    private void CollectEgg()
    {
        // Only allow collection when game is active.
        // Chỉ cho phép thu hoạch khi game đang hoạt động.
        if (!RulesoftheGame_VU2_1.GameActive)
        {
            Debug.Log("[David_Egg] Game chưa bắt đầu!");
            return;
        }
        
        // Add score to GameManager.
        // Cộng điểm vào GameManager.
        var gm = Thuan_23127_GameManager.Instance;
        if (gm != null)
        {
            gm.AddScore(pointValue);
        }
        
        // Play collection sound effect.
        // Phát âm thanh thu hoạch.
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        Debug.Log($"✅ [David_Egg] Collected egg +{pointValue} points!");
        
        // Destroy the egg object.
        // Hủy object trứng.
        Destroy(gameObject);
    }
}
