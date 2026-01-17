using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script hiển thị UI mực nước sông và độ mặn
/// Hỗ trợ đa ngôn ngữ thông qua Thuan_23127_JsonReader
/// Tự động cập nhật khi mùa thay đổi với hiệu ứng chuyển động mượt mà
/// Hiển thị độ mặn cả Trong Đê và Ngoài Đê
/// </summary>
public class David_SeasonHUD : MonoBehaviour
{
    [Header("Mực nước sông")]
    public Text waterLevelLabel;
    public Text waterLevelValue;
    public Slider waterLevelSlider;
    public Image waterLevelFill;
    
    [Header("Độ mặn Trong Đê")]
    public Text insideSalinityLabel;
    public Text insideSalinityValue;
    public Slider insideSalinitySlider;
    public Image insideSalinityFill;
    
    [Header("Độ mặn Ngoài Đê")]
    public Text outsideSalinityLabel;
    public Text outsideSalinityValue;
    public Slider outsideSalinitySlider;
    public Image outsideSalinityFill;
    
    [Header("Thời gian / Mùa (Optional)")]
    public Text seasonLabel;
    public Text timeLabel;
    
    [Header("Màu sắc")]
    public Color rainyColor = new Color(0.2f, 0.6f, 1f, 1f);   // Xanh dương
    public Color dryColor = new Color(1f, 0.6f, 0.2f, 1f);     // Cam
    
    [Header("Cấu hình Animation")]
    [Tooltip("Thời gian chuyển đổi (giây)")]
    public float transitionDuration = 10f;
    
    [Header("Nguồn độ mặn thực")]
    [Tooltip("Kéo FarmArea vùng 'Trong Đê' vào đây")]
    public FarmArea insideDykeArea;
    
    [Tooltip("Kéo FarmArea vùng 'Ngoài Đê' vào đây")]
    public FarmArea outsideDykeArea;
    
    [Tooltip("Độ mặn tối đa để tính % cho Slider (đơn vị ‰)")]
    public float maxSalinity = 5f;
    
    [Header("Đa ngôn ngữ")]
    [Tooltip("Kéo JsonReader từ scene vào đây")]
    public Thuan_23127_JsonReader jsonReader;
    
    // Cache trạng thái hiện tại
    private SeasonPhase _currentPhase = SeasonPhase.Rainy1;
    private Coroutine _animationCoroutine;
    
    private void Start()
    {
        // Tìm JsonReader nếu chưa gán
        if (jsonReader == null)
        {
            jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();
        }
        
        // Cập nhật UI ban đầu 
        UpdateUI(_currentPhase, true);
    }
    
    private void OnEnable()
    {
        // Đăng ký lắng nghe event đổi mùa
        RulesoftheGame_VU2_1.OnPhaseChanged += OnSeasonChanged;
    }
    
    private void OnDisable()
    {
        RulesoftheGame_VU2_1.OnPhaseChanged -= OnSeasonChanged;
    }
    
    /// <summary>
    /// Được gọi khi mùa thay đổi
    /// </summary>
    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        _currentPhase = newPhase;
        UpdateUI(newPhase, false);
    }
    
    /// <summary>
    /// Lấy độ mặn Trong Đê
    /// </summary>
    private float GetInsideSalinity()
    {
        if (insideDykeArea != null)
            return insideDykeArea.GetAreaSalinity();
        var gm = Thuan_23127_GameManager.Instance;
        return gm != null ? gm.GetSeasonSalinity() : 0f;
    }
    
    /// <summary>
    /// Lấy độ mặn Ngoài Đê
    /// </summary>
    private float GetOutsideSalinity()
    {
        if (outsideDykeArea != null)
            return outsideDykeArea.GetAreaSalinity();
        var gm = Thuan_23127_GameManager.Instance;
        return gm != null ? gm.GetSeasonSalinity() : 0f;
    }
    
    /// <summary>
    /// Cập nhật toàn bộ UI dựa trên mùa
    /// </summary>
    public void UpdateUI(SeasonPhase phase, bool instant = false)
    {
        bool isRainy = (phase == SeasonPhase.Rainy1 || phase == SeasonPhase.Rainy2);
        
        // Target values
        float targetWaterLevel = isRainy ? 1f : 0.5f;
        
        // Lấy độ mặn thực từ cả 2 vùng
        float insideSalinity = GetInsideSalinity();
        float outsideSalinity = GetOutsideSalinity();
        float targetInsideSlider = Mathf.Clamp01(insideSalinity / maxSalinity);
        float targetOutsideSlider = Mathf.Clamp01(outsideSalinity / maxSalinity);
        
        // Update Static Texts
        UpdateStaticLabels(isRainy);

        // Update Colors
        Color targetColor = isRainy ? rainyColor : dryColor;
        if (waterLevelFill != null) waterLevelFill.color = targetColor;
        if (insideSalinityFill != null) insideSalinityFill.color = isRainy ? Color.green : Color.red;
        if (outsideSalinityFill != null) outsideSalinityFill.color = isRainy ? Color.green : Color.red;

        if (instant)
        {
            if (waterLevelSlider != null) waterLevelSlider.value = targetWaterLevel;
            if (insideSalinitySlider != null) insideSalinitySlider.value = targetInsideSlider;
            if (outsideSalinitySlider != null) outsideSalinitySlider.value = targetOutsideSlider;
            
            UpdateDynamicValues(targetWaterLevel * 100f, insideSalinity, outsideSalinity, isRainy);
        }
        else
        {
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateUIChange(
                targetWaterLevel, 
                targetInsideSlider, insideSalinity,
                targetOutsideSlider, outsideSalinity, 
                isRainy));
        }
    }

    private IEnumerator AnimateUIChange(
        float targetWater, 
        float targetInsideSlider, float targetInsideReal,
        float targetOutsideSlider, float targetOutsideReal,
        bool isRainy)
    {
        // Giá trị khởi điểm
        float startWater = waterLevelSlider != null ? waterLevelSlider.value : 0f;
        float startInsideSlider = insideSalinitySlider != null ? insideSalinitySlider.value : 0f;
        float startOutsideSlider = outsideSalinitySlider != null ? outsideSalinitySlider.value : 0f;
        float startInsideReal = startInsideSlider * maxSalinity;
        float startOutsideReal = startOutsideSlider * maxSalinity;
        
        float timer = 0f;
        
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / transitionDuration);
            
            // Lerp giá trị
            float currentWater = Mathf.Lerp(startWater, targetWater, t);
            float currentInsideSlider = Mathf.Lerp(startInsideSlider, targetInsideSlider, t);
            float currentOutsideSlider = Mathf.Lerp(startOutsideSlider, targetOutsideSlider, t);
            float currentInsideReal = Mathf.Lerp(startInsideReal, targetInsideReal, t);
            float currentOutsideReal = Mathf.Lerp(startOutsideReal, targetOutsideReal, t);
            
            // Apply vào Slider
            if (waterLevelSlider != null) waterLevelSlider.value = currentWater;
            if (insideSalinitySlider != null) insideSalinitySlider.value = currentInsideSlider;
            if (outsideSalinitySlider != null) outsideSalinitySlider.value = currentOutsideSlider;
            
            UpdateDynamicValues(currentWater * 100f, currentInsideReal, currentOutsideReal, isRainy);
            
            yield return null;
        }
        
        // Đảm bảo giá trị cuối cùng chính xác
        if (waterLevelSlider != null) waterLevelSlider.value = targetWater;
        if (insideSalinitySlider != null) insideSalinitySlider.value = targetInsideSlider;
        if (outsideSalinitySlider != null) outsideSalinitySlider.value = targetOutsideSlider;
        UpdateDynamicValues(targetWater * 100f, targetInsideReal, targetOutsideReal, isRainy);
    }
    
    /// <summary>
    /// Cập nhật các giá trị động
    /// </summary>
    private void UpdateDynamicValues(float waterPercent, float insidePpt, float outsidePpt, bool isRainy)
    {
        var lang = jsonReader?.GetCurrentLangData();
        var labels = lang?.labels;
        
        var fullText = labels?.full ?? "Đầy";
        var lowText = labels?.low ?? "Thấp";
        
        string currentWaterText = (waterPercent >= 75f) ? fullText : lowText;
        
        if (waterLevelValue != null) waterLevelValue.text = currentWaterText;
        
        // Hiển thị độ mặn theo đơn vị ‰
        if (insideSalinityValue != null) insideSalinityValue.text = $"{insidePpt:0.0} ‰";
        if (outsideSalinityValue != null) outsideSalinityValue.text = $"{outsidePpt:0.0} ‰";
    }

    private void UpdateStaticLabels(bool isRainy)
    {
        var lang = jsonReader?.GetCurrentLangData();
        var labels = lang?.labels;
        
        var waterLabelText = labels?.water_level ?? "Mực nước sông";
        var rainyText = labels?.season_rainy ?? "Mùa mưa";
        var dryText = labels?.season_dry ?? "Mùa khô";
        
        if (waterLevelLabel != null) waterLevelLabel.text = waterLabelText + ":";
        if (insideSalinityLabel != null) insideSalinityLabel.text = "Độ Mặn Trong Đê:";
        if (outsideSalinityLabel != null) outsideSalinityLabel.text = "Độ Mặn Ngoài Đê:";
        if (seasonLabel != null) seasonLabel.text = isRainy ? rainyText : dryText;
    }
    
    /// <summary>
    /// Gọi khi ngôn ngữ thay đổi để refresh UI
    /// </summary>
    public void RefreshLanguage()
    {
        UpdateUI(_currentPhase, true);
    }
}
