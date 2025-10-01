using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class Thuan_23127_PlantGrowth : MonoBehaviour
{
    [Header("Progress UI (dùng chung cho grow & harvest)")]
    public Image progressFill;
    public TextMeshProUGUI progressPercentText;

    [Header("XR (Harvest optional)")]
    public XRSimpleInteractable harvestInteractable;

    [Header("Timing")]
    [SerializeField] private float destroyDelaySeconds = 30f; 

    private Fish _fishData; // ca 

    private Plant _plantData;
    private float _growTotal;
    private float _growElapsed;
    private bool  _growing, _ready;
    private bool  _harvested;
    private bool  _harvesting;
    private Coroutine _harvestCo;

    private FarmArea _ownerArea;
    private int _ownerIndex = -1;
    private Thuan_23127_JsonReader _jsonReader;

    public void Init(Plant data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = data;
        _ownerArea  = area;
        _ownerIndex = plotIndex;
        _jsonReader = reader;

        _growTotal = Mathf.Max(0.01f, data.growth_time);

        if (!harvestInteractable) harvestInteractable = GetComponent<XRSimpleInteractable>();
        if (harvestInteractable)
        {
            harvestInteractable.selectEntered.RemoveAllListeners();
            harvestInteractable.selectEntered.AddListener(_ => { TryStartHarvest(); });
        }

        _growing    = true;
        _ready      = false;
        _harvested  = false;
        _harvesting = false;

        UpdateUI(0f);
        StartCoroutine(CoGrow());
    }

    private IEnumerator CoGrow()
    {
        _growElapsed = 0f;
        while (_growElapsed < _growTotal)
        {
            _growElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_growElapsed / _growTotal);
            UpdateUI(t);
            yield return null;
        }

        _growing = false;
        _ready   = true;

        // Auto 
        TryStartHarvest();
    }

    private void OnMouseDown()
    {
        TryStartHarvest();
    }

    public void HarvestNow()
    {
        TryStartHarvest();
    }

    private void TryStartHarvest()
    {
        if (!_ready || _harvested || _harvesting) return;
        if (_harvestCo != null) StopCoroutine(_harvestCo);
        _harvestCo = StartCoroutine(CoHarvest());
    }

    private IEnumerator CoHarvest()
    {
        _harvesting = true;

        float hTime = (_plantData != null && _plantData.harvest_time > 0f) 
                        ? _plantData.harvest_time : 2f;

        if (harvestInteractable) harvestInteractable.enabled = false;

        // Reset UI to show thu hoach
        UpdateUI(0f);

        float e = 0f;
        while (e < hTime)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / hTime);
            UpdateUI(t);
            yield return null;
        }

        FinalizeHarvest();
    }

    private void FinalizeHarvest()
    {
        if (_harvested) return;

        _harvested  = true;
        _ready      = false;
        _harvesting = false;

        var gm = Thuan_23127_GameManager.Instance;
        if (gm && _plantData != null)
            gm.AddScore(_plantData.economic_benefits);

        if (harvestInteractable)
        {
            harvestInteractable.enabled = false;
            harvestInteractable.selectEntered.RemoveAllListeners();
        }

        // Ẩn UI sau khi xong
        if (progressFill) progressFill.enabled = false;
        if (progressPercentText) progressPercentText.gameObject.SetActive(false);

        StartCoroutine(CoDestroyAfter(destroyDelaySeconds));
    }

    private IEnumerator CoDestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_ownerArea) _ownerArea.FreePlot(_ownerIndex);
        Destroy(gameObject);
    }

    private void UpdateUI(float t)
    {
        if (progressFill) progressFill.fillAmount = t;
        if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";
    }
}
