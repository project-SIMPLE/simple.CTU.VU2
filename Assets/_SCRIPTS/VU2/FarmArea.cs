using UnityEngine;
using System;
using System.Collections.Generic;

public class FarmArea : MonoBehaviour
{
    [Header("Setup")]
    public Transform[] plotPoints;

    [Header("Refs")]
    public Thuan_23127_JsonReader jsonReader;
    public Thuan_23127_AreaHUD hud;

    private bool[] isPlanted;

    // Theo dõi “1 cây đang xem” cho progress/salinity
    private Thuan_23127_PlantGrowth _boundGrowth;
    private Action<float> _onProg;
    private Action<float, float> _onSalt;
    private Action<Thuan_23127_PlantGrowth.State> _onState;
    private Action _onAboutToDestroy;

    // Tổng điểm theo mùa của CẢ Ô (0:Rainy,1:Normal,2:Dry)
    private readonly int[] _seasonTotals = new int[3];

    // Danh sách toàn bộ cây trong ô 
    private readonly List<Thuan_23127_PlantGrowth> _growths = new List<Thuan_23127_PlantGrowth>();

    [Header("Area Seasonal Salinity")]
    [Header("Area Seasonal Salinity")]
    [Range(0, 5)] public float rainySalinity  = 0.5f;  // dùng cho Rainy1 & Rainy2
    [Range(0, 5)] public float drySalinity    = 1.5f;
    public bool useAreaSeasonalSalinity = true;


    /// <summary>
    /// Trả về độ mặn của Ô theo mùa
    /// </summary>
    private float GetAreaSalinity()
    {
        if (!useAreaSeasonalSalinity)
            return Thuan_23127_GameManager.Instance
                ? Thuan_23127_GameManager.Instance.GetSeasonSalinity()
                : 0f;

        // 0=Rainy1, 1=Dry, 2=Rainy2 (tương thích số cũ)
        var s = RulesoftheGame_VU2_1.Saltwater_Intrusion;
        if (Mathf.Approximately(s, 1f)) return drySalinity;  // Dry
        return rainySalinity;                                 // Rainy1 & Rainy2
    }

    private SeasonPhase CurrentPhase()
    {
        var s = RulesoftheGame_VU2_1.Saltwater_Intrusion;
        if (Mathf.Approximately(s, 0f)) return SeasonPhase.Rainy1;
        if (Mathf.Approximately(s, 1f)) return SeasonPhase.Dry;
        return SeasonPhase.Rainy2;
    }

    

    /// <summary>
    /// Xác định mùa hiện tại dựa trên Saltwater_Intrusion (0/1/2)
    /// </summary>
    private Season CurrentSeason()
    {
        var s = RulesoftheGame_VU2_1.Saltwater_Intrusion;
        if (Mathf.Approximately(s, 0f)) return Season.Rainy;
        if (Mathf.Approximately(s, 1f)) return Season.Normal;
        return Season.Dry;
    }

    /// <summary>
    /// Khởi tạo mảng trạng thái & reset HUD của ô
    /// </summary>
    private void Start()
    {
        isPlanted = new bool[plotPoints.Length];
        if (hud)
        {
            hud.Show(true);
            hud.SetSeasonScores(0, 0, 0); // clear tổng ban đầu
            hud.SetSubject(null);   
        }
    }

    /// <summary>
    /// Gỡ bind “cây đang xem” khỏi HUD (tránh leak listener)
    /// </summary>
    private void UnbindCurrent()
    {
        if (!_boundGrowth || !hud) return;

        _boundGrowth.OnProgressChanged -= _onProg;
        _boundGrowth.OnSalinityChanged -= _onSalt;
        _boundGrowth.OnStateChanged    -= _onState;
        _boundGrowth.OnAboutToDestroy  -= _onAboutToDestroy;

        _boundGrowth = null;
        _onProg = null; _onSalt = null; _onState = null; _onAboutToDestroy = null;
    }

    /// <summary>
    /// Đăng ký 1 cây vào bộ **tổng điểm của Ô**.
    /// - Nhận điểm harvest → cộng vào tổng mùa hiện tại
    /// - Cập nhật HUD cột mùa tương ứng
    /// - Tự gỡ lắng nghe khi cây sắp bị huỷ
    /// </summary>
    private void WireGrowthForAreaTotals(Thuan_23127_PlantGrowth g)
    {
        if (!g) return;
        g.SetSalinityProvider(GetAreaSalinity);

        g.OnHarvested += (points) =>
        {
            if (points <= 0) return;
            var phase = CurrentPhase();                 // Rainy1/Dry/Rainy2
            int idx = (int)phase;
            _seasonTotals[idx] += points;

            // HUD mới theo pha (xem thêm phần HUD bên dưới)
            if (hud) hud.AddSeasonPointsPhase(phase, points, null);
        };

        g.OnAboutToDestroy += () =>
        {
            g.OnHarvested -= null;
            _growths.Remove(g);
        };

        _growths.Add(g);
    }


    /// <summary>
    /// Bind “cây đang xem” cho HUD: chỉ progress/salinity (tổng điểm đã có WireGrowthForAreaTotals lo)
    /// </summary>
    private void BindGrowthToHUD(Thuan_23127_PlantGrowth growth)
    {
        if (!hud || !growth) return;
        UnbindCurrent();

        // progress/salinity theo Ô
        growth.SetSalinityProvider(GetAreaSalinity);

        hud.Show(true);
        hud.SetProgress(0f);

        _onProg = hud.SetProgress;
        _onSalt = hud.SetSalinity;
        _onState = s => { if (s == Thuan_23127_PlantGrowth.State.Done) hud.SetProgress(0f); };
        _onAboutToDestroy = () => { UnbindCurrent(); /* vẫn để HUD hiển thị tổng của ô */ };

        growth.OnProgressChanged += _onProg;
        growth.OnSalinityChanged += _onSalt;
        growth.OnStateChanged    += _onState;
        growth.OnAboutToDestroy  += _onAboutToDestroy;

        _boundGrowth = growth;
        growth.UpdateSalinityEvent(); // cập nhật số ban đầu
    }

    /// <summary>
    /// Trồng tất cả các plot rỗng trong ô bằng 1 prefab
    /// </summary>
    public void PlantAll(GameObject plantPrefab) => PlantInternal(plantPrefab, fillAll: true);

    /// <summary>
    /// Core trồng cây: instantiate, gắn Growth, wire vào tổng & (tuỳ chọn) bind lên HUD
    /// </summary>
    private void PlantInternal(GameObject plantPrefab, bool fillAll)
    {
        if (plantPrefab == null || jsonReader == null) return;

        var tag = plantPrefab.GetComponent<Thuan_23127_SeedTag>();
        if (tag == null) { Debug.LogWarning("Prefab thiếu SeedTag."); return; }

        var plantData  = (tag.plantId  > 0) ? jsonReader.GetPlantById(tag.plantId)      : null;
        var fishData   = (tag.fishId   > 0) ? jsonReader.GetFishById(tag.fishId)        : null;
        var animalData = (tag.animalId > 0) ? jsonReader.GetLivestockById(tag.animalId) : null;
        if (plantData == null && fishData == null && animalData == null)
        { Debug.LogWarning("Không tìm thấy dữ liệu phù hợp trong JSON."); return; }

        for (int i = 0; i < plotPoints.Length; i++)
        {
            if (isPlanted[i]) continue;

            var parent = plotPoints[i];
            var go = Instantiate(plantPrefab, parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = plantPrefab.transform.localScale;

            var growth = go.GetComponent<Thuan_23127_PlantGrowth>() ?? go.AddComponent<Thuan_23127_PlantGrowth>();

            // 1) Đưa cây này vào bộ tổng của Ô
            WireGrowthForAreaTotals(growth);
            
            var summary = Thuan_23127_SeasonalSummary.Instance;
            if (summary) summary.Track(growth, tag);
            // 2) (tuỳ ý) cho HUD theo dõi cây vừa trồng/được chọn
            BindGrowthToHUD(growth);
            
            

            var readerForThis = fillAll ? null : jsonReader;
            if (plantData != null)       growth.Init(plantData,  this, i, readerForThis);
            else if (animalData != null) growth.Init(animalData, this, i, readerForThis);
            else if (fishData != null)   growth.Init(fishData,   this, i, readerForThis);

            // Hiển thị icon đối tượng (nếu bạn set trong SeedTag)
            if (hud) hud.SetSubject(tag.hudIcon);

            isPlanted[i] = true;
            if (!fillAll) break;
        }
    }

    /// <summary>
    /// Khi 1 slot trống trở lại (do cây tự hủy sau delay)
    /// </summary>
    public void FreePlot(int index)
    {
        if (index >= 0 && index < isPlanted.Length) isPlanted[index] = false;
    }

    /// <summary>
    /// Xoá toàn bộ cây trong ô, tháo listener, reset tổng mùa & HUD
    /// </summary>
    public void ResetAllPlots()
    {
        UnbindCurrent();

        foreach (var g in _growths)
        {
            if (g) g.OnHarvested -= null; // phòng hờ
        }
        _growths.Clear();

        for (int i = 0; i < plotPoints.Length; i++)
        {
            var p = plotPoints[i];
            if (!p) continue;
            for (int c = p.childCount - 1; c >= 0; c--)
                Destroy(p.GetChild(c).gameObject);
            isPlanted[i] = false;
        }

        _seasonTotals[0] = _seasonTotals[1] = _seasonTotals[2] = 0;
        if (hud)
        {
            hud.SetProgress(0f);
            hud.SetSeasonScores(0, 0, 0);
            hud.Show(true);
        }
    }
}
