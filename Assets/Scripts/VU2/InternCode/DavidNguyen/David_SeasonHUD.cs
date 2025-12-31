using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script hiển thị UI mực nước sông và độ mặn
/// Hỗ trợ đa ngôn ngữ thông qua Thuan_23127_JsonReader
/// Tự động cập nhật khi mùa thay đổi với hiệu ứng chuyển động mượt mà
/// </summary>
public class David_SeasonHUD : MonoBehaviour
{
    [Header("Mực nước sông")]
    public Text waterLevelLabel;
    public Text waterLevelValue;
    public Slider waterLevelSlider;
    public Image waterLevelFill;
    
    [Header("Độ mặn")]
    public Text salinityLabel;
    public Text salinityValue;
    public Slider salinitySlider;
    public Image salinityFill;
    
    [Header("Thời gian / Mùa (Optional)")]
    public Text seasonLabel;
    public Text timeLabel;
    
    [Header("Màu sắc")]
    public Color rainyColor = new Color(0.2f, 0.6f, 1f, 1f);   // Xanh dương
    public Color dryColor = new Color(1f, 0.6f, 0.2f, 1f);     // Cam
    
    [Header("Cấu hình Animation")]
    [Tooltip("Thời gian chuyển đổi (giây)")]
    public float transitionDuration = 10f;
    
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
        
        // Cập nhật UI ban đầu (không cần animation lúc start)
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
    /// Cập nhật toàn bộ UI dựa trên mùa
    /// </summary>
    /// <param name="instant">Nếu true, cập nhật ngay lập tức không chạy hiệu ứng</param>
    public void UpdateUI(SeasonPhase phase, bool instant = false)
    {
        bool isRainy = (phase == SeasonPhase.Rainy1 || phase == SeasonPhase.Rainy2);
        
        // Target values
        float targetWaterLevel = isRainy ? 1f : 0.5f;   // Mưa: 100%, Khô: 50%
        float targetSalinity = isRainy ? 0f : 1f;       // Mưa: 0%, Khô: 100%
        
        // Update Static Texts (Labels, Season Name, etc.)
        UpdateStaticLabels(isRainy);

        // Update Colors
        Color targetColor = isRainy ? rainyColor : dryColor;
        if (waterLevelFill != null) waterLevelFill.color = targetColor;
        if (salinityFill != null) salinityFill.color = isRainy ? Color.green : Color.red;

        if (instant)
        {
            if (waterLevelSlider != null) waterLevelSlider.value = targetWaterLevel;
            if (salinitySlider != null) salinitySlider.value = targetSalinity;
            
            // Cập nhật text giá trị ngay lập tức
            UpdateDynamicValues(targetWaterLevel * 100f, targetSalinity * 100f, isRainy);
        }
        else
        {
            // Stop animation cũ nếu đang chạy
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            
            // Bắt đầu animation mới
            _animationCoroutine = StartCoroutine(AnimateUIChange(targetWaterLevel, targetSalinity, isRainy));
        }
    }

    private IEnumerator AnimateUIChange(float targetWater, float targetSalinity, bool isRainy)
    {
        // Lấy giá trị khởi điểm từ slider hiện tại
        float startWater = waterLevelSlider != null ? waterLevelSlider.value : 0f;
        float startSalinity = salinitySlider != null ? salinitySlider.value : 0f;
        
        float timer = 0f;
        
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / transitionDuration);
            
            // Lerp giá trị
            float currentWater = Mathf.Lerp(startWater, targetWater, t);
            float currentSalinity = Mathf.Lerp(startSalinity, targetSalinity, t);
            
            // Apply vào Slider
            if (waterLevelSlider != null) waterLevelSlider.value = currentWater;
            if (salinitySlider != null) salinitySlider.value = currentSalinity;
            
            // Cập nhật text số liệu theo thời gian thực
            UpdateDynamicValues(currentWater * 100f, currentSalinity * 100f, isRainy);
            
            yield return null;
        }
        
        // Đảm bảo giá trị cuối cùng chính xác
        if (waterLevelSlider != null) waterLevelSlider.value = targetWater;
        if (salinitySlider != null) salinitySlider.value = targetSalinity;
        UpdateDynamicValues(targetWater * 100f, targetSalinity * 100f, isRainy);
    }
    
    private void UpdateDynamicValues(float waterPercent, float salinityPercent, bool isRainy)
    {
        // Lấy data ngôn ngữ để hiển thị text (Full/Low)
        var lang = jsonReader?.GetCurrentLangData();
        var labels = lang?.labels;
        
        var fullText = labels?.full ?? "Đầy";
        var lowText = labels?.low ?? "Thấp";
        
        // Logic hiển thị Water Value Text (Ví dụ > 75% là Đầy, còn lại là Thấp - hoặc giữ logic cũ theo mùa)
        // Logic cũ: isRainy ? Full : Low. 
        // Để khớp với animation, ta có thể đổi text dựa trên ngưỡng
        string currentWaterText = (waterPercent >= 75f) ? fullText : lowText;
        
        if (waterLevelValue != null) waterLevelValue.text = currentWaterText;
        if (salinityValue != null) salinityValue.text = $"{salinityPercent:0}%";
    }

    private void UpdateStaticLabels(bool isRainy)
    {
        var lang = jsonReader?.GetCurrentLangData();
        var labels = lang?.labels;
        
        var waterLabelText = labels?.water_level ?? "Mực nước sông";
        var salinityLabelText = labels?.salinity ?? "Độ mặn";
        var rainyText = labels?.season_rainy ?? "Mùa mưa";
        var dryText = labels?.season_dry ?? "Mùa khô";
        
        if (waterLevelLabel != null) waterLevelLabel.text = waterLabelText + ":";
        if (salinityLabel != null) salinityLabel.text = salinityLabelText + ":";
        if (seasonLabel != null) seasonLabel.text = isRainy ? rainyText : dryText;
    }
    
    /// <summary>
    /// Gọi khi ngôn ngữ thay đổi để refresh UI
    /// </summary>
    public void RefreshLanguage()
    {
        UpdateUI(_currentPhase, true); // Refresh ngay lập tức
    }
}
