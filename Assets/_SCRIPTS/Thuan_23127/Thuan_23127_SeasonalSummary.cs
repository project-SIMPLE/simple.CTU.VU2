using System;
using System.Collections.Generic;
using UnityEngine;

public enum ProductKind { Plant, Animal, Fish }

[Serializable]
public class SeasonalCounters
{
    public Sprite icon;
    public int[] count = new int[3];   // 0=Rainy,1=Normal,2=Dry
    public int[] score = new int[3];   // Cộng điểm 
}

public class Thuan_23127_SeasonalSummary : MonoBehaviour
{
    public static Thuan_23127_SeasonalSummary Instance;

    private readonly Dictionary<string, SeasonalCounters> _map = new();
    public event Action OnChanged; // báo UI rebuild

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private static Season CurrentSeason()
    {
        var s = RulesoftheGame_VU2_1.Saltwater_Intrusion;
        if (Mathf.Approximately(s, 0f)) return Season.Rainy;
        if (Mathf.Approximately(s, 1f)) return Season.Normal;
        return Season.Dry;
    }
    
    private static SeasonPhase CurrentPhase()
    {
        var s = RulesoftheGame_VU2_1.Saltwater_Intrusion;
        if (Mathf.Approximately(s, 0f)) return SeasonPhase.Rainy1;
        if (Mathf.Approximately(s, 1f)) return SeasonPhase.Dry;
        return SeasonPhase.Rainy2;
    }


    private static string Key(ProductKind kind, int id) => $"{kind}:{id}";

    private SeasonalCounters GetOrCreate(ProductKind kind, int id, Sprite icon)
    {
        var key = Key(kind, id);
        if (!_map.TryGetValue(key, out var c))
        {
            c = new SeasonalCounters { icon = icon };
            _map[key] = c;
        }
        // cập nhật icon nếu chưa có
        if (c.icon == null && icon != null) c.icon = icon;
        return c;
    }

    /// <summary>
    /// Gọi sau khi Instantiate 1 hạt giống/cá/con; hàm này sẽ
    /// lắng nghe OnHarvested để cộng bộ đếm theo mùa.
    /// </summary>
    public void Track(Thuan_23127_PlantGrowth growth, Thuan_23127_SeedTag tag)
    {
        if (!growth || !tag) return;

        ProductKind kind;
        int id;
        if      (tag.plantId  > 0) { kind = ProductKind.Plant;  id = tag.plantId;  }
        else if (tag.animalId > 0) { kind = ProductKind.Animal; id = tag.animalId; }
        else if (tag.fishId   > 0) { kind = ProductKind.Fish;   id = tag.fishId;   }
        else return;

        // Khi harvest: +1 lượt cho mùa hiện tại (có thể đổi sang + điểm nếu muốn)
        growth.OnHarvested += points =>
        {
            var p = CurrentPhase();                   // 0=Rainy1,1=Dry,2=Rainy2
            var st = GetOrCreate(kind, id, tag.hudIcon);
            st.count[(int)p] += 1;
            st.score[(int)p] += points;
            OnChanged?.Invoke();
        };


        // Khi bị huỷ thì không cần unsub cụ thể — object sẽ bị GC cùng event target.
        growth.OnAboutToDestroy += () => { /* no-op */ };
    }

    /// <summary>Trả về snapshot dữ liệu để UI dựng bảng</summary>
    public List<(Sprite icon, int rainy1, int dry, int rainy2)> GetAllCounts()
    {
        var list = new List<(Sprite,int,int,int)>();
        foreach (var kv in _map.Values)
            list.Add((kv.icon, kv.count[0], kv.count[1], kv.count[2]));
        return list;
    }

    public List<(Sprite icon, int rainy1, int dry, int rainy2)> GetAllScores()
    {
        var list = new List<(Sprite,int,int,int)>();
        foreach (var kv in _map.Values)
            list.Add((kv.icon, kv.score[0], kv.score[1], kv.score[2]));
        return list;
    }

}
