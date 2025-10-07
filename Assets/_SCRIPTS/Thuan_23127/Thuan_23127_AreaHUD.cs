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

    public void Show(bool v) => gameObject.SetActive(v);
}
