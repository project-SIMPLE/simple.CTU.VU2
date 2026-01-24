using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// Thuan_23127_TotalBoard - Displays the end-game summary table.
// Thuan_23127_TotalBoard - Hiển thị bảng tổng kết cuối game.
// 
// This UI component shows a scrollable list of all products harvested,
// with their icons and scores per season (Rainy/Dry).
// 
// Component UI này hiển thị danh sách có thể cuộn của tất cả sản phẩm đã thu hoạch,
// với icon và điểm theo mùa (Mưa/Khô).
// 
// Rebuilds automatically when SeasonalSummary.OnChanged fires.
// Tự động rebuild khi SeasonalSummary.OnChanged được bắn.
// =============================================================================
public class Thuan_23127_TotalBoard : MonoBehaviour
{
    // =========================================================================
    // UI REFERENCES
    // THAM CHIẾU UI
    // =========================================================================
    [Header("Scroll content & row prefab")]
    // Parent transform for row instances.
    // Transform cha cho các instance row.
    public Transform content;
    
    // Prefab for each product row.
    // Prefab cho mỗi row sản phẩm.
    public UI_ProductRow rowPrefab;
    
    // =========================================================================
    // INTERNAL STATE
    // TRẠNG THÁI NỘI BỘ
    // =========================================================================
    
    // If true, prevents rebuilding (used during transitions).
    // Nếu true, ngăn rebuild (dùng trong các chuyển đổi).
    private bool _frozen = false;
    
    // Pool of instantiated row objects.
    // Pool các object row đã instantiate.
    private readonly List<UI_ProductRow> _pool = new();
    
    // =========================================================================
    // OnEnable - Subscribe to data changes and build initial UI.
    // OnEnable - Đăng ký lắng nghe thay đổi dữ liệu và xây dựng UI ban đầu.
    // =========================================================================
    private void OnEnable()
    {
        var sum = Thuan_23127_SeasonalSummary.Instance;
        if (sum) sum.OnChanged += Rebuild;
        Rebuild();
    }

    // =========================================================================
    // Freeze - Prevents or allows UI rebuilding.
    // Freeze - Ngăn cản hoặc cho phép rebuild UI.
    // =========================================================================
    public void Freeze(bool v) { _frozen = v; }

    // =========================================================================
    // OnDisable - Unsubscribe from events to prevent memory leaks.
    // OnDisable - Hủy đăng ký sự kiện để tránh rò rỉ bộ nhớ.
    // =========================================================================
    private void OnDisable()
    {
        var sum = Thuan_23127_SeasonalSummary.Instance;
        if (sum) sum.OnChanged -= Rebuild;
    }

    // =========================================================================
    // Rebuild - Clears and rebuilds the product rows from current data.
    // Rebuild - Xóa và xây dựng lại các row sản phẩm từ dữ liệu hiện tại.
    // 
    // Called by: OnEnable, SeasonalSummary.OnChanged event.
    // Được gọi bởi: OnEnable, sự kiện SeasonalSummary.OnChanged.
    // =========================================================================
    public void Rebuild()
    {
        // Don't rebuild if game not active and we already have rows.
        // Không rebuild nếu game chưa active và đã có rows.
        if (!RulesoftheGame_VU2_1.GameActive && _pool.Count > 0) return;
        if (!content || !rowPrefab) return;

        // Clear existing rows.
        // Xóa các row hiện có.
        for (int i = 0; i < _pool.Count; i++) if (_pool[i]) Destroy(_pool[i].gameObject);
        _pool.Clear();

        // Get score data from SeasonalSummary.
        // Lấy dữ liệu điểm từ SeasonalSummary.
        var sum = Thuan_23127_SeasonalSummary.Instance;
        var data = sum ? sum.GetAllScores() : new List<(Sprite, int, int)>();

        // Create a row for each product type.
        // Tạo một row cho mỗi loại sản phẩm.
        foreach (var (icon, rainy, dry) in data)
        {
            var row = Instantiate(rowPrefab, content);
            row.gameObject.SetActive(true);
            row.SetData(icon, rainy, dry);
            _pool.Add(row);
        }
    }

    // =========================================================================
    // ClearAllRows - Removes all product rows from display.
    // ClearAllRows - Xóa tất cả row sản phẩm khỏi hiển thị.
    // 
    // Called by: Game restart to clear the summary.
    // Được gọi bởi: Restart game để xóa tổng kết.
    // =========================================================================
    public void ClearAllRows()
    {
        if (!content) return;
        var rows = content.GetComponentsInChildren<UI_ProductRow>(true);
        for (int i = rows.Length - 1; i >= 0; i--) if (rows[i]) Destroy(rows[i].gameObject);
    }
}