using System;
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
    [Header("Icon warningIcon")]
    public Image warningIcon; // icon khi vuot ngưỡng mặn;
    private bool _warningEvaluatedOnce = false;

    [Header("XR (Harvest optional)")]
    public XRSimpleInteractable harvestInteractable;

    [Header("Timing")]
    [SerializeField] private float destroyDelaySeconds = 30f;

    // ===== Events để FarmArea/HUD nhận =====
    public enum State { Growing, Ready, Harvesting, Done }
    public event Action<float> OnProgressChanged;                  // 0..1
    public event Action<float, float> OnSalinityChanged;           // current, threshold
    public event Action<State> OnStateChanged;                     // vòng đời cây
    public event Action OnAboutToDestroy;                          // trước khi Destroy
    public event Action<int> OnHarvested;                          // điểm thực tế nhận được sau thu hoạch

    // dữ liệu nguồn (tuỳ loại)
    private Plant  _plantData;
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

    // Provider độ mặn theo Ô (được FarmArea tiêm vào)
    private Func<float> _salinityProvider;

    /// <summary>
    /// Cho phép FarmArea tiêm vào hàm trả về độ mặn của Ô theo mùa hiện tại
    /// </summary>
    public void SetSalinityProvider(Func<float> provider) { _salinityProvider = provider; }

    // === Init cho Plant ===
    /// <summary>Khởi tạo từ dữ liệu PLANT và bắt đầu vòng đời (grow→ready→harvest)</summary>
    public void Init(Plant data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = data;  _animalData = null; _fishData = null;
        _ownerArea  = area;  _ownerIndex = plotIndex; _jsonReader = reader;

        _growTotal   = Mathf.Max(0f, data.growth_time);
        _harvestTime = (data.harvest_time > 0f) ? data.harvest_time : 2f;
        _econ        = Mathf.Max(0, data.economic_benefits);
        Debug.Log($"Plant Init - ID: {data.id}, Growth Time: {data.growth_time}");

        CommonInitAndStart();
        UpdateSalinityEvent();   // đẩy UI độ mặn ban đầu
    }

    // === Init cho Animal ===
    /// <summary>Khởi tạo từ dữ liệu ANIMAL</summary>
    public void Init(Animal data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = null;  _animalData = data; _fishData = null;
        _ownerArea  = area;  _ownerIndex = plotIndex; _jsonReader = reader;

        _growTotal   = Mathf.Max(0f, data.growth_time);
        _harvestTime = (data.harvest_time > 0f) ? data.harvest_time : 2f;
        _econ = Mathf.Max(0, data.economic_benefits);
        Debug.Log($"Ânil Init - ID: {data.id}, Growth Time: {data.growth_time}");

        CommonInitAndStart();
        UpdateSalinityEvent();
    }

    // === Init cho Fish ===
    /// <summary>Khởi tạo từ dữ liệu FISH</summary>
    public void Init(Fish data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = null;  _animalData = null; _fishData = data;
        _ownerArea  = area;  _ownerIndex = plotIndex; _jsonReader = reader;

        _growTotal   = Mathf.Max(0f, data.growth_time);
        _harvestTime = (data.harvest_time > 0f) ? data.harvest_time : 2f;
        _econ        = Mathf.Max(0, data.economic_benefits);
        Debug.Log($"Fish Init - ID: {data.id}, Growth Time: {data.growth_time}");
        CommonInitAndStart();
        UpdateSalinityEvent();
    }

    /// <summary>
    /// Lấy độ mặn hiện tại để hiển thị/tính điểm   
    /// </summary>
    private float CurrentSalinity()
    {
        if (_salinityProvider != null) return Mathf.Max(0f, _salinityProvider());
        var gm = Thuan_23127_GameManager.Instance;
        return gm ? gm.GetSeasonSalinity() : 0f; // fallback
    }

    /// <summary>
    /// Bắn event cập nhật độ mặn cho HUD (dùng khi đổi mùa)
    /// </summary>
    public void UpdateSalinityEvent()
    {
        float current = CurrentSalinity();
        float threshold = 0f;
        if (_plantData  != null) threshold = _plantData.salinity_threshold;
        if (_animalData != null) threshold = _animalData.salinity_threshold;
        if (_fishData   != null) threshold = _fishData.salinity_threshold;
        OnSalinityChanged?.Invoke(current, threshold);
    }

    /// <summary>
    /// Chuẩn bị interaction, reset trạng thái và bắt Coroutine grow
    /// </summary>
    private void CommonInitAndStart()
    {
        if (!harvestInteractable) harvestInteractable = GetComponent<XRSimpleInteractable>();
        if (harvestInteractable)
        {
            harvestInteractable.selectEntered.RemoveAllListeners();
            harvestInteractable.selectEntered.AddListener(_ => { TryStartHarvest(); });
        }

        _growing = true; _ready = false; _harvested = false; _harvesting = false;

        OnProgressChanged?.Invoke(0f);
        OnStateChanged?.Invoke(State.Growing);
        StartCoroutine(CoGrow());
    }

    /// <summary>
    /// Vòng lặp tăng trưởng đến khi sẵn sàng thu hoạch
    /// </summary>
    private System.Collections.IEnumerator CoGrow()
    {
        _growElapsed = 0f;

        if (_growTotal <= 0f)
        {
            // ready ngay
            OnProgressChanged?.Invoke(1f);
            UpdateUI(1f);
            _growing = false; _ready = true;
            OnStateChanged?.Invoke(State.Ready);
            TryStartHarvest();           // auto-harvest nếu bạn muốn
            yield break;
        }

        while (_growElapsed < _growTotal)
        {
            _growElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_growElapsed / _growTotal);
            OnProgressChanged?.Invoke(t);
            UpdateUI(t);
            yield return null;
        }

        _growing = false;
        _ready   = true;
        OnStateChanged?.Invoke(State.Ready);
        TryStartHarvest();
    }


    private void OnMouseDown() { TryStartHarvest(); }
    public  void HarvestNow()  { TryStartHarvest(); }

    /// <summary>
    /// Kiểm tra điều kiện và bắt đầu coroutine thu hoạch
    /// </summary>
    private void TryStartHarvest()
    {
        if (!_ready || _harvested || _harvesting) return;
        if (_harvestCo != null) StopCoroutine(_harvestCo);
        _harvestCo = StartCoroutine(CoHarvest());
    }

    /// <summary>
    /// Hiệu ứng thu hoạch (đầy progress trong _harvestTime)
    /// </summary>
    private System.Collections.IEnumerator CoHarvest()
    {
        _harvesting = true;
        OnStateChanged?.Invoke(State.Harvesting);
        if (harvestInteractable) harvestInteractable.enabled = false;

        OnProgressChanged?.Invoke(0f);
        float e = 0f, h = (_harvestTime > 0f) ? _harvestTime : 2f;
        while (e < h)
        {
            e += Time.deltaTime;
            var tt = Mathf.Clamp01(e / h);
            OnProgressChanged?.Invoke(tt);

            UpdateUI(tt);

            yield return null;
        }

        FinalizeHarvest();
    }
    // private System.Collections.IEnumerator CoHarvest()
    // {
    //     _harvesting = true;
    //     OnStateChanged?.Invoke(State.Harvesting);
    //     if (harvestInteractable) harvestInteractable.enabled = false;
    //
    //     OnProgressChanged?.Invoke(0f);
    //     float e = 0f, h = (_harvestTime > 0f) ? _harvestTime : 2f;
    //     while (e < h) { e += Time.deltaTime; OnProgressChanged?.Invoke(Mathf.Clamp01(e / h)); yield return null; }
    //
    //     FinalizeHarvest();
    // }

    /// <summary>
    /// Chốt điểm, bắn sự kiện, lên lịch tự hủy
    /// </summary>
    private void FinalizeHarvest()
    {
        if (_harvested) return;
        _harvested = true; _ready = false; _harvesting = false;

        // Tính điểm theo độ mặn khu vực (đã set bằng provider)
        int points = AdjustBySalinity(_econ);

        if (_fishData != null && (_fishData.id == 5 || _fishData.id == 6))
        {
            if (_ownerArea && _ownerArea.waterType == WaterType.Fresh)
            {
                points = -0; // Trừ 5 điểm nếu nuôi sai chỗ tom su'
            }
        }

        if (_fishData != null && (_fishData.id == 2))
        {
            if (_ownerArea && _ownerArea.waterType == WaterType.Salt)
            {
                points = -0; // ca dieu hong ko nuoi dc trong nuoc man.
            }
        }

        var gm = Thuan_23127_GameManager.Instance;
        if (gm) gm.AddScore(points);

        // Cho FarmArea biết số điểm của lần harvest này
        OnHarvested?.Invoke(points);

        StartCoroutine(CoDestroyAfter(destroyDelaySeconds));
        OnStateChanged?.Invoke(State.Done);
    }

    /// <summary>
    /// Điều chỉnh điểm theo ngưỡng mặn T và độ mặn hiện tại S (nếu S>T thì giảm)
    /// </summary>
    private int AdjustBySalinity(int baseValue)
    {
        float t = 0f;
        if      (_plantData  != null) t = _plantData.salinity_threshold;
        else if (_animalData != null) t = _animalData.salinity_threshold;
        else if (_fishData   != null) t = _fishData.salinity_threshold;

        float s = CurrentSalinity();
        if (t <= 0f || s <= t) return baseValue;

        float ratio = Mathf.Clamp01(t / s);
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * ratio));
    }

    /// <summary>
    /// Đợi 1 khoảng rồi báo cho FarmArea giải phóng slot & Destroy
    /// </summary>
    private System.Collections.IEnumerator CoDestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnAboutToDestroy?.Invoke();
        if (_ownerArea) _ownerArea.FreePlot(_ownerIndex);
        Destroy(gameObject);
    }

    /// <summary>
    /// Cập nhật UI tiến độ cục bộ trên prefab (nếu có)
    /// </summary>
    private void UpdateUI(float t)
    {
        if (progressFill) progressFill.fillAmount = t;
        if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";

        var currentSalinity = CurrentSalinity();
        if (salinityText) salinityText.text = currentSalinity.ToString("F2") + " ‰";

        if (warningIcon)
        {
            float threshold = 0f;
            if (_plantData  != null) threshold = _plantData.salinity_threshold;
            if (_animalData != null) threshold = _animalData.salinity_threshold;
            if (_fishData   != null) threshold = _fishData.salinity_threshold;

            float targetAlpha = currentSalinity > threshold ? 1f : 0f;

            var cg = warningIcon.GetComponent<CanvasGroup>();
            if (!cg) cg = warningIcon.gameObject.AddComponent<CanvasGroup>();

            // --- SNAP lần đầu: bật/tắt ngay, không fade ---
            if (!_warningEvaluatedOnce)
            {
                _warningEvaluatedOnce = true;

                if (targetAlpha > 0f && !warningIcon.gameObject.activeSelf)
                    warningIcon.gameObject.SetActive(true);

                cg.alpha = targetAlpha;
                bool show = targetAlpha > 0f;
                warningIcon.enabled = show;
                warningIcon.raycastTarget = show;

                if (!show && warningIcon.gameObject.activeSelf)
                    warningIcon.gameObject.SetActive(false);

                return; // lần đầu kết thúc tại đây
            }

            // --- Từ lần sau: fade mượt như cũ ---
            if (targetAlpha > 0f && !warningIcon.gameObject.activeSelf)
            {
                warningIcon.gameObject.SetActive(true);
                if (cg.alpha <= 0f) cg.alpha = 0f;
            }

            const float fadeDuration = 0.25f;
            cg.alpha = Mathf.MoveTowards(
                cg.alpha,
                targetAlpha,
                (fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f)
            );

            bool visible = cg.alpha > 0.01f;
            warningIcon.enabled = visible;
            warningIcon.raycastTarget = visible;

            if (!visible && warningIcon.gameObject.activeSelf)
                warningIcon.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Ép tính điểm ngay lập tức và hủy object (bỏ qua harvest animation).
    /// Dùng cho "kết sổ theo mùa".
    /// </summary>
    public void ForceHarvestImmediateAndDestroy()
    {
        if (_harvested) return;         // đã chốt điểm rồi thì thôi
        _ready = true;                   // đảm bảo trạng thái coi như đã sẵn sàng
        _harvesting = false;             // không chạy effect nữa

        // Tính & cộng điểm (theo salinity của Ô vì FarmArea đã SetSalinityProvider)
        int points = AdjustBySalinity(_econ);

        // Ràng buộc đặc thù cho FISH (y như FinalizeHarvest)
        if (_fishData != null && (_fishData.id == 5 || _fishData.id == 6))    // tôm
            if (_ownerArea && _ownerArea.waterType == WaterType.Fresh) points = 0;

        if (_fishData != null && (_fishData.id == 2))                          // cá điêu hồng
            if (_ownerArea && _ownerArea.waterType == WaterType.Salt)  points = 0;

        var gm = Thuan_23127_GameManager.Instance;
        if (gm) gm.AddScore(points);

        OnHarvested?.Invoke(points);       // vẫn bắn event để thống kê
        OnStateChanged?.Invoke(State.Done);

        // Báo cho vùng giải phóng slot rồi hủy ngay (không chờ delay)
        OnAboutToDestroy?.Invoke();
        if (_ownerArea) _ownerArea.FreePlot(_ownerIndex);
        Destroy(gameObject);
    }


    private void Awake()
    {
        if (!warningIcon) return;
        var cg = warningIcon.GetComponent<CanvasGroup>();
        if (!cg) cg = warningIcon.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;                      // bắt đầu từ 0
        warningIcon.enabled = false;        // không vẽ
        warningIcon.raycastTarget = false;  // không bắt ray
        warningIcon.gameObject.SetActive(false); // tắt hẳn
    }
}
