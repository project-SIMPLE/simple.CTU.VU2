using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class Thuan_23127_PlantGrowth : MonoBehaviour
{
    [Header("Progress UI")]
    public Image progressFill;
    public TextMeshProUGUI progressPercentText;  
    public TextMeshProUGUI econGainText; 

    [Header("XR (Harvest optional)")]
    public XRSimpleInteractable harvestInteractable;

    private Plant _plantData;
    private float _totalTime;   // giây
    private float _elapsed;
    private bool _growing, _ready;

    // link trả ô đất & json cho label, score…
    private FarmArea _ownerArea;  private int _ownerIndex = -1;
    private Thuan_23127_JsonReader _jsonReader;

    public void Init(Plant data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData = data;
        _ownerArea = area;
        _ownerIndex = plotIndex;
        _jsonReader = reader;

        _totalTime = Mathf.Max(1f, data.growth_time); // JSON dùng giây

        if (!harvestInteractable) harvestInteractable = GetComponent<XRSimpleInteractable>();
        if (harvestInteractable)
        {
            harvestInteractable.enabled = false;               // chỉ bật khi sẵn sàng
            harvestInteractable.selectEntered.AddListener(_ => TryHarvest());
        }

        // Khởi tạo UI
        if (progressFill) progressFill.fillAmount = 0f;
        if (progressPercentText) progressPercentText.text = FormatTimeAndPercent(0f); // 0%
        if (econGainText) econGainText.gameObject.SetActive(false);

        // (tuỳ thích) đổi NameText trên HUD thành tên cây đang trồng
        if (_jsonReader && _jsonReader.nameText)
        {
            var l = _jsonReader.GetCurrentLangData();
            string label = l?.labels?.name ?? "Name";
            _jsonReader.nameText.text = $"{label}: {_plantData.tag_name}";
        }

        _elapsed = 0f; _growing = true; _ready = false;
        StartCoroutine(Grow());
    }

    IEnumerator Grow()
    {
        while (_growing && _elapsed < _totalTime)
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _totalTime);

            if (progressFill) progressFill.fillAmount = t;
            if (progressPercentText) progressPercentText.text = FormatTimeAndPercent(t);

            yield return null;
        }
        OnGrowthDone();
    }

    string FormatTimeAndPercent(float t01)
    {
        int pct = Mathf.RoundToInt(t01 * 100f);
        float remain = Mathf.Max(0f, _totalTime - _elapsed);
        int remainInt = Mathf.CeilToInt(remain);
        // ví dụ: "73% (52s)"
        return $"{pct}% ({remainInt}s)";
    }

    void OnGrowthDone()
    {
        _growing = false;
        _ready = true;

        bool autoHarvest = false; // đổi = true nếu muốn tự thu hoạch
        if (harvestInteractable) harvestInteractable.enabled = true;
    }

    public void TryHarvest()
    {
        if (!_ready) return;

        // + điểm theo economic_benefits
        Thuan_23127_GameManager.Instance?.AddScore(_plantData.economic_benefits);

        // Popup lợi ích kinh tế (optional)
        if (econGainText)
        {
            econGainText.text = $"+{_plantData.economic_benefits}";
            econGainText.gameObject.SetActive(true);
            // Ẩn sau 1.2s
            StartCoroutine(HideAfter(econGainText.gameObject, 1.2f));
        }

        _ownerArea?.FreePlot(_ownerIndex);
        Destroy(gameObject, 0.05f); // cho phép TMP hiển thị 1 frame nếu muốn
    }

    IEnumerator HideAfter(GameObject go, float sec)
    {
        yield return new WaitForSeconds(sec);
        if (go) go.SetActive(false);
    }
}
