using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SeasonPhase { Rainy1 = 0, Dry = 1, Rainy2 = 2 }

[Serializable]
public class SeasonUI
{
    public Text  scoreText;
    public Image iconImage;
    public Sprite defaultIcon;
}

public class Thuan_23127_AreaHUD : MonoBehaviour
{
    [Header("Progress")]
    public Image progressFill;
    public Text  progressPercentText;

    [Header("Salinity")]
    public Text salinityText;
    public TextMeshProUGUI salinityTextPro;

    public static Image salinityImage;
    
    public Image subjectImage;
    
    public Thuan_23127_JsonReader jsonReader;
    
    public Text descriptionText;
    
    public void SetDescription(string s)
    {
        if (descriptionText) descriptionText.text = s ?? string.Empty;
    }
    
    [Header("Season Scores (2 cột)")]
    public SeasonUI rainy;   // cột 1
    public SeasonUI dry;     // cột 2

    // [0]=Rainy, [1]=Dry
    private readonly int[] _phaseScores = new int[2];

    private static int PhaseToIndex(SeasonPhase p)
    {
        // ✅ mọi thứ ngoài Rainy → Dry
        return (p == SeasonPhase.Rainy1) ? 0 : 1;
    }

    public void Show(bool v) => gameObject.SetActive(v);

    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);
        if (progressFill)        progressFill.fillAmount = t;
        if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";
    }

    public void SetSalinity(float current, float threshold)
    {
        // Lấy nhãn "Độ mặn" từ JSON
        string salinityLabel = "Salinity"; // default
        if (jsonReader != null)
        {
            var langData = jsonReader.GetCurrentLangData();
            if (langData?.interpretation?.fields != null)
            {
                salinityLabel = langData.interpretation.fields.salinity ?? "Salinity";
            }
        }

        // Format: "Độ mặn + 0.5 / 0.8"
        string formattedText = $"{salinityLabel} : {current:0.00} / {threshold:0.00}";
        
        if (salinityText) salinityText.text = formattedText;
        if (salinityTextPro) salinityTextPro.text = formattedText;
    }

    public void SetSubject(Sprite icon)
    {
        if (!subjectImage) return;
        subjectImage.sprite  = icon;
        subjectImage.enabled = (icon != null);

        var c = subjectImage.color; c.a = (icon != null) ? 1f : 0f;
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

    public void ResetSeasonScoresPhase()
    {
        _phaseScores[0] = _phaseScores[1] = 0;
        RefreshPhase(SeasonPhase.Rainy1);
        RefreshPhase(SeasonPhase.Dry);
    }

    public void SetSeasonScoresPhase(int rainyVal, int dryVal)
    {
        _phaseScores[0] = Mathf.Max(0, rainyVal);
        _phaseScores[1] = Mathf.Max(0, dryVal);
        RefreshPhase(SeasonPhase.Rainy1);
        RefreshPhase(SeasonPhase.Dry);
    }

    public void AddSeasonPointsPhase(SeasonPhase phase, int delta, Sprite iconOverride = null)
    {
        int idx = PhaseToIndex(phase);            // ✅ map 2 cột
        _phaseScores[idx] = Mathf.Max(0, _phaseScores[idx] + delta);

        var ui = (idx == 0) ? rainy : dry;
        if (ui.scoreText) ui.scoreText.text = _phaseScores[idx].ToString();
        SetIconForPhase(idx, iconOverride);
    }

    private void RefreshPhase(SeasonPhase p)
    {
        int idx = PhaseToIndex(p);
        var ui  = (idx == 0) ? rainy : dry;
        if (ui == null) return;

        int val = _phaseScores[idx];
        if (ui.scoreText) ui.scoreText.text = val.ToString();

        if (ui.iconImage)
        {
            if (!ui.iconImage.sprite) ui.iconImage.sprite = ui.defaultIcon;
            ui.iconImage.enabled = (val > 0) && (ui.iconImage.sprite != null);
            ui.iconImage.preserveAspect = true;
        }
    }

    private void SetIconForPhase(int idx, Sprite iconOverride = null)
    {
        var ui = (idx == 0) ? rainy : dry;
        if (ui == null || ui.iconImage == null) return;

        ui.iconImage.sprite  = iconOverride != null ? iconOverride : ui.defaultIcon;
        ui.iconImage.enabled = (_phaseScores[idx] > 0) && (ui.iconImage.sprite != null);
        ui.iconImage.preserveAspect = true;
    }

    public void ResetHUDToDefaults()
    {
        // Reset progress về 0
        SetProgress(0f);
        
        // Reset description về empty
        if (descriptionText) descriptionText.text = string.Empty;
        
        // Reset salinity text về empty (không hiển thị gì)
        if (salinityText) salinityText.text = string.Empty;
        if (salinityTextPro) salinityTextPro.text = string.Empty;
        
        // Reset season scores về 0
        ResetSeasonScoresPhase();
        
        // Reset subject image về null
        if (subjectImage)
        {
            subjectImage.sprite  = null;
            subjectImage.enabled = false;
            var c = subjectImage.color; c.a = 0f; subjectImage.color = c;
            subjectImage.preserveAspect = true;
        }
    }
}
