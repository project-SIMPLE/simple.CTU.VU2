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

        _totalTime = Mathf.Max(1f, data.growth_time); // ví dụ: 180 -> 180s

        if (!harvestInteractable) harvestInteractable = GetComponent<XRSimpleInteractable>();
        if (harvestInteractable)
        {
            harvestInteractable.selectEntered.RemoveAllListeners();
            harvestInteractable.selectEntered.AddListener(_ => { TryHarvest(); });
        }

        // Bắt đầu tăng trưởng
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

            if (progressFill)         progressFill.fillAmount = t;
            if (progressPercentText)  progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";

            yield return null;
        }

        _growing = false;
        _ready   = true;
        // Nếu bạn muốn auto-harvest, gọi TryHarvest() ở đây,
        TryHarvest();
        // còn nếu muốn người chơi tương tác thì để trống.
    }

    private void TryHarvest()
    {
        if (!_ready) return;

        // CỘNG ĐIỂM theo economic_benefits
        var gm = Thuan_23127_GameManager.Instance;
        if (gm != null && _plantData != null)
        {
            gm.AddScore(_plantData.economic_benefits);
        }

        // Giải phóng ô và huỷ cây
        if (_ownerArea != null) _ownerArea.FreePlot(_ownerIndex);
        Destroy(gameObject);
    }
}
