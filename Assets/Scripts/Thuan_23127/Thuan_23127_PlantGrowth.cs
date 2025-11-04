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
    
    
    //========================= Cấu hình animator cho plant chưa có fish và animal
    [Header("Anim (optional)")]
    public Animator plantAnimator;              // drag vào nếu có; nếu bỏ trống sẽ tự tìm
    public string animGood = "Tree_Good";
    public string animBad  = "Tree_Bad";
    public float salinityBadDelay = 10f;  
    // runtime
    private bool _isOverSalt = false;           // đang vượt ngưỡng?
    private bool _badAnimPlayedThisSaltPeriod = false;
    private Coroutine _salinityBadCo;

    public event Action<string> OnHealthTextChanged;
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
    /// Bắn event cập nhật độ mặn cho HUD 
    /// </summary>
    public void UpdateSalinityEvent()
    {
        float current = CurrentSalinity();
        float threshold = 0f;
        if (_plantData  != null) threshold = _plantData.salinity_threshold;
        if (_animalData != null) threshold = _animalData.salinity_threshold;
        if (_fishData   != null) threshold = _fishData.salinity_threshold;
        OnSalinityChanged?.Invoke(current, threshold);
        
        EvaluateSalinityEffects(current, threshold); //  kiểm tra để đếm giờ anim xấu khi vượt
        EmitHealthDescription(current, threshold); // thêm dòng này
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
    private IEnumerator CoGrow()
    {
        _growElapsed = 0f;

        if (_growTotal <= 0f)
        {
            OnProgressChanged?.Invoke(1f);
            UpdateUI(1f);
            _growing = false; _ready = true;
            OnStateChanged?.Invoke(State.Ready);

            // if (RulesoftheGame_VU2_1.CurrentScoringMode == ScoreFlow.GrowthTime)
                TryStartHarvest();

            yield break;
        }

        while (_growElapsed < _growTotal) {
            _growElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_growElapsed / _growTotal);
            OnProgressChanged?.Invoke(t);
            UpdateUI(t);
            yield return null;
        }

        _growing = false; _ready = true;
        OnStateChanged?.Invoke(State.Ready);

        // if (RulesoftheGame_VU2_1.CurrentScoringMode == ScoreFlow.GrowthTime)
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
    /// Cập nhật UI tiến độ cục bộ trên prefab 
    /// </summary>
    private void UpdateUI(float t)
    {
        if (progressFill) progressFill.fillAmount = t;
        if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";

        var currentSalinity = CurrentSalinity();
        if (salinityText) salinityText.text = currentSalinity.ToString("F2") + " ‰";

        // Tính threshold 1 lần
        float threshold = 0f;
        if (_plantData  != null) threshold = _plantData.salinity_threshold;
        if (_animalData != null) threshold = _animalData.salinity_threshold;
        if (_fishData   != null) threshold = _fishData.salinity_threshold;

        //  Luôn đánh giá logic mặn → coroutine anim, KHÔNG phụ thuộc vào warningIcon
        EvaluateSalinityEffects(currentSalinity, threshold);
        EmitHealthDescription(currentSalinity, threshold);
        // Sau đó mới xử lý icon (nếu có)
        if (warningIcon)
        {
            float targetAlpha = currentSalinity > threshold ? 1f : 0f;

            var cg = warningIcon.GetComponent<CanvasGroup>();
            if (!cg) cg = warningIcon.gameObject.AddComponent<CanvasGroup>();

            // SNAP lần đầu
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

                return; // kết thúc lần đầu
            }

            // Fade mượt các lần sau
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
    
    
    /// <summary>
    /// Hàm xu lý animator cho plant 
    /// </summary>
    private void EvaluateSalinityEffects(float currentSalinity, float threshold)
    {
        // Chỉ áp cho cây (plant) — nếu muốn áp cho animal/fish thì bỏ điều kiện này
        if (_plantData == null) return;

        bool nowOver = currentSalinity > threshold;

        // Khi rơi vào trạng thái "vượt"
        if (nowOver && !_isOverSalt)
        {
            _isOverSalt = true;
            _badAnimPlayedThisSaltPeriod = false;

            // Bắt đầu đếm 10s
            if (_salinityBadCo != null) StopCoroutine(_salinityBadCo);
            _salinityBadCo = StartCoroutine(CoPlayBadAfterDelay());
        }
        // Khi rời trạng thái "vượt" (quay về an toàn)
        else if (!nowOver && _isOverSalt)
        {
            _isOverSalt = false;

            // Hủy đếm nếu còn
            if (_salinityBadCo != null) { StopCoroutine(_salinityBadCo); _salinityBadCo = null; }

            // (tuỳ chọn) phát anim tốt lại
            if (plantAnimator && !string.IsNullOrEmpty(animGood))
            {
                // Play lại good nếu bạn muốn reset trạng thái
                plantAnimator.Play(animGood, -1, 0f);
            }
        }
    }

    private IEnumerator CoPlayBadAfterDelay()
    {
        float t = 0f;
        while (t < salinityBadDelay)
        {
            // Nếu trong lúc chờ mà hết vượt ngưỡng → thoát
            if (!_isOverSalt)
            {
                _salinityBadCo = null;
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        // Sau 10s vẫn vượt ⇒ play "xấu" 1 lần
        if (_isOverSalt && !_badAnimPlayedThisSaltPeriod)
        {
            _badAnimPlayedThisSaltPeriod = true;
            if (plantAnimator && !string.IsNullOrEmpty(animBad))
            {
                plantAnimator.Play(animBad, -1, 0f);
            }
        }
        _salinityBadCo = null;
    }


    private void Awake()
    {
        if (warningIcon)
        {
            var cg = warningIcon.GetComponent<CanvasGroup>();
            if (!cg) cg = warningIcon.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            warningIcon.enabled = false;
            warningIcon.raycastTarget = false;
            warningIcon.gameObject.SetActive(false);
        }

        // Tìm animator nếu chưa gán
        if (!plantAnimator)
            plantAnimator = GetComponentInChildren<Animator>();
    }
    
    private (string unit, string statusHealthy, string statusDiseased,
        string labelThreshold, string labelCurrent,
        string tplHealthy, string tplDiseased)
        GetLangStrings()
    {
        var jr = _jsonReader ?? Thuan_23127_GameManager.Instance?.jsonReader;

        string unit = "‰", sh = "Tốt", sd = "Bệnh",
            thr = "Ngưỡng chịu mặn", cur = "Độ mặn hiện tại",
            thTpl = "{tag} đang {status}. {currentLabel}: {current}{unit} | {thresholdLabel}: {threshold}{unit}.",
            dsTpl = "{tag} đang {status} do mặn vượt ngưỡng. {currentLabel}: {current}{unit} > {thresholdLabel}: {threshold}{unit}.";

        var l = jr != null ? jr.GetCurrentLangData() : null;
        if (l != null && l.interpretation != null)
        {
            var f = l.interpretation.fields;
            var s = l.interpretation.status_text;
            var t = l.interpretation.templates;

            unit  = f?.unit_ppt                ?? unit;
            sh    = s?.healthy                 ?? sh;
            sd    = s?.diseased                ?? sd;
            thr   = f?.threshold_label         ?? thr;
            cur   = f?.current_salinity_label  ?? cur;
            thTpl = t?.healthy_desc            ?? thTpl;
            dsTpl = t?.diseased_desc           ?? dsTpl;

            Debug.Log($"[Lang] templates from JSON? healthy={t?.healthy_desc != null}, diseased={t?.diseased_desc != null}, langCode={jr?.GetCurrentLangCode()}");
        }
        else
        {
            Debug.LogWarning("[Lang] interpretation null → using fallback defaults.");
        }

        return (unit, sh, sd, thr, cur, thTpl, dsTpl);
    }


    private void EmitHealthDescription(float currentSalinity, float threshold)
    {
        if (_plantData == null) { OnHealthTextChanged?.Invoke(string.Empty); return; }

        string tagName = _plantData.tag_name ?? "Cây";
        bool   diseased = currentSalinity > threshold;

        var ls = GetLangStrings();

        string text = !diseased
            ? ls.tplHealthy
                .Replace("{tag}", tagName)
                .Replace("{status}", ls.statusHealthy)
                .Replace("{currentLabel}", ls.labelCurrent)
                .Replace("{thresholdLabel}", ls.labelThreshold)
                .Replace("{current}", currentSalinity.ToString("0.00"))
                .Replace("{threshold}", threshold.ToString("0.00"))
                .Replace("{unit}", ls.unit)
            : ls.tplDiseased
                .Replace("{tag}", tagName)
                .Replace("{status}", ls.statusDiseased)
                .Replace("{currentLabel}", ls.labelCurrent)
                .Replace("{thresholdLabel}", ls.labelThreshold)
                .Replace("{current}", currentSalinity.ToString("0.00"))
                .Replace("{threshold}", threshold.ToString("0.00"))
                .Replace("{unit}", ls.unit);

        OnHealthTextChanged?.Invoke(text);
    }

}
