using System;
using UnityEngine;
using UnityEngine.UI;

public enum Season { Rainy = 0, Normal = 1, Dry = 2 }

[Serializable]
public class SeasonUI
{
    public Text  scoreText;     // Text điểm của mùa
    public Image iconImage;     // Ảnh đại diện của mùa (badge)
    public Sprite defaultIcon;  // Icon mặc định (kéo sẵn ở Inspector)
}

public class Thuan_23127_AreaHUD : MonoBehaviour
{
    [Header("Progress")]
    public Image progressFill;
    public Text  progressPercentText;

    [Header("Salinity")]
    public Text salinityText;

    [Header("Season Scores (Theo mùa)")]
    public SeasonUI rainy;
    public SeasonUI normal;
    public SeasonUI dry;

    [Header("Subject (cây/vật nuôi đang theo dõi)")]
    public Image subjectImage;

    // lưu tổng điểm theo mùa (0:Rainy,1:Normal,2:Dry)
    private readonly int[] _seasonScores = new int[3];

    /// <summary>Ẩn/hiện toàn bộ HUD</summary>
    public void Show(bool v) => gameObject.SetActive(v);

    /// <summary>Cập nhật thanh % tiến độ</summary>
    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);
        if (progressFill)        progressFill.fillAmount = t;
        if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";
    }

    /// <summary>Hiển thị S hiện tại / T ngưỡng</summary>
    public void SetSalinity(float current, float threshold)
    {
        if (salinityText) salinityText.text = $"{current:0.00} / {threshold:0.00}";
    }

    /// <summary>Đặt icon “đối tượng đang theo dõi” (plant/animal/fish)</summary>
    public void SetSubject(Sprite icon) {
        if (!subjectImage) return;
        subjectImage.sprite  = icon;
        subjectImage.enabled = (icon != null);

        var c = subjectImage.color;
        c.a = (icon != null) ? 1f : 0f;
        subjectImage.color = c;

        subjectImage.preserveAspect = true;
    }

    /// <summary>Reset điểm cả 3 mùa về 0 và refresh UI</summary>
    public void ResetSeasonScores()
    {
        _seasonScores[0] = _seasonScores[1] = _seasonScores[2] = 0;
        RefreshSeason(Season.Rainy);
        RefreshSeason(Season.Normal);
        RefreshSeason(Season.Dry);
    }

    /// <summary>Set cứng tổng điểm 3 mùa (dùng khi reset hoặc load)</summary>
    public void SetSeasonScores(int rainyVal, int normalVal, int dryVal)
    {
        _seasonScores[0] = Mathf.Max(0, rainyVal);
        _seasonScores[1] = Mathf.Max(0, normalVal);
        _seasonScores[2] = Mathf.Max(0, dryVal);
        RefreshSeason(Season.Rainy);
        RefreshSeason(Season.Normal);
        RefreshSeason(Season.Dry);
    }

    /// <summary>Gộp điểm vào mùa tương ứng và (tuỳ chọn) set icon cho mùa đó</summary>
    public void AddSeasonPoints(Season season, int delta, Sprite iconOverride = null)
    {
        int idx = (int)season;
        _seasonScores[idx] = Mathf.Max(0, _seasonScores[idx] + delta);

        var ui = GetUI(season);
        if (ui == null) return;

        if (ui.scoreText) ui.scoreText.text = _seasonScores[idx].ToString();

        // if (ui.iconImage)
        // {
        //     if (iconOverride) ui.iconImage.sprite = iconOverride;
        //     if (!ui.iconImage.sprite) ui.iconImage.sprite = ui.defaultIcon;
        //     ui.iconImage.enabled = (_seasonScores[idx] > 0) && (ui.iconImage.sprite != null);
        //     ui.iconImage.preserveAspect = true;
        // }
        SetIconForSeason(season, iconOverride);
    }

    /// <summary>Refresh UI cho 1 mùa từ giá trị đang lưu</summary>
    private void RefreshSeason(Season s)
    {
        var ui = GetUI(s);
        if (ui == null) return;

        int val = _seasonScores[(int)s];
        if (ui.scoreText) ui.scoreText.text = val.ToString();

        if (ui.iconImage)
        {
            if (!ui.iconImage.sprite) ui.iconImage.sprite = ui.defaultIcon;
            ui.iconImage.enabled = (val > 0) && (ui.iconImage.sprite != null);
            ui.iconImage.preserveAspect = true;
        }
    }

    private void Awake()
    {
        // Ẩn Subject khi mới vào game
        if (!subjectImage) return;
        subjectImage.sprite  = null;
        subjectImage.enabled = false;
        var c = subjectImage.color; c.a = 0f; subjectImage.color = c;
    }

    /// <summary>Trả về struct UI tương ứng mùa</summary>
    private SeasonUI GetUI(Season s)
    {
        switch (s)
        {
            case Season.Rainy:  return rainy;
            case Season.Normal: return normal;
            case Season.Dry:    return dry;
        }
        return null;
    }
    
    private void SetIconForSeason(Season season, Sprite iconOverride = null)
    {
        var ui = GetUI(season);
        if (ui == null || ui.iconImage == null) return;

        // GÁN THẲNG sprite mỗi lần gọi (không check null)
        ui.iconImage.sprite  = iconOverride != null ? iconOverride : ui.defaultIcon;
        ui.iconImage.enabled = (_seasonScores[(int)season] > 0) && (ui.iconImage.sprite != null);
        ui.iconImage.preserveAspect = true;
    }
    
    public void ResetHUDToDefaults()
    {
        SetProgress(0f);

        if (salinityText) salinityText.text = "0.00 / 0.00";

        ResetSeasonScores();

        if (!subjectImage) return;
        subjectImage.sprite  = null;
        subjectImage.enabled = false;
        var c = subjectImage.color; c.a = 0f; subjectImage.color = c;

        subjectImage.preserveAspect = true;
    }


}
