using UnityEngine;

/// <summary>
/// Script gắn vào prefab quả (Dừa, Sầu riêng)
/// Khi chạm vào túi (tag "Bag") sẽ cộng điểm theo bảng và biến mất
/// Điểm tính theo: Vùng (Ngọt/Lợ) × Mùa (Mưa/Khô)
/// </summary>
public enum FruitType 
{ 
    Coconut,  // Dừa
    Durian,   // Sầu riêng
    Fish      // Cá
}

public class David_Fruit : MonoBehaviour
{
    [Header("Loại quả")]
    public FruitType fruitType = FruitType.Coconut;
    
    [Header("Nguồn xác định Vùng")]
    [Tooltip("Kéo FarmArea của vùng này vào để xác định Ngọt/Lợ. Nếu để trống sẽ tự tìm.")]
    public FarmArea ownerArea;
    
    [Header("Tag của túi thu hoạch")]
    public string bagTag = "Bag";

    [Header("Cấu hình khác")]
    [Tooltip("Nếu true: Destroy object khi hái. Nếu false: Chỉ set active false (để respawn)")]
    public bool destroyOnCollect = true;

    [Header("Audio (Optional)")]
    public AudioClip collectSound;
    
    private bool _collected = false;
    
    private void Start()
    {
        // Tự động tìm FarmArea cha nếu chưa gán
        if (ownerArea == null)
        {
            ownerArea = GetComponentInParent<FarmArea>();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag(bagTag)) return;
        TryCollect();
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(bagTag)) return;
        TryCollect();
    }
    
    private void TryCollect()
    {
        if (!RulesoftheGame_VU2_1.GameActive)
        {
            Debug.Log("[David_Fruit] Game chưa bắt đầu!");
            return;
        }
        
        // Sầu riêng chỉ hái được mùa mưa (độ mặn thấp)
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
    
    /// <summary>
    /// Tính điểm theo bảng: Vùng × Mùa
    /// | Loại       | Ngọt+Mưa | Ngọt+Khô | Lợ+Mưa | Lợ+Khô |
    /// |------------|----------|----------|--------|--------|
    /// | Sầu riêng  | 15       | 10       | 6      | 4      |
    /// | Dừa        | 12       | 8        | 8      | 5      |
    /// | Cá         | 1        | 2        | 3      | 4      |
    /// </summary>
    private int GetTableScore()
    {
        bool isFresh = true; // Mặc định là vùng ngọt
        if (ownerArea != null)
        {
            isFresh = (ownerArea.waterType == WaterType.Fresh);
        }
        
        bool isRainy = (RulesoftheGame_VU2_1.Saltwater_Intrusion < 1f);
        
        switch (fruitType)
        {
            case FruitType.Durian:
                // Sầu riêng: Ngọt+Mưa=15, Ngọt+Khô=10, Lợ+Mưa=6, Lợ+Khô=4
                if (isFresh) return isRainy ? 15 : 10;
                else         return isRainy ? 6 : 4;
                
            case FruitType.Coconut:
                // Dừa: Ngọt+Mưa=12, Ngọt+Khô=8, Lợ+Mưa=8, Lợ+Khô=5
                if (isFresh) return isRainy ? 12 : 8;
                else         return isRainy ? 8 : 5;
                
            case FruitType.Fish:
                // Cá: Ngọt+Mưa=1, Ngọt+Khô=2, Lợ+Mưa=3, Lợ+Khô=4
                if (isFresh) return isRainy ? 1 : 2;
                else         return isRainy ? 3 : 4;
                
            default:
                return 1;
        }
    }
    
    private void CollectFruit()
    {
        _collected = true;
        
        // Tính điểm theo bảng
        int points = GetTableScore();
        
        var gm = Thuan_23127_GameManager.Instance;
        if (gm != null)
        {
            gm.AddScore(points);
        }
        
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        string fruitName = fruitType == FruitType.Coconut ? "Dừa" : 
                          (fruitType == FruitType.Durian ? "Sầu riêng" : "Cá");
        bool isFresh = ownerArea != null && ownerArea.waterType == WaterType.Fresh;
        bool isRainy = RulesoftheGame_VU2_1.Saltwater_Intrusion < 1f;
        
        Debug.Log($"[David_Fruit] Thu hoạch {fruitName} " +
                  $"[Vùng {(isFresh ? "Ngọt" : "Lợ")} + Mùa {(isRainy ? "Mưa" : "Khô")}] " +
                  $"+{points} điểm!");
        
        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    // Reset khi respawn
    private void OnEnable()
    {
        _collected = false;
    }
}
