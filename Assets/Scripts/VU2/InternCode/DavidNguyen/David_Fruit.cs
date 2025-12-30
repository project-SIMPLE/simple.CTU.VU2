using UnityEngine;

/// <summary>
/// Script gắn vào prefab quả (Dừa, Sầu riêng)
/// Khi chạm vào túi (tag "Bag") sẽ cộng điểm và biến mất
/// Sầu riêng chỉ hái được trong mùa khô (Saltwater_Intrusion >= 1)
/// </summary>
public enum FruitType 
{ 
    Coconut,  // Dừa - luôn hái được
    Durian    // Sầu riêng - chỉ hái được mùa khô
}

public class David_Fruit : MonoBehaviour
{
    [Header("Loại quả")]
    [Tooltip("Coconut = Dừa (2đ), Durian = Sầu riêng (5đ)")]
    public FruitType fruitType = FruitType.Coconut;
    
    [Header("Điểm số")]
    [Tooltip("Dừa: 2, Sầu riêng: 5")]
    public int pointValue = 2;
    
    [Header("Tag của túi thu hoạch")]
    public string bagTag = "Bag";

    [Header("Audio (Optional)")]
    public AudioClip collectSound;
    
    // Cờ tránh collect nhiều lần
    private bool _collected = false;
    private void Start()
    {
        Debug.Log($"[David_Fruit] Script active on {gameObject.name}, Type={fruitType}, BagTag={bagTag}");
    }
    
    /// <summary>
    /// Xử lý khi trigger collider chạm nhau (Is Trigger = true)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;  // Đã collect rồi, bỏ qua
        if (!other.CompareTag(bagTag)) return;
        TryCollect();
    }
    
    /// <summary>
    /// Xử lý khi collider va chạm (Is Trigger = false)
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(bagTag)) return;
        Debug.Log("[David_Fruit] Collision Enter!");
        TryCollect();
    }
    
    /// <summary>
    /// Kiểm tra điều kiện mùa và thu hoạch
    /// </summary>
    private void TryCollect()
    {
        // Kiểm tra game có đang chạy không
        if (!RulesoftheGame_VU2_1.GameActive)
        {
            Debug.Log("[David_Fruit] Game chưa bắt đầu!");
            return;
        }
        
        // Kiểm tra điều kiện mùa cho Sầu riêng
        if (fruitType == FruitType.Durian)
        {
            // Sầu riêng chỉ hái được khi MÙA MƯA (Saltwater_Intrusion = 0, độ mặn thấp)
            // Mùa khô (Saltwater_Intrusion >= 1, độ mặn cao) → cây héo, KHÔNG hái được
            if (RulesoftheGame_VU2_1.Saltwater_Intrusion >= 1f)
            {
                Debug.Log("[David_Fruit] Cây sầu riêng bị héo do độ mặn cao - không thể thu hoạch trong mùa khô!");
                return;
            }
        }
        
        // Thu hoạch thành công
        CollectFruit();
    }
    
    /// <summary>
    /// Thu hoạch quả - cộng điểm và hủy object
    /// </summary>
    private void CollectFruit()
    {
        // Đánh dấu đã collect để tránh gọi nhiều lần
        _collected = true;
        
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
        
        string fruitName = fruitType == FruitType.Coconut ? "Dừa" : "Sầu riêng";
        Debug.Log($"[David_Fruit] Thu hoạch {fruitName} +{pointValue} điểm!");
        
        // Hủy quả
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Thiết lập nhanh cho prefab
    /// </summary>
    public void SetupAsCoconut()
    {
        fruitType = FruitType.Coconut;
        pointValue = 2;
    }
    
    public void SetupAsDurian()
    {
        fruitType = FruitType.Durian;
        pointValue = 5;
    }
}
