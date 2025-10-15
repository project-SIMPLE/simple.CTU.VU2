using System.Collections.Generic;
using UnityEngine;

public class Thuan_23127_TotalBoard : MonoBehaviour
{
    [Header("Scroll content & row prefab")]
    public Transform content;          // -> ScrollView/Viewport/Content
    public UI_ProductRow rowPrefab;    // -> Prefab mỗi hàng

    private readonly List<UI_ProductRow> _pool = new();

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

    public void Rebuild()
    {
        if (!content || !rowPrefab) return;

        // Xóa hàng cũ
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i])
                Destroy(_pool[i].gameObject);
        }
        _pool.Clear();

        var sum = Thuan_23127_SeasonalSummary.Instance;

        // Nếu cần lấy quantity (số lần lặp) dùng GetAllCounts()
        // var data = sum ? sum.GetAllCounts() : new List<(Sprite, int, int, int)>();

        // Hiện đang lấy điểm
        var data = sum
            ? sum.GetAllScores()
            : new List<(Sprite, int, int, int)>();

        // Tạo lại các hàng
        foreach (var (icon, r, n, d) in data)
        {
            var row = Instantiate(rowPrefab, content);
            row.gameObject.SetActive(true);
            row.SetData(icon, r, n, d);
            _pool.Add(row);
        }
    }

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