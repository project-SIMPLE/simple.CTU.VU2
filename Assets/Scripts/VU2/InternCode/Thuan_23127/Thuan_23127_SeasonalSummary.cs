using System;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// ProductKind - Categorizes entities for summary tracking.
// ProductKind - Phân loại thực thể để theo dõi tổng kết.
// =============================================================================
public enum ProductKind { Plant, Animal, Fish }

// =============================================================================
// SeasonalCounters - Stores harvest counts and scores per phase (3 phases).
// SeasonalCounters - Lưu trữ số lần thu hoạch và điểm theo giai đoạn (3 GĐ).
//
// Phase 0 = GĐ1 (T11–T1), Phase 1 = GĐ2 (T2–T3), Phase 2 = GĐ3 (T4)
// =============================================================================
[Serializable]
public class SeasonalCounters
{
    // Icon for this product type.
    // Icon cho loại sản phẩm này.
    public Sprite icon;
    
    // Harvest count per phase: [0]=GĐ1, [1]=GĐ2, [2]=GĐ3.
    // Số lần thu hoạch theo giai đoạn: [0]=GĐ1, [1]=GĐ2, [2]=GĐ3.
    public int[] count = new int[3];
    
    // Total score earned per phase: [0]=GĐ1, [1]=GĐ2, [2]=GĐ3.
    // Tổng điểm kiếm được theo giai đoạn: [0]=GĐ1, [1]=GĐ2, [2]=GĐ3.
    public int[] score = new int[3];
}

// =============================================================================
// Thuan_23127_SeasonalSummary - Tracks and summarizes all harvests by product type.
// Thuan_23127_SeasonalSummary - Theo dõi và tổng kết tất cả thu hoạch theo loại SP.
// 
// 3-phase system:
//   Phase 0 (GĐ1): T11–T1 (MonthIndex 1–3)
//   Phase 1 (GĐ2): T2–T3  (MonthIndex 4–5)
//   Phase 2 (GĐ3): T4     (MonthIndex 6)
//
// Hệ thống 3 giai đoạn:
//   GĐ 0 (GĐ1): T11–T1 (MonthIndex 1–3)
//   GĐ 1 (GĐ2): T2–T3  (MonthIndex 4–5)
//   GĐ 2 (GĐ3): T4     (MonthIndex 6)
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
    // CurrentPhaseIndex - Returns current phase index (0, 1, or 2).
    // CurrentPhaseIndex - Trả về index giai đoạn hiện tại (0, 1, hoặc 2).
    //
    // Based on CurrentMonthIndex from RulesoftheGame_VU2_1:
    //   MonthIndex 1–3 → Phase 0 (GĐ1: T11–T1)
    //   MonthIndex 4–5 → Phase 1 (GĐ2: T2–T3)
    //   MonthIndex 6   → Phase 2 (GĐ3: T4)
    //
    // Dựa trên CurrentMonthIndex từ RulesoftheGame_VU2_1:
    //   MonthIndex 1–3 → Phase 0 (GĐ1: T11–T1)
    //   MonthIndex 4–5 → Phase 1 (GĐ2: T2–T3)
    //   MonthIndex 6   → Phase 2 (GĐ3: T4)
    // =========================================================================
    private static int CurrentPhaseIndex()
    {
        int month = RulesoftheGame_VU2_1.CurrentMonthIndex;
        if (month <= 3) return 0;  // GĐ1: T11, T12, T1
        if (month <= 5) return 1;  // GĐ2: T2, T3
        return 2;                   // GĐ3: T4
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
    // - Determine current phase from CurrentMonthIndex
    // - Increment count for that phase
    // - Add points to phase total
    // - Fire OnChanged event for UI refresh
    // 
    // Khi thực thể được thu hoạch:
    // - Xác định giai đoạn hiện tại từ CurrentMonthIndex
    // - Tăng số đếm cho giai đoạn đó
    // - Cộng điểm vào tổng giai đoạn
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
            int phase = CurrentPhaseIndex();  // 0=GĐ1, 1=GĐ2, 2=GĐ3
            var st = GetOrCreate(kind, id, tag.hudIcon);
            st.count[phase] += 1;
            st.score[phase] += points;
            OnChanged?.Invoke();
        };

        // No need to explicitly unsubscribe - object will be GC'd with its events.
        // Không cần hủy đăng ký - object sẽ được GC cùng với events của nó.
        growth.OnAboutToDestroy += () => { /* no-op */ };
    }
    
    // =========================================================================
    // TrackDirect - Direct tracking for grab-based items (Egg, Shrimp, Fruit).
    // TrackDirect - Theo dõi trực tiếp cho items dùng grab (Trứng, Tôm, Quả).
    // 
    // Called by: David_EggGrab, David_ShrimpGrab, David_Fruit when collecting.
    // Được gọi bởi: David_EggGrab, David_ShrimpGrab, David_Fruit khi thu hoạch.
    // 
    // This bypasses PlantGrowth and directly records points.
    // Điều này bỏ qua PlantGrowth và ghi nhận điểm trực tiếp.
    // =========================================================================
    public void TrackDirect(string productName, Sprite icon, int points)
    {
        // Use product name as unique key (e.g., "Egg", "Shrimp", "Coconut")
        var key = $"Direct:{productName}";
        
        int phase = CurrentPhaseIndex(); // 0=GĐ1, 1=GĐ2, 2=GĐ3
        var counters = GetOrCreate_Direct(key, icon);
        
        counters.score[phase] += points;
        counters.count[phase] += 1;
        
        OnChanged?.Invoke();
    }
    
    private SeasonalCounters GetOrCreate_Direct(string key, Sprite icon)
    {
        if (!_map.TryGetValue(key, out var c))
        {
            c = new SeasonalCounters { icon = icon };
            _map[key] = c;
        }
        if (c.icon == null && icon != null) c.icon = icon;
        return c;
    }

    // =========================================================================
    // GetAllPhaseData - Returns full 3-phase data for all product types.
    // GetAllPhaseData - Trả về dữ liệu đầy đủ 3 giai đoạn cho tất cả SP.
    // 
    // Returns: List of tuples:
    //   (key, icon, score[3], count[3])
    //   - key    = product identifier (e.g., "Direct:Durian", "Plant:1")
    //   - score[i] = total score in phase i
    //   - count[i] = harvest count in phase i (area = count × 10)
    // 
    // Trả về: Danh sách tuple:
    //   (key, icon, score[3], count[3])
    //   - key    = định danh sản phẩm (ví dụ: "Direct:Durian", "Plant:1")
    //   - score[i] = tổng điểm ở giai đoạn i
    //   - count[i] = số lần thu hoạch ở giai đoạn i (diện tích = count × 10)
    // =========================================================================
    public List<(string key, Sprite icon, int[] scores, int[] counts)> GetAllPhaseData()
    {
        var list = new List<(string, Sprite, int[], int[])>();
        foreach (var kv in _map)
        {
            var c = kv.Value;
            // Clone arrays to prevent external mutation.
            // Clone mảng để tránh thay đổi từ bên ngoài.
            int[] scores = { c.score[0], c.score[1], c.score[2] };
            int[] counts = { c.count[0], c.count[1], c.count[2] };
            list.Add((kv.Key, c.icon, scores, counts));
        }
        return list;
    }

    // =========================================================================
    // GetAllScores - LEGACY wrapper, returns 2-season data for compatibility.
    // GetAllScores - Wrapper CŨ, trả về dữ liệu 2 mùa tương thích.
    // =========================================================================
    public List<(Sprite icon, int rainy, int dry)> GetAllScores()
    {
        var list = new List<(Sprite, int, int)>();
        foreach (var kv in _map.Values)
            list.Add((kv.icon, kv.score[0], kv.score[1] + kv.score[2]));
        return list;
    }

    // =========================================================================
    // GetAllCounts - Returns harvest counts for all product types (3 phases).
    // GetAllCounts - Trả về số lần thu hoạch (3 giai đoạn).
    // =========================================================================
    public List<(Sprite icon, int phase1, int phase2, int phase3)> GetAllCounts()
    {
        var list = new List<(Sprite, int, int, int)>();
        foreach (var kv in _map.Values)
            list.Add((kv.icon, kv.count[0], kv.count[1], kv.count[2]));
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
