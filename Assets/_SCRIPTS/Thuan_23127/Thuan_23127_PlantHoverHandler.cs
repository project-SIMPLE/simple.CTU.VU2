using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;

public enum EntityType
{
    Auto = 0,     // Tự dò cả 3
    Plant = 1,    // Cây trồng
    Livestock = 2,// Vật nuôi
    Fish = 3      // Thủy sản
}

public class Thuan_23127_PlantHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Config")]
    public int id;                       // ID dùng chung cho mọi loại
    public EntityType type = EntityType.Auto; // 0=Auto, 1=Plant, 2=Livestock, 3=Fish

    [Header("UI")]
    public GameObject scrollInfoPanel;   // Panel hiển thị
    public Text headText;                // Tiêu đề
    public Text infoText;                // Nội dung

    private Thuan_23127_JsonReader jsonReader;  // Reader lấy dữ liệu từ JSON

    private void Start()
    {
        jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();
        scrollInfoPanel?.SetActive(false);
    }

    private string LocalizeGroupLabelHardcoded(string langCode, EntityType t)
    {
        bool vi = (langCode == "vi");
        switch (t)
        {
            case EntityType.Plant:     return vi ? "Cây"       : "Plant";
            case EntityType.Livestock: return vi ? "Vật nuôi"  : "Livestock";
            case EntityType.Fish:      return vi ? "Thủy sản"  : "Fish";
            default:                   return vi ? "Thông tin" : "Info";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (jsonReader == null || scrollInfoPanel == null || infoText == null) return;

        var lang = jsonReader.GetCurrentLangData();
        if (lang == null || lang.interpretation == null) return;

        var fields = lang.interpretation.fields;
        var units  = lang.interpretation.units;
        var langCode = jsonReader.GetCurrentLangCode();

        // Gom nhiều kết quả nếu trùng ID ở nhiều type
        var sbBody = new StringBuilder();
        var sbHead = new StringBuilder();

        void AppendBlock(string groupLabel, string tag_name, int growth_time, int economic_benefits, string information)
        {
            if (sbHead.Length > 0) sbHead.Append(" | ");
            sbHead.Append($"{tag_name}");

            if (sbBody.Length > 0) sbBody.AppendLine().AppendLine("----------------");

            sbBody.AppendLine($"- {fields.growth_time}: {growth_time} {units.growth_time}")
                 .AppendLine($"- {fields.economic_benefits}: {economic_benefits}")
                 .Append($"- {fields.information}: {information}");
        }

        bool foundAny = false;

        // Theo "type" Auto scan tất cả
        if (type == EntityType.Plant || type == EntityType.Auto)
        {
            var p = jsonReader.GetPlantById(id);
            if (p != null)
            {
                var label = LocalizeGroupLabelHardcoded(langCode, EntityType.Plant);
                AppendBlock(label, p.tag_name, p.growth_time, p.economic_benefits, p.information);
                foundAny = true;
            }
        }

        if (type == EntityType.Livestock || type == EntityType.Auto)
        {
            var a = jsonReader.GetLivestockById(id);
            if (a != null)
            {
                var label = LocalizeGroupLabelHardcoded(langCode, EntityType.Livestock);
                AppendBlock(label, a.tag_name, a.growth_time, a.economic_benefits, a.information);
                foundAny = true;
            }
        }

        if (type == EntityType.Fish || type == EntityType.Auto)
        {
            var f = jsonReader.GetFishById(id);
            if (f != null)
            {
                var label = LocalizeGroupLabelHardcoded(langCode, EntityType.Fish);
                AppendBlock(label, f.tag_name, f.growth_time, f.economic_benefits, f.information);
                foundAny = true;
            }
        }

        if (foundAny)
        {
            scrollInfoPanel.SetActive(true);
            if (headText) headText.text = sbHead.ToString();
            infoText.text = sbBody.ToString();
        }
        else
        {
            scrollInfoPanel.SetActive(false);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        scrollInfoPanel?.SetActive(false);
    }
}
