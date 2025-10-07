using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public enum Season { Rainy = 0, Normal = 1, Dry = 2 }

public class Thuan_23127_AreaHUD : MonoBehaviour
{
    [Header("Progress")]
    public Image progressFill;
    public Text progressPercentText;

    [Header("Salinity")]
    public Text salinityText;
    
    public Text rainyScoreText;
    public Text normalScoreText;
    public Text dryScoreText;
    
    private int _rainy, _normal, _dry; 
    
    public void SetProgress(float t)
    {
        if (progressFill)        progressFill.fillAmount = Mathf.Clamp01(t);
        if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(Mathf.Clamp01(t) * 100f) + "%";
    }
    
    public void SetSalinity(float current, float threshold)
    {
        if (!salinityText) return;
        salinityText.text = $"{current:0.00} / {threshold:0.00}";
    }
    
    public void ResetSeasonScores()
    {
        _rainy = _normal = _dry = 0;
        RefreshSeasonTexts();
    }
    
    public void AddSeasonPoints(Season season, int delta)
    {
        switch (season)
        {
            case Season.Rainy:  _rainy  += delta; break;
            case Season.Normal: _normal += delta; break;
            case Season.Dry:    _dry    += delta; break;
            default:
                throw new ArgumentOutOfRangeException(nameof(season), season, null);
        }
        RefreshSeasonTexts();
    }
    
    public void SetSeasonScores(int rainy, int normal, int dry)
    {
        _rainy = rainy; _normal = normal; _dry = dry;
        RefreshSeasonTexts();
    }
    
    private void RefreshSeasonTexts() 
    {
        if (rainyScoreText)  rainyScoreText.text  = _rainy.ToString();
        if (normalScoreText) normalScoreText.text = _normal.ToString();
        if (dryScoreText)    dryScoreText.text    = _dry.ToString();
    }

    public void Show(bool v) => gameObject.SetActive(v);
}
