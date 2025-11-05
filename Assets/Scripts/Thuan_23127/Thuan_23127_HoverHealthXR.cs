using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class Thuan_23127_HoverHealthXR : MonoBehaviour
{
    [Header("Scroll UI Sample panel")]
    public GameObject panel;                  // world-space panel
    [Tooltip("Text tiêu đề (ví dụ 'Độ mặn')")]
    public Text headText;                     // optional 
    [Tooltip("Text thườngP")]
    public Text bodyText;                     // optional

    public Thuan_23127_JsonReader jsonReader; // có thể bỏ trống, sẽ fallback

    private Thuan_23127_PlantGrowth _growth;
    private XRSimpleInteractable _xr;
    private bool _shown;
    private CanvasGroup _cg;

    void Awake()
    {
        _growth = GetComponent<Thuan_23127_PlantGrowth>();
        _xr     = GetComponent<XRSimpleInteractable>();

        if (panel)
        {
            _cg = panel.GetComponent<CanvasGroup>();
            if (!_cg) _cg = panel.AddComponent<CanvasGroup>();

            _cg.blocksRaycasts = false;
            _cg.interactable   = false;

            foreach (var g in panel.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;

            // ẩn ban đầu
            SetVisible(false, instant:true);
        }
    }

    private void OnEnable()
    {
        if (_growth != null)
        {
            _growth.OnSalinityChanged   += HandleSalinityChanged;
            _growth.OnHealthTextChanged += HandleDescriptionChanged;
        }

        if (_xr != null)
        {
            _xr.hoverEntered.AddListener(OnHoverEntered);
            _xr.hoverExited.AddListener(OnHoverExited);
        }
    }

    private void OnDisable()
    {
        if (_growth != null)
        {
            _growth.OnSalinityChanged   -= HandleSalinityChanged;
            _growth.OnHealthTextChanged -= HandleDescriptionChanged;
        }

        if (_xr != null)
        {
            _xr.hoverEntered.RemoveListener(OnHoverEntered);
            _xr.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs _)
    {
        if (!_growth) return;
        _growth.UpdateSalinityEvent();
        SetVisible(true);
    }

    private void OnHoverExited(HoverExitEventArgs _)
    {
        SetVisible(false);
    }

    // ===== PC (Editor/Standalone) fallback =====
    void OnMouseEnter()
    {
        if (!_growth) return;
        _growth.UpdateSalinityEvent();
        SetVisible(true);
    }

    private void OnMouseExit() => SetVisible(false);

    // ===== Nhận dữ liệu & cập nhật UI =====
    private void HandleSalinityChanged(float current, float threshold)
    {
        if (!headText) return;

        // Lấy nhãn từ JSON 
        string salinityLabel = "Salinity";
        var jr = jsonReader ? jsonReader : Thuan_23127_GameManager.Instance?.jsonReader;
        var lang = jr ? jr.GetCurrentLangData() : null;
        if (lang?.interpretation?.fields != null)
            salinityLabel = lang.interpretation.fields.salinity ?? salinityLabel;

        headText.text = $"{salinityLabel}: {current:0.00} / {threshold:0.00}";
    }

    private void HandleDescriptionChanged(string desc)
    {
        if (bodyText)    bodyText.text    = desc ?? string.Empty;
    }

    private void SetVisible(bool v, bool instant = false)
    {
        _shown = v;
        if (!panel || !_cg) return;

        if (instant)
        {
            panel.SetActive(v);
            _cg.alpha = v ? 1f : 0f;
            return;
        }

        if (v && !panel.activeSelf) panel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeTo(v ? 1f : 0f, 0.15f, () =>
        {
            if (!v) panel.SetActive(false);
        }));
    }

    private System.Collections.IEnumerator FadeTo(float target, float dur, System.Action done)
    {
        float start = _cg.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            _cg.alpha = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        _cg.alpha = target;
        done?.Invoke();
    }
}
