using System.Collections.Generic;
using UnityEngine;

public class Thuan_23127_TotalBoard : MonoBehaviour
{
    [Header("Scroll content & row prefab (Prefab ASSET!)")]
    public Transform content;       // Scroll View/Viewport/Content
    public UI_ProductRow rowPrefab; // Prefab ASSET trong Project (không phải scene object)

    private void OnEnable()
    {
        var sum = Thuan_23127_SeasonalSummary.Instance;
        if (sum) sum.OnChanged += Rebuild;
        Rebuild();
    }

    private void OnDisable()
    {
        var sum = Thuan_23127_SeasonalSummary.Instance;
        if (sum) sum.OnChanged -= Rebuild;
    }

    private void Rebuild()
    {
        RebuildNow();
    }

    /// <summary>
    /// Clear các dòng & tạo lại từ dữ liệu hiện tại
    /// </summary>
    public void RebuildNow()
    {
        if (!content || !rowPrefab) return;

        ClearAllRows();

        var sum = Thuan_23127_SeasonalSummary.Instance;
        var data = sum ? sum.GetAllScores() : new List<(Sprite, int, int, int)>();

        foreach (var (icon, r, n, d) in data)
        {
            var row = Instantiate(rowPrefab, content);
            row.gameObject.SetActive(true);
            row.SetData(icon, r, n, d);
        }
    }

    /// <summary>
    /// Xóa các hàng Product
    /// </summary>
    public void ClearAllRows()
    {
        if (!content) return;

        var rows = content.GetComponentsInChildren<UI_ProductRow>(true);
        for (int i = rows.Length - 1; i >= 0; i--)
        {
            if (rows[i])
                Destroy(rows[i].gameObject);
        }
    }
}