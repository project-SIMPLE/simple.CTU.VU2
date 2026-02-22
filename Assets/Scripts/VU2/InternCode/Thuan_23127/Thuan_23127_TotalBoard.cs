using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// Thuan_23127_TotalBoard - Builds an Excel-style summary table (3 phases).
// Thuan_23127_TotalBoard - Xây dựng bảng tổng kết dạng Excel (3 giai đoạn).
//
// Layout (Excel-like grid):
// ┌──────────┬──────────────┬──────────────┬──────────────┐
// │          │   T11–T1     │    T2–T3     │     T4       │
// ├──────────┼──────────────┼──────────────┼──────────────┤
// │ [Icon]   │ DT: X        │ DT: X        │ DT: X        │
// │ Tôm      │ SL: Y        │ SL: Y        │ SL: Y        │
// ├──────────┼──────────────┼──────────────┼──────────────┤
// │ [Icon]   │ DT: X        │ DT: X        │ DT: X        │
// │ Lúa      │ SL: Y        │ SL: Y        │ SL: Y        │
// └──────────┴──────────────┴──────────────┴──────────────┘
//
// DT = Diện tích (Area) = harvest count × 10
// SL = Sản lượng (Production/Score)
// =============================================================================
public class Thuan_23127_TotalBoard : MonoBehaviour
{
    // =========================================================================
    // UI REFERENCES
    // THAM CHIẾU UI
    // =========================================================================
    [Header("Scroll content parent / Parent chứa bảng")]
    public Transform content;

    [Header("Row prefab (legacy, optional) / Prefab hàng (cũ, tùy chọn)")]
    public UI_ProductRow rowPrefab;

    // =========================================================================
    // TABLE STYLE SETTINGS
    // CÀI ĐẶT KIỂU BẢNG
    // =========================================================================
    [Header("Table Style / Kiểu bảng")]
    [Tooltip("Font for table text. Leave null for default Arial.")]
    public Font tableFont;

    [Tooltip("Font size for table body text.")]
    public int bodyFontSize = 16;

    [Tooltip("Font size for header text.")]
    public int headerFontSize = 22;

    [Tooltip("Header background color.")]
    public Color headerBgColor = new Color(0.15f, 0.35f, 0.65f, 1f);

    [Tooltip("Header text color.")]
    public Color headerTextColor = Color.white;

    [Tooltip("Body text color.")]
    public Color bodyTextColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    [Tooltip("Even row background.")]
    public Color evenRowColor = new Color(0.92f, 0.95f, 1f, 1f);

    [Tooltip("Odd row background.")]
    public Color oddRowColor = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Grid line color.")]
    public Color gridLineColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Tooltip("Row height for data rows.")]
    public float dataRowHeight = 80f;

    [Tooltip("Row height for header.")]
    public float headerRowHeight = 45f;

    // =========================================================================
    // AREA MULTIPLIER
    // HỆ SỐ DIỆN TÍCH
    // =========================================================================
    private const int AREA_MULTIPLIER = 10;

    // =========================================================================
    // INTERNAL STATE
    // TRẠNG THÁI NỘI BỘ
    // =========================================================================
    private readonly List<GameObject> _createdObjects = new();

    // =========================================================================
    // PHASE LABELS
    // NHÃN GIAI ĐOẠN
    // =========================================================================
    private static readonly string[] PhaseHeaders = { "T11–T1", "T2–T3", "T4" };

    // =========================================================================
    // OnEnable - Subscribe to data change events.
    // OnEnable - Đăng ký sự kiện thay đổi dữ liệu.
    // =========================================================================
    private void OnEnable()
    {
        var summary = Thuan_23127_SeasonalSummary.Instance;
        if (summary != null)
        {
            summary.OnChanged += Rebuild;
        }
        Rebuild();
    }

    // =========================================================================
    // Freeze - Prevents or allows UI rebuilding.
    // Freeze - Ngăn cản hoặc cho phép rebuild UI.
    // =========================================================================
    public void Freeze(bool v) { /* reserved for future use */ }

    // =========================================================================
    // OnDisable - Unsubscribe from events.
    // OnDisable - Hủy đăng ký khỏi events.
    // =========================================================================
    private void OnDisable()
    {
        var summary = Thuan_23127_SeasonalSummary.Instance;
        if (summary != null)
        {
            summary.OnChanged -= Rebuild;
        }
    }

    // =========================================================================
    // Rebuild - Clears and rebuilds the entire Excel-style table.
    // Rebuild - Xóa và xây dựng lại toàn bộ bảng dạng Excel.
    // =========================================================================
    private void Rebuild()
    {
        // Allow rebuild when game ends (for final summary display).
        // Cho phép rebuild khi game kết thúc (để hiển thị tổng kết).
        if (!content)
        {
            Debug.LogWarning("[TotalBoard] Rebuild() skipped: content is null!");
            return;
        }

        Debug.Log($"[TotalBoard] Rebuild() STARTED. content={content.name}, GameActive={RulesoftheGame_VU2_1.GameActive}");

        // Clear all previously created objects.
        // Xóa tất cả objects đã tạo trước đó.
        foreach (var obj in _createdObjects)
            if (obj) Destroy(obj);
        _createdObjects.Clear();

        // Also clear any legacy UI_ProductRow children.
        // Xóa cả các UI_ProductRow con cũ.
        var legacyRows = content.GetComponentsInChildren<UI_ProductRow>(true);
        for (int i = legacyRows.Length - 1; i >= 0; i--)
            if (legacyRows[i]) Destroy(legacyRows[i].gameObject);

        // Ensure font is available.
        // Đảm bảo font khả dụng.
        if (tableFont == null)
            tableFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Ensure content has VerticalLayoutGroup for table layout.
        // Đảm bảo content có VerticalLayoutGroup cho layout bảng.
        EnsureVerticalLayout();

        // Get 3-phase data.
        // Lấy dữ liệu 3 giai đoạn.
        var summary = Thuan_23127_SeasonalSummary.Instance;
        Debug.Log($"[TotalBoard] SeasonalSummary.Instance = {(summary != null ? "EXISTS" : "NULL")}");

        var data = summary
            ? summary.GetAllPhaseData()
            : new List<(Sprite, int[], int[])>();

        Debug.Log($"[TotalBoard] Data rows count = {data.Count}");

        // ── Build header row ──
        // ── Xây dựng hàng tiêu đề ──
        BuildHeaderRow();

        // ── Build data rows ──
        // ── Xây dựng các hàng dữ liệu ──
        int rowIdx = 0;
        foreach (var (icon, scores, counts) in data)
        {
            Debug.Log($"[TotalBoard] Row {rowIdx}: icon={(icon != null ? icon.name : "null")}, " +
                      $"scores=[{scores[0]},{scores[1]},{scores[2]}], " +
                      $"counts=[{counts[0]},{counts[1]},{counts[2]}]");
            BuildDataRow(icon, scores, counts, rowIdx);
            rowIdx++;
        }

        Debug.Log($"[TotalBoard] Rebuild() COMPLETED. Total created objects = {_createdObjects.Count}");
    }

    // =========================================================================
    // EnsureVerticalLayout - Adds VerticalLayoutGroup if missing.
    // EnsureVerticalLayout - Thêm VerticalLayoutGroup nếu chưa có.
    // =========================================================================
    private void EnsureVerticalLayout()
    {
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.spacing = 0;
        vlg.padding = new RectOffset(5, 5, 5, 5);

        var csf = content.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
            csf = content.gameObject.AddComponent<ContentSizeFitter>();
        }
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // =========================================================================
    // BuildHeaderRow - Creates the header: [ ] | T11–T1 | T2–T3 | T4
    // BuildHeaderRow - Tạo hàng tiêu đề: [ ] | T11–T1 | T2–T3 | T4
    // =========================================================================
    private void BuildHeaderRow()
    {
        var row = CreateRowContainer("Header", headerRowHeight, headerBgColor);

        // Empty first cell (icon column).
        // Ô trống đầu tiên (cột icon).
        CreateTextCell(row.transform, "", headerFontSize, headerTextColor, 0.15f);

        // Phase header cells.
        // Các ô tiêu đề giai đoạn.
        for (int i = 0; i < 3; i++)
        {
            CreateTextCell(row.transform, PhaseHeaders[i], headerFontSize,
                           headerTextColor, 0.283f, FontStyle.Bold);
        }
    }

    // =========================================================================
    // BuildDataRow - Creates one product row with icon + 3 phase cells.
    // BuildDataRow - Tạo một hàng sản phẩm với icon + 3 ô giai đoạn.
    //
    // Each phase cell shows:
    //   Diện tích: {count × 10}
    //   Sản lượng: {score}
    // =========================================================================
    private void BuildDataRow(Sprite icon, int[] scores, int[] counts, int rowIdx)
    {
        Color bgColor = (rowIdx % 2 == 0) ? evenRowColor : oddRowColor;
        var row = CreateRowContainer($"Row_{rowIdx}", dataRowHeight, bgColor);

        // Icon cell (first column).
        // Ô icon (cột đầu tiên).
        CreateIconCell(row.transform, icon, 0.15f);

        // 3 phase cells with area + score.
        // 3 ô giai đoạn với diện tích + sản lượng.
        for (int i = 0; i < 3; i++)
        {
            int area = counts[i] * AREA_MULTIPLIER;
            string cellText = $"DT:  {area,4}\nSL:  {scores[i],4}";
            CreateTextCell(row.transform, cellText, bodyFontSize,
                           bodyTextColor, 0.283f, FontStyle.Normal,
                           TextAnchor.MiddleLeft);
        }
    }

    // =========================================================================
    // CreateRowContainer - Creates a horizontal layout row with background.
    // CreateRowContainer - Tạo hàng ngang với nền.
    // =========================================================================
    private GameObject CreateRowContainer(string name, float height, Color bgColor)
    {
        var row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(content, false);
        _createdObjects.Add(row);

        // Background image.
        // Hình nền.
        var bg = row.AddComponent<Image>();
        bg.color = bgColor;

        // Horizontal layout.
        // Layout ngang.
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.spacing = 2;
        hlg.padding = new RectOffset(4, 4, 2, 2);

        // Fixed height.
        // Chiều cao cố định.
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;

        return row;
    }

    // =========================================================================
    // CreateTextCell - Creates a text cell within a row.
    // CreateTextCell - Tạo ô text trong một hàng.
    //
    // flexibleWidth: proportional width (e.g., 0.2 = 20% of row).
    // flexibleWidth: chiều rộng tỷ lệ (ví dụ: 0.2 = 20% hàng).
    // =========================================================================
    private GameObject CreateTextCell(Transform parent, string text, int size,
                                      Color color, float flexibleWidth,
                                      FontStyle style = FontStyle.Normal,
                                      TextAnchor align = TextAnchor.MiddleCenter)
    {
        var cell = new GameObject("Cell", typeof(RectTransform));
        cell.transform.SetParent(parent, false);

        // Cell background (for grid lines).
        // Nền ô (cho đường kẻ lưới).
        var cellBg = cell.AddComponent<Image>();
        cellBg.color = new Color(0, 0, 0, 0); // transparent

        // Layout sizing.
        // Kích thước layout.
        var le = cell.AddComponent<LayoutElement>();
        le.flexibleWidth = flexibleWidth;

        // Add outline for readability (on cell Image).
        // Thêm outline để dễ đọc (trên Image ô).
        var outline = cell.AddComponent<Outline>();
        outline.effectColor = gridLineColor;
        outline.effectDistance = new Vector2(0.5f, 0.5f);

        // Text on a child object (Unity only allows one Graphic per GameObject).
        // Text trên object con (Unity chỉ cho phép 1 Graphic mỗi GameObject).
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(cell.transform, false);

        // Stretch text to fill entire cell.
        // Kéo giãn text để lấp đầy toàn bộ ô.
        var textRT = textGo.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // Text component.
        // Component text.
        var txt = textGo.AddComponent<Text>();
        txt.text = text;
        txt.font = tableFont;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = align;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.resizeTextForBestFit = false;

        return cell;
    }

    // =========================================================================
    // CreateIconCell - Creates an icon cell with the product sprite.
    // CreateIconCell - Tạo ô icon với sprite sản phẩm.
    // =========================================================================
    private GameObject CreateIconCell(Transform parent, Sprite sprite, float flexibleWidth)
    {
        var cell = new GameObject("IconCell", typeof(RectTransform));
        cell.transform.SetParent(parent, false);

        // Layout sizing.
        var le = cell.AddComponent<LayoutElement>();
        le.flexibleWidth = flexibleWidth;

        // Icon image.
        // Hình icon.
        var img = cell.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
        }
        else
        {
            img.color = new Color(0, 0, 0, 0); // transparent if no icon
        }

        return cell;
    }

    // =========================================================================
    // ClearAllRows - Removes all created objects from display.
    // ClearAllRows - Xóa tất cả objects đã tạo khỏi hiển thị.
    //
    // Called by: Game restart to clear the summary.
    // Được gọi bởi: Restart game để xóa tổng kết.
    // =========================================================================
    public void ClearAllRows()
    {
        foreach (var obj in _createdObjects)
            if (obj) Destroy(obj);
        _createdObjects.Clear();

        // Also clear legacy rows.
        if (!content) return;
        var rows = content.GetComponentsInChildren<UI_ProductRow>(true);
        for (int i = rows.Length - 1; i >= 0; i--)
            if (rows[i]) Destroy(rows[i].gameObject);
    }
}