using UnityEngine;

public class FarmArea : MonoBehaviour
{
    [Header("Setup")]
    public Transform[] plotPoints;


    [Header("Refs")]
    public Thuan_23127_JsonReader jsonReader;

    private bool[] isPlanted;

    void Start()
    {
        isPlanted = new bool[plotPoints.Length];
    }
    // Plant all trees
    public void PlantAll(GameObject plantPrefab)
    {
        PlantInternal(plantPrefab, fillAll: true);
    }

    private void PlantInternal(GameObject plantPrefab, bool fillAll)
    {
        if (plantPrefab == null || jsonReader == null) return;

        var tag = plantPrefab.GetComponent<Thuan_23127_SeedTag>(); // get ID
        if (tag == null) { Debug.LogWarning("Prefab thiếu SeedTag."); return; }

        var plantData = jsonReader.GetPlantById(tag.plantId); // get data from ID
        var fishData = jsonReader.GetFishById(tag.fishId); // get data from  fish ID
        var animalData = jsonReader.GetLivestockById(tag.animalId); // get data from animal ID

        if (animalData == null) { Debug.LogWarning("Đây là con vật, không thể trồng ở đây."); return; }
        if (fishData == null) { Debug.LogWarning("Đây là con cá, không thể trồng ở đây."); return; }
        if (plantData == null) { Debug.LogWarning($"Không tìm thấy plant id {tag.plantId} trong JSON."); return; }

        for (int i = 0; i < plotPoints.Length; i++)
        {
            if (!isPlanted[i])
            {
                var p = plotPoints[i];
                var go = Instantiate(plantPrefab, p.position, p.rotation);

                var growth = go.GetComponent<Thuan_23127_PlantGrowth>();
                if (!growth) growth = go.AddComponent<Thuan_23127_PlantGrowth>();

                // when plant all tree, do not pass json to tranh' error
                var readerForThis = fillAll ? null : jsonReader;
                growth.Init(plantData, this, i, readerForThis);

                isPlanted[i] = true;

                if (!fillAll) break;
            }
        }
    }

    public void FreePlot(int index)
    {
        if (index >= 0 && index < isPlanted.Length) isPlanted[index] = false;
    }
    
}
