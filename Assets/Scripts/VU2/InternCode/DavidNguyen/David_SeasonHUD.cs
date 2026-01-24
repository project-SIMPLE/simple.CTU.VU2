using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// David_SeasonHUD - Displays water level and salinity HUD with smooth animations.
// David_SeasonHUD - Hiển thị HUD mực nước và độ mặn với animation mượt.
// 
// This HUD component shows:
// - River water level (based on month)
// - Salinity inside dyke (fresh water zone)
// - Salinity outside dyke (brackish water zone)
// - Current season indicator
// 
// Component HUD này hiển thị:
// - Mực nước sông (dựa trên tháng)
// - Độ mặn trong đê (vùng nước ngọt)
// - Độ mặn ngoài đê (vùng nước lợ)
// - Chỉ báo mùa hiện tại
// 
// Features:
// - Smooth animated transitions when season/month changes
// - Multi-language support via JsonReader
// - Color changes based on season (blue=rainy, orange=dry)
// 
// Tính năng:
// - Chuyển đổi animation mượt khi mùa/tháng thay đổi
// - Hỗ trợ đa ngôn ngữ qua JsonReader
// - Đổi màu theo mùa (xanh=mưa, cam=khô)
// =============================================================================
public class David_SeasonHUD : MonoBehaviour
{
    // =========================================================================
    // WATER LEVEL UI ELEMENTS
    // CÁC PHẦN TỬ UI MỰC NƯỚC
    // =========================================================================
    [Header("River Water Level / Mực nước sông")]
    // Label text (e.g., "Mực nước sông:").
    // Text nhãn (ví dụ: "Mực nước sông:").
    public Text waterLevelLabel;
    
    // Value text showing current status (e.g., "Đầy" or "Thấp").
    // Text giá trị hiển thị trạng thái (ví dụ: "Đầy" hoặc "Thấp").
    public Text waterLevelValue;
    
    // Slider showing water level 0-100%.
    // Slider hiển thị mực nước 0-100%.
    public Slider waterLevelSlider;
    
    // Fill image for color changes.
    // Image fill để đổi màu.
    public Image waterLevelFill;
    
    // =========================================================================
    // INSIDE DYKE SALINITY (Fresh water zone)
    // ĐỘ MẶN TRONG ĐÊ (Vùng nước ngọt)
    // =========================================================================
    [Header("Inside Dyke Salinity / Độ mặn Trong Đê")]
    public Text insideSalinityLabel;
    public Text insideSalinityValue;
    public Slider insideSalinitySlider;
    public Image insideSalinityFill;
    
    // =========================================================================
    // OUTSIDE DYKE SALINITY (Brackish water zone)
    // ĐỘ MẶN NGOÀI ĐÊ (Vùng nước lợ)
    // =========================================================================
    [Header("Outside Dyke Salinity / Độ mặn Ngoài Đê")]
    public Text outsideSalinityLabel;
    public Text outsideSalinityValue;
    public Slider outsideSalinitySlider;
    public Image outsideSalinityFill;
    
    // =========================================================================
    // SEASON/TIME DISPLAY (Optional)
    // HIỂN THỊ MÙA/THỜI GIAN (Tùy chọn)
    // =========================================================================
    [Header("Season/Time (Optional) / Thời gian/Mùa")]
    // Displays current season name (e.g., "Mùa mưa").
    // Hiển thị tên mùa hiện tại (ví dụ: "Mùa mưa").
    public Text seasonLabel;
    
    // Displays current month (e.g., "Month 6").
    // Hiển thị tháng hiện tại (ví dụ: "Month 6").
    public Text timeLabel;
    
    // =========================================================================
    // COLOR CONFIGURATION
    // CẤU HÌNH MÀU SẮC
    // =========================================================================
    [Header("Colors / Màu sắc")]
    // Color during rainy season (blue-ish).
    // Màu trong mùa mưa (xanh dương).
    public Color rainyColor = new Color(0.2f, 0.6f, 1f, 1f);
    
    // Color during dry season (orange-ish).
    // Màu trong mùa khô (cam).
    public Color dryColor = new Color(1f, 0.6f, 0.2f, 1f);
    
    // =========================================================================
    // ANIMATION CONFIGURATION
    // CẤU HÌNH ANIMATION
    // =========================================================================
    [Header("Animation Config / Cấu hình Animation")]
    [Tooltip("Transition duration in seconds / Thời gian chuyển đổi (giây)")]
    // Duration for smooth slider animations when values change.
    // Thời lượng animation mượt khi giá trị thay đổi.
    public float transitionDuration = 10f;
    
    // =========================================================================
    // SALINITY DATA SOURCES
    // NGUỒN DỮ LIỆU ĐỘ MẶN
    // =========================================================================
    [Header("Salinity Sources / Nguồn độ mặn")]
    [Tooltip("Drag FarmArea for 'Inside Dyke' zone here")]
    // FarmArea representing the fresh water zone (inside dyke).
    // FarmArea đại diện cho vùng nước ngọt (trong đê).
    public FarmArea insideDykeArea;
    
    [Tooltip("Drag FarmArea for 'Outside Dyke' zone here")]
    // FarmArea representing the brackish water zone (outside dyke).
    // FarmArea đại diện cho vùng nước lợ (ngoài đê).
    public FarmArea outsideDykeArea;
    
    [Tooltip("Maximum salinity for slider calculation (unit: ‰)")]
    // Used to convert salinity value to slider percentage.
    // Dùng để chuyển đổi giá trị độ mặn thành phần trăm slider.
    public float maxSalinity = 5f;
    
    // =========================================================================
    // LOCALIZATION
    // ĐA NGÔN NGỮ
    // =========================================================================
    [Header("Localization / Đa ngôn ngữ")]
    [Tooltip("Drag JsonReader from scene")]
    // Reference for getting localized text labels.
    // Tham chiếu để lấy các nhãn text đa ngôn ngữ.
    public Thuan_23127_JsonReader jsonReader;
    
    // =========================================================================
    // INTERNAL STATE
    // TRẠNG THÁI NỘI BỘ
    // =========================================================================
    
    // Currently displayed season phase.
    // Phase mùa đang hiển thị.
    private SeasonPhase _currentPhase = SeasonPhase.Rainy1;
    
    // Active animation coroutine (for cancellation).
    // Coroutine animation đang chạy (để hủy).
    private Coroutine _animationCoroutine;
    
    // =========================================================================
    // Start - Initialize and display initial UI state.
    // Start - Khởi tạo và hiển thị trạng thái UI ban đầu.
    // =========================================================================
    private void Start()
    {
        // Auto-find JsonReader if not assigned.
        // Tự động tìm JsonReader nếu chưa gán.
        if (jsonReader == null)
        {
            jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();
        }
        
        // Display initial UI instantly (no animation).
        // Hiển thị UI ban đầu ngay lập tức (không animation).
        UpdateUI(_currentPhase, true);
    }
    
    // =========================================================================
    // OnEnable - Subscribe to season and month change events.
    // OnEnable - Đăng ký lắng nghe sự kiện đổi mùa và tháng.
    // =========================================================================
    private void OnEnable()
    {
        // Subscribe to season change event.
        // Đăng ký sự kiện đổi mùa.
        RulesoftheGame_VU2_1.OnPhaseChanged += OnSeasonChanged;
        
        // Subscribe to month change event.
        // Đăng ký sự kiện đổi tháng.
        RulesoftheGame_VU2_1.OnMonthChanged += OnMonthChanged;
    }
    
    // =========================================================================
    // OnDisable - Unsubscribe from events to prevent memory leaks.
    // OnDisable - Hủy đăng ký sự kiện để tránh rò rỉ bộ nhớ.
    // =========================================================================
    private void OnDisable()
    {
        RulesoftheGame_VU2_1.OnPhaseChanged -= OnSeasonChanged;
        RulesoftheGame_VU2_1.OnMonthChanged -= OnMonthChanged;
    }
    
    // =========================================================================
    // OnSeasonChanged - Called when season phase changes.
    // OnSeasonChanged - Được gọi khi phase mùa thay đổi.
    // 
    // Triggers UI update with smooth animation.
    // Kích hoạt cập nhật UI với animation mượt.
    // =========================================================================
    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        _currentPhase = newPhase;
        UpdateUI(newPhase, false);
    }

    // =========================================================================
    // OnMonthChanged - Called when month changes.
    // OnMonthChanged - Được gọi khi tháng thay đổi.
    // 
    // Updates water level based on new month's water table.
    // Cập nhật mực nước dựa trên bảng nước của tháng mới.
    // =========================================================================
    private void OnMonthChanged(int newMonth)
    {
        // Update UI using current season state.
        // Cập nhật UI dựa trên mùa hiện tại.
        UpdateUI(_currentPhase, false);
    }
    
    // =========================================================================
    // GetInsideSalinity - Gets salinity for inside dyke zone.
    // GetInsideSalinity - Lấy độ mặn cho vùng trong đê.
    // 
    // Priority: Assigned FarmArea > Global GameManager.
    // Ưu tiên: FarmArea được gán > GameManager toàn cục.
    // =========================================================================
    private float GetInsideSalinity()
    {
        if (insideDykeArea != null)
            return insideDykeArea.GetAreaSalinity();
        var gm = Thuan_23127_GameManager.Instance;
        return gm != null ? gm.GetSeasonSalinity() : 0f;
    }
    
    // =========================================================================
    // GetOutsideSalinity - Gets salinity for outside dyke zone.
    // GetOutsideSalinity - Lấy độ mặn cho vùng ngoài đê.
    // =========================================================================
    private float GetOutsideSalinity()
    {
        if (outsideDykeArea != null)
            return outsideDykeArea.GetAreaSalinity();
        var gm = Thuan_23127_GameManager.Instance;
        return gm != null ? gm.GetSeasonSalinity() : 0f;
    }
    
    // =========================================================================
    // UpdateUI - Main method to update all HUD elements.
    // UpdateUI - Method chính để cập nhật tất cả phần tử HUD.
    // 
    // Parameters:
    // - phase: Current season phase
    // - instant: If true, update immediately. If false, animate.
    // 
    // Tham số:
    // - phase: Phase mùa hiện tại
    // - instant: Nếu true, cập nhật ngay. Nếu false, có animation.
    // =========================================================================
    public void UpdateUI(SeasonPhase phase, bool instant = false)
    {
        bool isRainy = (phase == SeasonPhase.Rainy1 || phase == SeasonPhase.Rainy2);
        
        // Calculate target values.
        // Tính các giá trị đích.
        
        // Water level from month table (0-100%).
        // Mực nước từ bảng tháng (0-100%).
        float targetWaterLevel = Mathf.Clamp01(RulesoftheGame_VU2_1.CurrentWaterLevelPercent / 100f);
        
        // Get actual salinity from both zones.
        // Lấy độ mặn thực từ cả 2 vùng.
        float insideSalinity = GetInsideSalinity();
        float outsideSalinity = GetOutsideSalinity();
        float targetInsideSlider = Mathf.Clamp01(insideSalinity / maxSalinity);
        float targetOutsideSlider = Mathf.Clamp01(outsideSalinity / maxSalinity);
        
        // Update static text labels.
        // Cập nhật các nhãn text cố định.
        UpdateStaticLabels(isRainy);
        UpdateMonthLabel();

        // Update fill colors based on season.
        // Cập nhật màu fill dựa trên mùa.
        Color targetColor = isRainy ? rainyColor : dryColor;
        if (waterLevelFill != null) waterLevelFill.color = targetColor;
        
        // Green for healthy salinity, red for dangerous.
        // Xanh cho độ mặn an toàn, đỏ cho nguy hiểm.
        if (insideSalinityFill != null) insideSalinityFill.color = isRainy ? Color.green : Color.red;
        if (outsideSalinityFill != null) outsideSalinityFill.color = isRainy ? Color.green : Color.red;

        if (instant)
        {
            // Instant update - no animation.
            // Cập nhật tức thì - không animation.
            if (waterLevelSlider != null) waterLevelSlider.value = targetWaterLevel;
            if (insideSalinitySlider != null) insideSalinitySlider.value = targetInsideSlider;
            if (outsideSalinitySlider != null) outsideSalinitySlider.value = targetOutsideSlider;
            
            UpdateDynamicValues(targetWaterLevel * 100f, insideSalinity, outsideSalinity, isRainy);
        }
        else
        {
            // Animated update - smooth transition.
            // Cập nhật có animation - chuyển đổi mượt.
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateUIChange(
                targetWaterLevel, 
                targetInsideSlider, insideSalinity,
                targetOutsideSlider, outsideSalinity, 
                isRainy));
        }
    }

    // =========================================================================
    // AnimateUIChange - Coroutine for smooth slider animations.
    // AnimateUIChange - Coroutine cho animation slider mượt.
    // 
    // Lerps all slider values from current to target over transitionDuration.
    // Nội suy tất cả giá trị slider từ hiện tại đến đích trong transitionDuration.
    // =========================================================================
    private IEnumerator AnimateUIChange(
        float targetWater, 
        float targetInsideSlider, float targetInsideReal,
        float targetOutsideSlider, float targetOutsideReal,
        bool isRainy)
    {
        // Record starting values.
        // Ghi lại giá trị bắt đầu.
        float startWater = waterLevelSlider != null ? waterLevelSlider.value : 0f;
        float startInsideSlider = insideSalinitySlider != null ? insideSalinitySlider.value : 0f;
        float startOutsideSlider = outsideSalinitySlider != null ? outsideSalinitySlider.value : 0f;
        float startInsideReal = startInsideSlider * maxSalinity;
        float startOutsideReal = startOutsideSlider * maxSalinity;
        
        float timer = 0f;
        
        // Animate over duration.
        // Animation trong thời lượng.
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / transitionDuration);
            
            // Lerp all values.
            // Nội suy tất cả giá trị.
            float currentWater = Mathf.Lerp(startWater, targetWater, t);
            float currentInsideSlider = Mathf.Lerp(startInsideSlider, targetInsideSlider, t);
            float currentOutsideSlider = Mathf.Lerp(startOutsideSlider, targetOutsideSlider, t);
            float currentInsideReal = Mathf.Lerp(startInsideReal, targetInsideReal, t);
            float currentOutsideReal = Mathf.Lerp(startOutsideReal, targetOutsideReal, t);
            
            // Apply to sliders.
            // Áp dụng vào slider.
            if (waterLevelSlider != null) waterLevelSlider.value = currentWater;
            if (insideSalinitySlider != null) insideSalinitySlider.value = currentInsideSlider;
            if (outsideSalinitySlider != null) outsideSalinitySlider.value = currentOutsideSlider;
            
            UpdateDynamicValues(currentWater * 100f, currentInsideReal, currentOutsideReal, isRainy);
            
            yield return null;
        }
        
        // Ensure final values are exact.
        // Đảm bảo giá trị cuối cùng chính xác.
        if (waterLevelSlider != null) waterLevelSlider.value = targetWater;
        if (insideSalinitySlider != null) insideSalinitySlider.value = targetInsideSlider;
        if (outsideSalinitySlider != null) outsideSalinitySlider.value = targetOutsideSlider;
        UpdateDynamicValues(targetWater * 100f, targetInsideReal, targetOutsideReal, isRainy);
    }
    
    // =========================================================================
    // UpdateDynamicValues - Updates text values that change during animation.
    // UpdateDynamicValues - Cập nhật giá trị text thay đổi trong animation.
    // =========================================================================
    private void UpdateDynamicValues(float waterPercent, float insidePpt, float outsidePpt, bool isRainy)
    {
        var lang = jsonReader?.GetCurrentLangData();
        var labels = lang?.labels;
        
        // Get localized text for water level status.
        // Lấy text đa ngôn ngữ cho trạng thái mực nước.
        var fullText = labels?.full ?? "Đầy";
        var lowText = labels?.low ?? "Thấp";
        
        // Show "Full" if water >= 75%, otherwise "Low".
        // Hiển thị "Đầy" nếu nước >= 75%, ngược lại "Thấp".
        string currentWaterText = (waterPercent >= 75f) ? fullText : lowText;
        
        if (waterLevelValue != null) waterLevelValue.text = currentWaterText;
        
        // Display salinity with unit (‰ = parts per thousand).
        // Hiển thị độ mặn với đơn vị (‰ = phần nghìn).
        if (insideSalinityValue != null) insideSalinityValue.text = $"{insidePpt:0.0} ‰";
        if (outsideSalinityValue != null) outsideSalinityValue.text = $"{outsidePpt:0.0} ‰";
    }

    // =========================================================================
    // UpdateStaticLabels - Updates fixed text labels with localization.
    // UpdateStaticLabels - Cập nhật các nhãn text cố định với đa ngôn ngữ.
    // =========================================================================
    private void UpdateStaticLabels(bool isRainy)
    {
        var lang = jsonReader?.GetCurrentLangData();
        var labels = lang?.labels;
        
        // Get localized labels with fallbacks.
        // Lấy nhãn đa ngôn ngữ với fallback.
        var waterLabelText = labels?.water_level ?? "Mực nước sông";
        var rainyText = labels?.season_rainy ?? "Mùa mưa";
        var dryText = labels?.season_dry ?? "Mùa khô";
        
        if (waterLevelLabel != null) waterLevelLabel.text = waterLabelText + ":";
        if (insideSalinityLabel != null) insideSalinityLabel.text = "Độ Mặn Trong Đê:";
        if (outsideSalinityLabel != null) outsideSalinityLabel.text = "Độ Mặn Ngoài Đê:";
        if (seasonLabel != null) seasonLabel.text = isRainy ? rainyText : dryText;
    }

    // =========================================================================
    // UpdateMonthLabel - Updates the month display.
    // UpdateMonthLabel - Cập nhật hiển thị tháng.
    // =========================================================================
    private void UpdateMonthLabel()
    {
        if (timeLabel == null) return;

        // Display current month number.
        // Hiển thị số tháng hiện tại.
        var monthText = "Month";
        int monthIndex = RulesoftheGame_VU2_1.CurrentMonthIndex;
        timeLabel.text = $"{monthText} {monthIndex}";
    }
    
    // =========================================================================
    // RefreshLanguage - Call when language changes to update all text.
    // RefreshLanguage - Gọi khi đổi ngôn ngữ để cập nhật tất cả text.
    // =========================================================================
    public void RefreshLanguage()
    {
        UpdateUI(_currentPhase, true);
    }
}
