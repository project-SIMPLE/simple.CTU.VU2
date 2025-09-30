// using System.Collections;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.XR.Interaction.Toolkit;
//
// public class Thuan_23127_PlantGrowth : MonoBehaviour
// {
//     [Header("Progress UI")]
//     public Image progressFill;
//     public TextMeshProUGUI progressPercentText;
//
//     [Header("XR (Harvest optional)")]
//     public XRSimpleInteractable harvestInteractable;
//
//     private Plant _plantData;
//     private float _totalTime;
//     private float _elapsed;
//     private bool  _growing, _ready;
//     private bool  _harvested; // NEW: để đảm bảo cộng điểm 1 lần
//
//     private FarmArea _ownerArea;
//     private int _ownerIndex = -1;
//     private Thuan_23127_JsonReader _jsonReader;
//
//     public void Init(Plant data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
//     {
//         _plantData  = data;
//         _ownerArea  = area;
//         _ownerIndex = plotIndex;
//         _jsonReader = reader;
//
//         _totalTime = Mathf.Max(1f, data.growth_time);
//
//         if (!harvestInteractable) harvestInteractable = GetComponent<XRSimpleInteractable>();
//         if (harvestInteractable)
//         {
//             harvestInteractable.selectEntered.RemoveAllListeners();
//             harvestInteractable.selectEntered.AddListener(_ => { TryHarvest(); });
//         }
//
//         _growing   = true;
//         _ready     = false;
//         _harvested = false;
//         StartCoroutine(CoGrow());
//     }
//
//     private IEnumerator CoGrow()
//     {
//         _elapsed = 0f;
//         while (_elapsed < _totalTime)
//         {
//             _elapsed += Time.deltaTime;
//             float t = Mathf.Clamp01(_elapsed / _totalTime);
//
//             if (progressFill)        progressFill.fillAmount = t;
//             if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";
//
//             yield return null;
//         }
//
//         _growing = false;
//         _ready   = true;
//
//         TryHarvest();
//     }
//
//     private void TryHarvest()
//     {
//         if (!_ready || _harvested) return;
//
//         _harvested = true; 
//
//         var gm = Thuan_23127_GameManager.Instance;
//         if (gm && _plantData != null)
//         {
//             gm.AddScore(_plantData.economic_benefits);
//         }
//
//         // Giải phóng ô và huỷ cây
//         if (_ownerArea) _ownerArea.FreePlot(_ownerIndex);
//         Destroy(gameObject);
//     }
// }

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class Thuan_23127_PlantGrowth : MonoBehaviour
{
    [Header("Progress UI")]
    public Image progressFill;
    public TextMeshProUGUI progressPercentText;

    [Header("XR (Harvest optional)")]
    public XRSimpleInteractable harvestInteractable;

    [Header("Timing")]
    [SerializeField] private float destroyDelaySeconds = 60f; 

    private Plant _plantData;
    private float _totalTime;
    private float _elapsed;
    private bool  _growing, _ready;
    private bool  _harvested;

    private FarmArea _ownerArea;
    private int _ownerIndex = -1;
    private Thuan_23127_JsonReader _jsonReader;

    public void Init(Plant data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = data;
        _ownerArea  = area;
        _ownerIndex = plotIndex;
        _jsonReader = reader;

        _totalTime = Mathf.Max(1f, data.growth_time);

        if (!harvestInteractable) harvestInteractable = GetComponent<XRSimpleInteractable>();
        if (harvestInteractable)
        {
            harvestInteractable.selectEntered.RemoveAllListeners();
            harvestInteractable.selectEntered.AddListener(_ => { TryHarvest(); });
        }

        _growing   = true;
        _ready     = false;
        _harvested = false;
        StartCoroutine(CoGrow());
    }

    private IEnumerator CoGrow()
    {
        _elapsed = 0f;
        while (_elapsed < _totalTime)
        {
            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / _totalTime);

            if (progressFill)        progressFill.fillAmount = t;
            if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";

            yield return null;
        }

        _growing = false;
        _ready   = true;

        TryHarvest(); // auto-harvest khi chín (an toàn nhờ _harvested)
    }

    private void TryHarvest()
    {
        if (!_ready || _harvested) return;

        _harvested = true;       // chặn double-score
        _ready     = false;

        // Cộng điểm ngay
        var gm = Thuan_23127_GameManager.Instance;
        if (gm && _plantData != null)
            gm.AddScore(_plantData.economic_benefits);

        // Khoá tương tác & UI trong thời gian chờ huỷ
        if (harvestInteractable)
        {
            harvestInteractable.enabled = false;
            harvestInteractable.selectEntered.RemoveAllListeners();
        }
        if (progressFill)        progressFill.enabled = false;
        if (progressPercentText) progressPercentText.gameObject.SetActive(false);

        // (tuỳ chọn) bật hiệu ứng "đã thu hoạch

        // Huỷ sau 60 giây và lúc đó mới FreePlot
        StartCoroutine(CoDestroyAfter(destroyDelaySeconds));
    }

    private IEnumerator CoDestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_ownerArea) _ownerArea.FreePlot(_ownerIndex);
        Destroy(gameObject);
    }
}
