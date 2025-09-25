using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Thuan_23127_PlantHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int plantID;                         // ID cây
    public GameObject scrollInfoPanel;          // Panel hiển thị
    public Text headText;                       // Head Text panel
    public Text infoText;                       // Text trong panel

    private Thuan_23127_JsonReader jsonReader;  // Reader lấy dữ liệu từ JSON

    private void Start()
    {
        jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();
        scrollInfoPanel?.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (jsonReader == null || scrollInfoPanel == null || infoText == null) return;

        Lang lang = jsonReader.GetCurrentLangData();
        if (lang == null || lang.interpretation == null) return;

        var fields = lang.interpretation.fields;
        var units  = lang.interpretation.units;

        var plantList = jsonReader.GetCurrentLangPlants();
        var plant = plantList?.Find(p => p.id == plantID);
        if (plant != null)
        {
            scrollInfoPanel.SetActive(true);
            if (headText) headText.text = plant.tag_name;

            infoText.text =
                $"- {fields.growth_time}: {plant.growth_time} {units.growth_time}\n" +
                $"- {fields.economic_benefits}: {plant.economic_benefits}\n" +
                $"- {fields.information}: {plant.information}";
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        scrollInfoPanel?.SetActive(false);
    }
}