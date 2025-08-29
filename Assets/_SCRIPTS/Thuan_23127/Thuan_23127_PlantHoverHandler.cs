using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Thuan_23127_PlantHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int plantID;                         // ID cây
    public GameObject scrollInfoPanel;          // Panel hiển thị
    public Text infoText;                       // Text trong panel

    private Thuan_23127_JsonReader jsonReader;  // Reader lấy dữ liệu từ JSON

    void Start()
    {
        jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();
        if (scrollInfoPanel != null)
            scrollInfoPanel.SetActive(false); // Ẩn lúc đầu
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (jsonReader == null || scrollInfoPanel == null || infoText == null) return;

        var plantList = jsonReader.GetCurrentLangPlants();
        var plant = plantList?.Find(p => p.id == plantID);
        if (plant != null)
        {
            scrollInfoPanel.SetActive(true);
            infoText.text = $"{plant.tag_name}\n" +
                            $"- Growth: {plant.growth_time}\n" +
                            $"- Benefit: {plant.economic_benefits}\n" +
                            $"- Info: {plant.information}";
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (scrollInfoPanel != null)
            scrollInfoPanel.SetActive(false);
    }
}