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

    private Plant _plantData;
    private float _totalTime;   // giây
    private float _elapsed;
    private bool  _growing, _ready;

    private FarmArea _ownerArea;
    private int _ownerIndex = -1;
    
    private Thuan_23127_JsonReader _jsonReader;

    public void Init(Plant data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = data;
        _ownerArea  = area;
        _ownerIndex = plotIndex;
        _jsonReader = reader;

        _totalTime = Mathf.Max(1f, data.growth_time); // 180 -> 180s

        if (!harvestInteractable) harvestInteractable = GetComponent<XRSimpleInteractable>();
        if (harvestInteractable)
        {
            harvestInteractable.selectEntered.RemoveAllListeners();
            harvestInteractable.selectEntered.AddListener(_ => { TryHarvest(); });
        }

        if (progressFill)        progressFill.fillAmount = 0f;
        if (progressPercentText) progressPercentText.text = "0%";

        _growing = true;
        _ready   = false;
        StartCoroutine(CoGrow());
    }

    private IEnumerator CoGrow()
    {
        _elapsed = 0f;
        while (_elapsed < _totalTime)
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _totalTime);

            if (progressFill)        progressFill.fillAmount = t;
            if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";

            yield return null;
        }

        _growing = false;
        _ready   = true;

        //auto-harvest tự tính điểm
        TryHarvest();
    }

    private void TryHarvest()
    {
        if (!_ready) return;

        var gm = Thuan_23127_GameManager.Instance;
        if (gm && _plantData != null)
        {
            //plantId để chỉ cộng 1 lần cho mỗi loại
            gm.AddScoreForPlant(_plantData.id, _plantData.economic_benefits);
        }

        if (_ownerArea) _ownerArea.FreePlot(_ownerIndex);
        Destroy(gameObject);
    }
}
