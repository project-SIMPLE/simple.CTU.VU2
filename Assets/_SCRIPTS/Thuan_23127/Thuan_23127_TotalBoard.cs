using System.Collections.Generic;
using UnityEngine;

public class Thuan_23127_TotalBoard : MonoBehaviour
{
    [Header("Scroll content & row prefab")]
    public Transform content;       // -> Scroll View/Viewport/Content
    public UI_ProductRow rowPrefab; // -> Prefab hàng

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

    private void Rebuild()
    {
        if (!content || !rowPrefab) return;

        // clear cũ
        for (int i = 0; i < _pool.Count; i++)
            if (_pool[i]) Destroy(_pool[i].gameObject);
        _pool.Clear();

        var sum = Thuan_23127_SeasonalSummary.Instance;
        // var data = sum ? sum.GetAllCounts() : new List<(Sprite,int,int,int)>(); lấy quantiy lần lập lại

        var data = sum ? sum.GetAllScores() : new List<(Sprite,int,int,int)>(); // Lấy điểm 
        foreach (var (icon, r, n, d) in data)
        {
            var row = Instantiate(rowPrefab, content);
            row.SetData(icon, r, n, d);
            _pool.Add(row);
        }
    }
}