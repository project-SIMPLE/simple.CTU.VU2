using UnityEngine;
using System;

public class FarmArea : MonoBehaviour
{
    [Header("Setup")]
    public Transform[] plotPoints;

    [Header("Refs")]
    public Thuan_23127_JsonReader jsonReader;
    public Thuan_23127_AreaHUD hud;

    private bool[] isPlanted;

    // giữ cây đang được HUD theo dõi + delegate để tháo ra gọn gàng
    private Thuan_23127_PlantGrowth _boundGrowth;
    private Action<float> _onProg;
    private Action<float, float> _onSalt;
    private Action<Thuan_23127_PlantGrowth.State> _onState;
    private Action _onAboutToDestroy;
    
    [Range(0, 5)] public float rainySalinity = 0.5f;   // Test cho 1_4
    [Range(0, 5)] public float normalSalinity = 1.0f; // bth
    [Range(0, 5)] public float drySalinity = 1.5f; // kho
    public bool useAreaSeasonalSalinity = true;
    
    private int _rainyScore, _normalScore, _dryScore;

    //trả về độ mặn hiện tại của area theo mùa toàn cục
    public float GetAreaSalinity()
    {
        if (!useAreaSeasonalSalinity)
            return Thuan_23127_GameManager.Instance
                ? Thuan_23127_GameManager.Instance.GetSeasonSalinity()
                : 0f;
        
        var s = RulesoftheGame_VU2_1.Saltwater_Intrusion;
        if (Mathf.Approximately(s, 0f)) return rainySalinity;
        if (Mathf.Approximately(s, 1f)) return normalSalinity;
        if (Mathf.Approximately(s, 2f)) return drySalinity;
        return normalSalinity;
    }

    void Start()
    {
        isPlanted = new bool[plotPoints.Length];
        if (hud) hud.Show(true);
    }

    private void UnbindCurrent() // trành 2 hub tự dính nhau
    {
        if (!_boundGrowth || !hud) return;

        _boundGrowth.OnProgressChanged -= _onProg;
        _boundGrowth.OnSalinityChanged -= _onSalt;
        _boundGrowth.OnStateChanged    -= _onState;
        _boundGrowth.OnAboutToDestroy  -= _onAboutToDestroy;

        _boundGrowth = null;
        _onProg = null; _onSalt = null; _onState = null; _onAboutToDestroy = null;
    }


    // Pass qua hub to show điểm
    private void BindGrowthToHUD(Thuan_23127_PlantGrowth growth)
    {
        if (!hud || !growth) return;
        UnbindCurrent();

        growth.SetSalinityProvider(GetAreaSalinity); // bạn đã có

        hud.Show(true);
        hud.SetProgress(0f);
        hud.SetSeasonScores(_rainyScore, _normalScore, _dryScore); 

        _onProg = hud.SetProgress;
        _onSalt = hud.SetSalinity;
        _onState = s => { if (s == Thuan_23127_PlantGrowth.State.Done) hud.SetProgress(0f); };
        _onAboutToDestroy = () => { UnbindCurrent(); hud.Show(false); };

        // THÊM: cộng điểm vào đúng mùa khi harvest xong
        System.Action<int> onHarvested = (gain) =>
        {
            var season = RulesoftheGame_VU2_1.CurrentSeason;
            switch (season)
            {
                case Season.Rainy:  _rainyScore  += gain; break;
                case Season.Normal: _normalScore += gain; break;
                case Season.Dry:    _dryScore    += gain; break;
            }
            hud.SetSeasonScores(_rainyScore, _normalScore, _dryScore);
        };

        growth.OnProgressChanged += _onProg;
        growth.OnSalinityChanged += _onSalt;
        growth.OnStateChanged    += _onState;
        growth.OnAboutToDestroy  += _onAboutToDestroy;
        growth.OnHarvested       += onHarvested;               // << THÊM

        // nhớ unbind cả OnHarvested:
        _onAboutToDestroy += () => { growth.OnHarvested -= onHarvested; };

        _boundGrowth = growth;

        growth.UpdateSalinityEvent();
    }

    public void PlantAll(GameObject plantPrefab) => PlantInternal(plantPrefab, fillAll: true);

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

            BindGrowthToHUD(growth);

            var readerForThis = fillAll ? null : jsonReader;
            if (plantData != null)       growth.Init(plantData,  this, i, readerForThis);
            else if (animalData != null) growth.Init(animalData, this, i, readerForThis);
            else if (fishData != null)   growth.Init(fishData,   this, i, readerForThis);

            isPlanted[i] = true;

            if (!fillAll) break; // nếu chỉ trồng 1 vị trí, dừng tại đây
            // nếu fillAll = true: HUD sẽ theo dõi cây trồng CUỐI CÙNG (do lần bind cuối cùng).
        }
    }

    public void FreePlot(int index)
    {
        if (index >= 0 && index < isPlanted.Length) isPlanted[index] = false;
    }

    public void ResetAllPlots()
    {
        UnbindCurrent(); // ngắt HUD trước khi xoá
        for (int i = 0; i < plotPoints.Length; i++)
        {
            var p = plotPoints[i];
            if (!p) continue;
            for (int c = p.childCount - 1; c >= 0; c--)
                Destroy(p.GetChild(c).gameObject);
            isPlanted[i] = false;
        }
        _rainyScore = _normalScore = _dryScore = 0; 
        if (hud) { hud.SetProgress(0f); hud.Show(false); }
    }
}
