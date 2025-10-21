using System;
using UnityEngine;
using UnityEngine.UI;

public enum SeasonPhase { Rainy1 = 0, Dry = 1, Rainy2 = 2 }

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
    public static Image salinityImage;

    [Header("Season Scores (Theo pha)")]
    // Thứ tự cột mong muốn: Rainy1 (mùa mưa 1), Dry (mùa khô), Rainy2 (mùa mưa 2)
    public SeasonUI rainy;   // cột 1
    public SeasonUI dry;     // cột 2
    public SeasonUI rainy2;  // cột 3

    [Header("Subject (cây/vật nuôi đang theo dõi)")]
    public Image subjectImage;

    // Tổng điểm theo pha: [0]=Rainy1, [1]=Dry, [2]=Rainy2
    private readonly int[] _phaseScores = new int[3];

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
    public void SetSubject(Sprite icon)
    {
        if (!subjectImage) return;
        subjectImage.sprite  = icon;
        subjectImage.enabled = (icon != null);

        var c = subjectImage.color;
        c.a = (icon != null) ? 1f : 0f;
        subjectImage.color = c;

        subjectImage.preserveAspect = true;
    }

    void Awake()
    {
        if (subjectImage)
        {
            subjectImage.sprite  = null;
            subjectImage.enabled = false;
            var c = subjectImage.color; c.a = 0f; subjectImage.color = c;
        }
    }

    /// ========== API THEO PHA (dùng ở FarmArea) ==========

    /// <summary>Reset điểm 3 pha về 0 và refresh UI</summary>
    public void ResetSeasonScoresPhase()
    {
        _phaseScores[0] = _phaseScores[1] = _phaseScores[2] = 0;
        RefreshPhase(SeasonPhase.Rainy1);
        RefreshPhase(SeasonPhase.Dry);
        RefreshPhase(SeasonPhase.Rainy2);
    }

    /// <summary>Set cứng tổng điểm 3 pha</summary>
    public void SetSeasonScoresPhase(int rainy1, int dryVal, int rainy2)
    {
        _phaseScores[0] = Mathf.Max(0, rainy1);
        _phaseScores[1] = Mathf.Max(0, dryVal);
        _phaseScores[2] = Mathf.Max(0, rainy2);

        RefreshPhase(SeasonPhase.Rainy1); // cột mưa 1
        RefreshPhase(SeasonPhase.Dry);    // cột khô
        RefreshPhase(SeasonPhase.Rainy2); // cột mưa 2
    }

    /// <summary>Gộp điểm vào pha tương ứng và (tuỳ chọn) set icon cho pha đó</summary>
    public void AddSeasonPointsPhase(SeasonPhase phase, int delta, Sprite iconOverride = null)
    {
        int idx = (int)phase;
        _phaseScores[idx] = Mathf.Max(0, _phaseScores[idx] + delta);

        var ui = GetUI(phase);
        if (ui == null) return;

        if (ui.scoreText) ui.scoreText.text = _phaseScores[idx].ToString();
        SetIconForPhase(phase, iconOverride);
    }

    /// <summary>Refresh UI cho 1 pha từ giá trị đang lưu</summary>
    private void RefreshPhase(SeasonPhase p)
    {
        var ui = GetUI(p);
        if (ui == null) return;

        int val = _phaseScores[(int)p];
        if (ui.scoreText) ui.scoreText.text = val.ToString();

        if (ui.iconImage)
        {
            if (!ui.iconImage.sprite) ui.iconImage.sprite = ui.defaultIcon;
            ui.iconImage.enabled = (val > 0) && (ui.iconImage.sprite != null);
            ui.iconImage.preserveAspect = true;
        }
    }

    /// <summary>Trả về struct UI tương ứng pha</summary>
    private SeasonUI GetUI(SeasonPhase p)
    {
        switch (p)
        {
            case SeasonPhase.Rainy1: return rainy;   // cột trái
            case SeasonPhase.Dry:    return dry;     // cột giữa
            case SeasonPhase.Rainy2: return rainy2;  // cột phải
        }
        return null;
    }

    private void SetIconForPhase(SeasonPhase phase, Sprite iconOverride = null)
    {
        var ui = GetUI(phase);
        if (ui == null || ui.iconImage == null) return;

        ui.iconImage.sprite  = iconOverride != null ? iconOverride : ui.defaultIcon;
        ui.iconImage.enabled = (_phaseScores[(int)phase] > 0) && (ui.iconImage.sprite != null);
        ui.iconImage.preserveAspect = true;
    }

    public void ResetHUDToDefaults()
    {
        SetProgress(0f);
        if (salinityText) salinityText.text = "0.00 / 0.00";

        ResetSeasonScoresPhase();

        if (subjectImage)
        {
            subjectImage.sprite  = null;
            subjectImage.enabled = false;
            var c = subjectImage.color; c.a = 0f; subjectImage.color = c;
            subjectImage.preserveAspect = true;
        }
    }
}
