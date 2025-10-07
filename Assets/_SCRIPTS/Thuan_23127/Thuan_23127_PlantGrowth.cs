using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class Thuan_23127_PlantGrowth : MonoBehaviour
{
    [Header("Progress UI (dùng chung cho grow & harvest)")]
    public Image progressFill;
    public TextMeshProUGUI progressPercentText;

    [Header("UI (Salinity)")]
    public TextMeshProUGUI salinityText;  // hiển thị độ mặn cho instance này

    [Header("XR (Harvest optional)")]
    public XRSimpleInteractable harvestInteractable;

    [Header("Timing")]
    [SerializeField] private float destroyDelaySeconds = 30f;

    
    // ===== Events để FarmArea/HUD nhận =====
    public enum State { Growing, Ready, Harvesting, Done }
    public event Action<float> OnProgressChanged;                 // 0..1
    public event Action<float, float> OnSalinityChanged;          // current, threshold
    public event Action<State> OnStateChanged;
    public event Action OnAboutToDestroy;

    // dữ liệu nguồn (tuỳ loại)
    private Plant _plantData;
    private Animal _animalData;
    private Fish   _fishData;

    // tham số chung
    private float _growTotal;
    private float _growElapsed;
    private float _harvestTime;
    private int   _econ;

    private bool  _growing, _ready;
    private bool  _harvested;
    private bool  _harvesting;
    private Coroutine _harvestCo;

    private FarmArea _ownerArea;
    private int _ownerIndex = -1;
    private Thuan_23127_JsonReader _jsonReader;
    private Func<float> _salinityProvider;
    
    public event System.Action<int> OnHarvested;

    // === Init cho Plant ===
    public void Init(Plant data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = data;  _animalData = null; _fishData = null;
        _ownerArea  = area;  _ownerIndex = plotIndex; _jsonReader = reader;

        _growTotal   = Mathf.Max(0.01f, data.growth_time);
        _harvestTime = (data.harvest_time > 0f) ? data.harvest_time : 2f;
        _econ        = Mathf.Max(0, data.economic_benefits);

        CommonInitAndStart();
        UpdateSalinityEvent();
    }

    // === Init cho Animal ===
    public void Init(Animal data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = null;  _animalData = data; _fishData = null;
        _ownerArea  = area;  _ownerIndex = plotIndex; _jsonReader = reader;

        _growTotal   = Mathf.Max(0.01f, data.growth_time);
        _harvestTime = (data.harvest_time > 0f) ? data.harvest_time : 2f;
        _econ        = Mathf.Max(0, data.economic_benefits);

        CommonInitAndStart();
        UpdateSalinityEvent();   
    }

    // === Init cho Fish ===
    public void Init(Fish data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = null;  _animalData = null; _fishData = data;
        _ownerArea  = area;  _ownerIndex = plotIndex; _jsonReader = reader;

        _growTotal   = Mathf.Max(0.01f, data.growth_time);
        _harvestTime = (data.harvest_time > 0f) ? data.harvest_time : 2f;
        _econ        = Mathf.Max(0, data.economic_benefits);

        CommonInitAndStart();
        UpdateSalinityEvent();   
    }

    // === Update Salinity UI ===
    public void UpdateSalinityText()
    {
        if (salinityText == null) return;
        var gm = Thuan_23127_GameManager.Instance; if (!gm) return;
        var currentSalinity = gm.GetSeasonSalinity();
        var threshold = 0f;
        if (_plantData  != null) threshold = _plantData.salinity_threshold;
        if (_animalData != null) threshold = _animalData.salinity_threshold;
        if (_fishData   != null) threshold = _fishData.salinity_threshold;

        salinityText.text = $"{currentSalinity:0.00} / {threshold:0.00}";
    }
    
    public  void SetSalinityProvider(System.Func<float> provider) { _salinityProvider = provider; }

    
    private float CurrentSalinity() //cho hiển thị và tính điểm 
    {
        if (_salinityProvider != null) return Mathf.Max(0f, _salinityProvider());
        var gm = Thuan_23127_GameManager.Instance;
        return gm ? gm.GetSeasonSalinity() : 0f; // fallback
    }
    
    private void PushProgress(float t) => OnProgressChanged?.Invoke(t);

    // === Common Init ===
    private void CommonInitAndStart()
    {
        if (!harvestInteractable) harvestInteractable = GetComponent<XRSimpleInteractable>();
        if (harvestInteractable)
        {
            harvestInteractable.selectEntered.RemoveAllListeners();
            harvestInteractable.selectEntered.AddListener(_ => { TryStartHarvest(); });
        }

        _growing = true; _ready = false; _harvested = false; _harvesting = false;

        PushProgress(0f);
        OnStateChanged?.Invoke(State.Growing);
        StartCoroutine(CoGrow());
    }
    
    private IEnumerator CoGrow()
    {
        _growElapsed = 0f;
        while (_growElapsed < _growTotal)
        {
            _growElapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_growElapsed / _growTotal);

            PushProgress(t);          
            UpdateUI(t);            

            yield return null;
        }

        _growing = false;
        _ready   = true;
        OnStateChanged?.Invoke(State.Ready); 

        TryStartHarvest(); // Auto
    }
    
    public void UpdateSalinityEvent() // dùng text cục bộ
    {
        var current = CurrentSalinity();
        var threshold = 0f;
        if (_plantData  != null) threshold = _plantData.salinity_threshold;
        if (_animalData != null) threshold = _animalData.salinity_threshold;
        if (_fishData   != null) threshold = _fishData.salinity_threshold;

        OnSalinityChanged?.Invoke(current, threshold);
    }

    private void OnMouseDown() { TryStartHarvest(); }
    public  void HarvestNow()  { TryStartHarvest(); }

    private void TryStartHarvest()
    {
        if (!_ready || _harvested || _harvesting) return;
        if (_harvestCo != null) StopCoroutine(_harvestCo);
        _harvestCo = StartCoroutine(CoHarvest());
    }

    private IEnumerator CoHarvest()
    {
        _harvesting = true;
        OnStateChanged?.Invoke(State.Harvesting);
        if (harvestInteractable) harvestInteractable.enabled = false;

        PushProgress(0f);
        float e = 0f, h = (_harvestTime > 0f) ? _harvestTime : 2f;
        while (e < h) { e += Time.deltaTime; PushProgress(Mathf.Clamp01(e / h)); yield return null; }

        FinalizeHarvest();
    }
    
    private void FinalizeHarvest()
    {
        if (_harvested) return;
        _harvested = true; _ready = false; _harvesting = false;

        var gm = Thuan_23127_GameManager.Instance;
        // tính hiện tại của bạn theo farmArea
        var points = AdjustBySalinity(_econ);

        if (gm) gm.AddScore(points);

        //  FarmArea/HUD cộng vào ô điểm mùa tương ứng
        OnHarvested?.Invoke(points);

        StartCoroutine(CoDestroyAfter(destroyDelaySeconds));
        OnStateChanged?.Invoke(State.Done);
    }
    
    private int AdjustBySalinity(int baseValue)
    {
        var t = 0f;
        if      (_plantData  != null) t = _plantData.salinity_threshold;
        else if (_animalData != null) t = _animalData.salinity_threshold;
        else if (_fishData   != null) t = _fishData.salinity_threshold;

        var s = CurrentSalinity();
        if (t <= 0f || s <= t) return baseValue;

        var ratio = Mathf.Clamp01(t / s);
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * ratio));
    }

    private IEnumerator CoDestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnAboutToDestroy?.Invoke();             
        if (_ownerArea) _ownerArea.FreePlot(_ownerIndex);
        Destroy(gameObject);
    }

    private void UpdateUI(float t)
    {
        if (progressFill) progressFill.fillAmount = t;
        if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";
    }
    
    // private void FinalizeHarvest()
    // {
    //     if (_harvested) return;
    //     _harvested = true; _ready = false; _harvesting = false;
    //
    //     var gm = Thuan_23127_GameManager.Instance;
    //     if (gm)
    //     {
    //         if      (_plantData  != null) gm.AddScoreForPlant (_econ, _plantData);
    //         else if (_animalData != null) gm.AddScoreForAnimal(_econ, _animalData);
    //         else if (_fishData   != null) gm.AddScoreForFish  (_econ, _fishData);
    //         else                          gm.AddScore(_econ);
    //     }
    //
    //     StartCoroutine(CoDestroyAfter(destroyDelaySeconds));
    //     OnStateChanged?.Invoke(State.Done);
    // }
}
