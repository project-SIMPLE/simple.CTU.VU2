using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script hiển thị UI mực nước sông và độ mặn
/// Hỗ trợ đa ngôn ngữ thông qua Thuan_23127_JsonReader
/// Tự động cập nhật khi mùa thay đổi
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
    
    [Header("Đa ngôn ngữ")]
    [Tooltip("Kéo JsonReader từ scene vào đây")]
    public Thuan_23127_JsonReader jsonReader;
    
    // Cache trạng thái hiện tại
    private SeasonPhase _currentPhase = SeasonPhase.Rainy1;
    
    private void Start()
    {
        // Tìm JsonReader nếu chưa gán
        if (jsonReader == null)
        {
            jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();
        }
        
        // Cập nhật UI ban đầu
        UpdateUI(_currentPhase);
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
        UpdateUI(newPhase);
    }
    
    /// <summary>
    /// Cập nhật toàn bộ UI dựa trên mùa
    /// </summary>
    public void UpdateUI(SeasonPhase phase)
    {
        bool isRainy = (phase == SeasonPhase.Rainy1 || phase == SeasonPhase.Rainy2);
        
        // Tính giá trị
        float waterLevel = isRainy ? 100f : 50f;   // Mưa: 100, Khô: 50
        float salinity = isRainy ? 0f : 100f;       // Mưa: 0%, Khô: 100%
        
        // Cập nhật Slider
        if (waterLevelSlider != null)
        {
            waterLevelSlider.value = waterLevel / 100f;
        }
        if (salinitySlider != null)
        {
            salinitySlider.value = salinity / 100f;
        }
        
        // Cập nhật màu Fill
        Color currentColor = isRainy ? rainyColor : dryColor;
        if (waterLevelFill != null)
        {
            waterLevelFill.color = currentColor;
        }
        if (salinityFill != null)
        {
            salinityFill.color = isRainy ? Color.green : Color.red;
        }
        
        // Cập nhật Text với đa ngôn ngữ
        UpdateLabels(isRainy, waterLevel, salinity);
    }
    
    /// <summary>
    /// Cập nhật text labels với hỗ trợ đa ngôn ngữ
    /// </summary>
    private void UpdateLabels(bool isRainy, float waterLevel, float salinity)
    {
        // Lấy dữ liệu ngôn ngữ hiện tại
        var lang = jsonReader?.GetCurrentLangData();
        var labels = lang?.labels;
        
        // Labels mặc định (fallback tiếng Việt)
        var waterLabelText = labels?.water_level ?? "Mực nước sông";
        var salinityLabelText = labels?.salinity ?? "Độ mặn";
        var fullText = labels?.full ?? "Đầy";
        var lowText = labels?.low ?? "Thấp";
        var rainyText = labels?.season_rainy ?? "Mùa mưa";
        var dryText = labels?.season_dry ?? "Mùa khô";
        
        // Cập nhật text
        if (waterLevelLabel != null)
        {
            waterLevelLabel.text = waterLabelText + ":";
        }
        if (waterLevelValue != null)
        {
            waterLevelValue.text = isRainy ? fullText : lowText;
        }
        
        if (salinityLabel != null)
        {
            salinityLabel.text = salinityLabelText + ":";
        }
        if (salinityValue != null)
        {
            salinityValue.text = $"{salinity:0}%";
        }
        
        if (seasonLabel != null)
        {
            seasonLabel.text = isRainy ? rainyText : dryText;
        }
    }
    
    /// <summary>
    /// Gọi khi ngôn ngữ thay đổi để refresh UI
    /// </summary>
    public void RefreshLanguage()
    {
        UpdateUI(_currentPhase);
    }
}
