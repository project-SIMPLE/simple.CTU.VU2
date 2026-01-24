using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// UI_ProductRow - Single row in the end-game summary table.
// UI_ProductRow - Một hàng đơn trong bảng tổng kết cuối game.
// 
// This prefab component displays one product type's scores:
// - Icon for the product (plant/animal/fish)
// - Score earned in Rainy season
// - Score earned in Dry season
// 
// Component prefab này hiển thị điểm của một loại sản phẩm:
// - Icon cho sản phẩm (cây/động vật/cá)
// - Điểm kiếm được trong mùa Mưa
// - Điểm kiếm được trong mùa Khô
// 
// Instantiated by: Thuan_23127_TotalBoard.Rebuild()
// Được instantiate bởi: Thuan_23127_TotalBoard.Rebuild()
// =============================================================================
public class UI_ProductRow : MonoBehaviour
{
    // =========================================================================
    // UI ELEMENTS
    // CÁC PHẦN TỬ UI
    // =========================================================================
    
    // Product icon (plant/animal/fish sprite).
    // Icon sản phẩm (sprite cây/động vật/cá).
    public Image icon;
    
    // Text showing Rainy season score.
    // Text hiển thị điểm mùa Mưa.
    public Text rainyText;
    
    // Text showing Dry season score.
    // Text hiển thị điểm mùa Khô.
    public Text dryText;

    // =========================================================================
    // SetData - Populates the row with product data.
    // SetData - Điền dữ liệu sản phẩm vào hàng.
    // 
    // Parameters:
    // - s: Product icon sprite
    // - r: Rainy season score
    // - d: Dry season score
    // 
    // Tham số:
    // - s: Sprite icon sản phẩm
    // - r: Điểm mùa Mưa
    // - d: Điểm mùa Khô
    // =========================================================================
    public void SetData(Sprite s, int r, int d)
    {
        // Set icon if available.
        // Đặt icon nếu có.
        if (icon)
        {
            icon.sprite = s;
            icon.enabled = (s != null);
            icon.preserveAspect = true;
        }
        
        // Set score texts.
        // Đặt các text điểm.
        if (rainyText)  rainyText.text  = r.ToString();
        if (dryText)    dryText.text    = d.ToString();
    }
}