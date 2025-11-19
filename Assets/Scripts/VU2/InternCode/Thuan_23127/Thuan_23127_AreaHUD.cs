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

    [Header("Salinity (HUD nhỏ trên bảng)")]
    public Text salinityText;
    public TextMeshProUGUI salinityTextPro;

    [Header("Subject & mô tả ngắn trên bảng")]
    public Image subjectImage;
    public Thuan_23127_JsonReader jsonReader;
    public Text descriptionText;

    // (UI_Info_Salinity) 
    [Header("Popup salinity panel (UI_Info_Salinity)")]
    public GameObject showUIInformationSalinity; // UI_Info_Salinity
    public Text popupHeadText;                   // SalintyText (Text)
    public Text popupBodyText;                   // Scroll Text (Text)

    // Giữ CanvasGroup của panel để chỉnh alpha
    private CanvasGroup _popupCg;

    // ==== NEW: HUD nào đang "sở hữu" popup hiện tại ====
    private static Thuan_23127_AreaHUD _currentOwner;

    private void Awake()
    {
        // Lấy CanvasGroup nếu có 
        if (showUIInformationSalinity)
        {
            _popupCg = showUIInformationSalinity.GetComponent<CanvasGroup>();
        }

        if (subjectImage)
        {
            subjectImage.sprite  = null;
            subjectImage.enabled = false;
            var c = subjectImage.color; 
            c.a = 0f; 
            subjectImage.color = c;
        }
    }

    public void OnClickShowInformationSalinity()
    {
        if (!showUIInformationSalinity) return;

        // Panel hiện chưa? (active + alpha > 0)
        bool visible = showUIInformationSalinity.activeSelf;
        if (_popupCg == null && visible)
            _popupCg = showUIInformationSalinity.GetComponent<CanvasGroup>();
        if (_popupCg != null)
            visible = visible && _popupCg.alpha > 0.01f;

        // Nếu panel đang mở và owner chính là HUD này -> tắt
        if (visible && _currentOwner == this)
        {
            HideInformationSalinity();
            return;
        }

        // Ngược lại: mở (hoặc chuyển sang HUD mới)

        // Copy text độ mặn hiện tại từ HUD sang popup
        if (popupHeadText)
        {
            if (salinityTextPro != null && !string.IsNullOrEmpty(salinityTextPro.text))
                popupHeadText.text = salinityTextPro.text;
            else if (salinityText != null)
                popupHeadText.text = salinityText.text;
            else
                popupHeadText.text = string.Empty;
        }

        // Copy mô tả dài 
        if (popupBodyText && descriptionText)
            popupBodyText.text = descriptionText.text ?? string.Empty;

        // Bật panel + đảm bảo alpha = 1
        showUIInformationSalinity.SetActive(true);

        if (_popupCg == null)
            _popupCg = showUIInformationSalinity.GetComponent<CanvasGroup>();

        if (_popupCg != null)
        {
            _popupCg.alpha = 1f;
            // Nếu muốn popup bắt click thì mở 2 dòng dưới:
            // _popupCg.blocksRaycasts = true;
            // _popupCg.interactable   = true;
        }

        _currentOwner = this; // gán owner hiện tại
        Debug.Log($"[AreaHUD] Show salinity info popup - owner = {name}");
    }

    // Nút Close hoặc gọi nội bộ
    public void HideInformationSalinity()
    {
        if (showUIInformationSalinity)
            showUIInformationSalinity.SetActive(false);

        // Nếu chính mình đang sở hữu thì clear
        if (_currentOwner == this)
            _currentOwner = null;

        if (_popupCg != null)
            _popupCg.alpha = 0f;

        Debug.Log($"[AreaHUD] Hide salinity info popup - owner = {name}");
    }

    public void SetDescription(string s)
    {
        if (descriptionText) descriptionText.text = s ?? string.Empty;
    }

    [Header("Season Scores (2 cột)")]
    public SeasonUI rainy;   // cột 1
    public SeasonUI dry;     // cột 2

    private readonly int[] _phaseScores = new int[2];

    private static int PhaseToIndex(SeasonPhase p)
    {
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
        string salinityLabel = "Salinity";
        if (jsonReader != null)
        {
            var langData = jsonReader.GetCurrentLangData();
            if (langData?.interpretation?.fields != null)
            {
                salinityLabel = langData.interpretation.fields.salinity ?? "Salinity";
            }
        }

        string formattedText = $"{salinityLabel} : {current:0.00} / {threshold:0.00}";

        if (salinityText)    salinityText.text    = formattedText;
        if (salinityTextPro) salinityTextPro.text = formattedText;
    }

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
        int idx = PhaseToIndex(phase);
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

        if (descriptionText) descriptionText.text = string.Empty;
        if (salinityText) salinityText.text = string.Empty;
        if (salinityTextPro) salinityTextPro.text = string.Empty;

        ResetSeasonScoresPhase();

        if (subjectImage)
        {
            subjectImage.sprite  = null;
            subjectImage.enabled = false;
            var c = subjectImage.color; 
            c.a = 0f; 
            subjectImage.color = c;
            subjectImage.preserveAspect = true;
        }
    }
}
