using System;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// ProductKind - Categorizes entities for summary tracking.
// ProductKind - Phân loại thực thể để theo dõi tổng kết.
// =============================================================================
public enum ProductKind { Plant, Animal, Fish }

// =============================================================================
// SeasonalCounters - Stores harvest counts and scores per season.
// SeasonalCounters - Lưu trữ số lần thu hoạch và điểm theo mùa.
// =============================================================================
[Serializable]
public class SeasonalCounters
{
    // Icon for this product type.
    // Icon cho loại sản phẩm này.
    public Sprite icon;
    
    // Harvest count per season: [0]=Rainy, [1]=Dry.
    // Số lần thu hoạch theo mùa: [0]=Mưa, [1]=Khô.
    public int[] count = new int[2];
    
    // Total score earned per season: [0]=Rainy, [1]=Dry.
    // Tổng điểm kiếm được theo mùa: [0]=Mưa, [1]=Khô.
    public int[] score = new int[2];
}

// =============================================================================
// Thuan_23127_SeasonalSummary - Tracks and summarizes all harvests by product type.
// Thuan_23127_SeasonalSummary - Theo dõi và tổng kết tất cả thu hoạch theo loại sản phẩm.
// 
// This singleton component provides statistics for the end-game summary screen:
// - How many of each product type was harvested
// - Total score earned per product type per season
// 
// Component singleton này cung cấp thống kê cho màn hình tổng kết cuối game:
// - Số lượng mỗi loại sản phẩm đã thu hoạch
// - Tổng điểm kiếm được theo loại sản phẩm theo mùa
// 
// Usage:
// 1. FarmArea.PlantInternal() calls Track() when planting
// 2. Track() subscribes to OnHarvested event
// 3. When harvested, counters are updated
// 4. UI calls GetAllScores() to display summary table
// =============================================================================
public class Thuan_23127_SeasonalSummary : MonoBehaviour
{
    // =========================================================================
    // SINGLETON INSTANCE
    // INSTANCE SINGLETON
    // =========================================================================
    public static Thuan_23127_SeasonalSummary Instance;

    // =========================================================================
    // DATA STORAGE
    // LƯU TRỮ DỮ LIỆU
    // =========================================================================
    
    // Dictionary mapping "ProductKind:ID" to counters.
    // Dictionary map "ProductKind:ID" sang bộ đếm.
    private readonly Dictionary<string, SeasonalCounters> _map = new();
    
    // Event fired when data changes (for UI refresh).
    // Sự kiện được bắn khi dữ liệu thay đổi (để refresh UI).
    public event Action OnChanged;

    // =========================================================================
    // Awake - Singleton setup.
    // Awake - Thiết lập Singleton.
    // =========================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // =========================================================================
    // CurrentPhase - Determines current season from saltwater intrusion value.
    // CurrentPhase - Xác định mùa hiện tại từ giá trị xâm nhập mặn.
    // 
    // Returns: Rainy1 (intrusion=0) or Dry (intrusion=1).
    // Trả về: Rainy1 (intrusion=0) hoặc Dry (intrusion=1).
    // =========================================================================
    private static SeasonPhase CurrentPhase()
    {
        var s = RulesoftheGame_VU2_1.Saltwater_Intrusion;
        return Mathf.Approximately(s, 1f) ? SeasonPhase.Dry : SeasonPhase.Rainy1;
    }

    // =========================================================================
    // Key - Generates unique key for product lookup.
    // Key - Tạo key duy nhất để tra cứu sản phẩm.
    // =========================================================================
    private static string Key(ProductKind kind, int id) => $"{kind}:{id}";

    // =========================================================================
    // GetOrCreate - Gets existing counters or creates new ones.
    // GetOrCreate - Lấy bộ đếm hiện có hoặc tạo mới.
    // =========================================================================
    private SeasonalCounters GetOrCreate(ProductKind kind, int id, Sprite icon)
    {
        var key = Key(kind, id);
        if (!_map.TryGetValue(key, out var c))
        {
            c = new SeasonalCounters { icon = icon };
            _map[key] = c;
        }
        // Update icon if not set yet.
        // Cập nhật icon nếu chưa có.
        if (c.icon == null && icon != null) c.icon = icon;
        return c;
    }

    // =========================================================================
    // Track - Registers a plant/animal/fish for harvest tracking.
    // Track - Đăng ký một cây/động vật/cá để theo dõi thu hoạch.
    // 
    // Called by: FarmArea.PlantInternal() when planting.
    // Được gọi bởi: FarmArea.PlantInternal() khi trồng.
    // 
    // When the entity is harvested:
    // - Increment count for current season
    // - Add points to season total
    // - Fire OnChanged event for UI refresh
    // 
    // Khi thực thể được thu hoạch:
    // - Tăng số đếm cho mùa hiện tại
    // - Cộng điểm vào tổng mùa
    // - Bắn sự kiện OnChanged để refresh UI
    // =========================================================================
    public void Track(Thuan_23127_PlantGrowth growth, Thuan_23127_SeedTag tag)
    {
        if (!growth || !tag) return;

        // Determine product kind and ID from seed tag.
        // Xác định loại sản phẩm và ID từ seed tag.
        ProductKind kind;
        int id;
        if      (tag.plantId  > 0) { kind = ProductKind.Plant;  id = tag.plantId;  }
        else if (tag.animalId > 0) { kind = ProductKind.Animal; id = tag.animalId; }
        else if (tag.fishId   > 0) { kind = ProductKind.Fish;   id = tag.fishId;   }
        else return;

        // Subscribe to harvest event.
        // Đăng ký sự kiện thu hoạch.
        growth.OnHarvested += points =>
        {
            var p = CurrentPhase();  // 0=Rainy1, 1=Dry
            var st = GetOrCreate(kind, id, tag.hudIcon);
            st.count[(int)p] += 1;
            st.score[(int)p] += points;
            OnChanged?.Invoke();
        };

        // No need to explicitly unsubscribe - object will be GC'd with its events.
        // Không cần hủy đăng ký - object sẽ được GC cùng với events của nó.
        growth.OnAboutToDestroy += () => { /* no-op */ };
    }

    // =========================================================================
    // GetAllCounts - Returns harvest counts for all product types.
    // GetAllCounts - Trả về số lần thu hoạch cho tất cả loại sản phẩm.
    // 
    // Used by: UI to display summary table.
    // Được dùng bởi: UI để hiển thị bảng tổng kết.
    // =========================================================================
    public List<(Sprite icon, int rainy1, int dry, int rainy2)> GetAllCounts()
    {
        var list = new List<(Sprite,int,int,int)>();
        foreach (var kv in _map.Values)
            list.Add((kv.icon, kv.count[0], kv.count[1], kv.count[2]));
        return list;
    }

    // =========================================================================
    // GetAllScores - Returns total scores for all product types.
    // GetAllScores - Trả về tổng điểm cho tất cả loại sản phẩm.
    // 
    // Returns: List of (icon, rainyScore, dryScore) tuples.
    // Trả về: Danh sách các tuple (icon, điểm mưa, điểm khô).
    // =========================================================================
    public List<(Sprite icon, int rainy, int dry)> GetAllScores()
    {
        var list = new List<(Sprite, int, int)>();
        foreach (var kv in _map.Values)
            list.Add((kv.icon, kv.score[0], kv.score[1]));
        return list;
    }
    
    // =========================================================================
    // ResetAllData - Clears all tracking data.
    // ResetAllData - Xóa tất cả dữ liệu theo dõi.
    // 
    // Called by: Game restart to clear statistics.
    // Được gọi bởi: Restart game để xóa thống kê.
    // =========================================================================
    public void ResetAllData()
    {
        _map.Clear();
        
        OnChanged?.Invoke();
        
        Debug.Log("SeasonalSummary: All data has been reset");
    }

}
