using System;
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
    public static Image salinityImage;

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
        if (salinityText) salinityText.text = $"{current:0.00} / {threshold:0.00}";
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
    public Image subjectImage;

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
