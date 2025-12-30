using UnityEngine;

/// <summary>
/// Script gắn vào prefab trứng
/// Khi chạm vào túi (tag "Bag") sẽ cộng 3 điểm và biến mất
/// Trứng có thể nhặt bất kỳ mùa nào
/// </summary>
public class David_Egg : MonoBehaviour
{
    [Header("Điểm số")]
    [Tooltip("Mặc định: 3 điểm")]
    public int pointValue = 3;
    
    [Header("Tag của túi thu hoạch")]
    public string bagTag = "Bag";
    
    [Header("Audio (Optional)")]
    public AudioClip collectSound;
    
    /// <summary>
    /// Xử lý khi trigger collider chạm nhau (Is Trigger = true)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(bagTag)) return;
        
        CollectEgg();
    }
    
    /// <summary>
    /// Xử lý khi collider va chạm (Is Trigger = false)
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(bagTag)) return;
        
        CollectEgg();
    }
    
    /// <summary>
    /// Nhặt trứng - cộng điểm và hủy object
    /// </summary>
    private void CollectEgg()
    {
        // Kiểm tra game có đang chạy không
        if (!RulesoftheGame_VU2_1.GameActive)
        {
            Debug.Log("[David_Egg] Game chưa bắt đầu!");
            return;
        }
        
        // Cộng điểm vào GameManager
        var gm = Thuan_23127_GameManager.Instance;
        if (gm != null)
        {
            gm.AddScore(pointValue);
        }
        
        // Phát âm thanh (nếu có)
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        Debug.Log($"✅ [David_Egg] Nhặt trứng +{pointValue} điểm!");
        
        // Hủy trứng
        Destroy(gameObject);
    }
}
