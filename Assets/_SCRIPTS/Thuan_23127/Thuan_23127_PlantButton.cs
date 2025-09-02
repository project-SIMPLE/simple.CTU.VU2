using UnityEngine;

public class Thuan_23127_PlantButton : MonoBehaviour
{
    public int plantID;                 // ID trong JSON (bắt đầu từ 1)
    public string plantName;            // (không bắt buộc) dùng khi tìm theo tag_name
    public GameObject plantPrefab;      // Prefab cây trồng
    public Thuan_23127_FarmArea farmArea;
    public Thuan_23127_JsonReader jsonReader;

    public void OnPlantButtonClicked()
    {
        if (jsonReader == null || farmArea == null || plantPrefab == null)
            return;

        var list = jsonReader.GetCurrentLangPlants();
        if (list == null || list.Count == 0)
        {
            // Không có JSON -> vẫn trồng, chết sau 100s (FarmArea xử lý khi plantData = null)
            farmArea.Plant(plantPrefab, null);
            return;
        }

        if (plantID <= 0 || plantID > list.Count)
            return;

        var plantData = list[plantID - 1]; // JSON id bắt đầu từ 1
        farmArea.Plant(plantPrefab, plantData);
    }

    
    /*
    public void OnPlantByNameClicked()
    {
        if (jsonReader == null || farmArea == null || plantPrefab == null)
            return;

        var plants = jsonReader.GetCurrentLangPlants();
        if (plants == null) return;

        var plant = plants.Find(p => p.tag_name.ToLower().Contains(plantName.ToLower()));
        if (plant == null) return;

        farmArea.Plant(plantPrefab, plant);
    }
    */
}