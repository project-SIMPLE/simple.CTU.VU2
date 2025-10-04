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

        var plantData  = (tag.plantId  > 0) ? jsonReader.GetPlantById(tag.plantId)         : null;
        var fishData   = (tag.fishId   > 0) ? jsonReader.GetFishById(tag.fishId)           : null;
        var animalData = (tag.animalId > 0) ? jsonReader.GetLivestockById(tag.animalId)    : null;

        if (plantData == null && fishData == null && animalData == null)
        {
            Debug.LogWarning("Không tìm thấy dữ liệu phù hợp trong JSON (plant/fish/animal).");
            return;
        }

        for (var i = 0; i < plotPoints.Length; i++)
        {
            if (!isPlanted[i])
            {
                var p  = plotPoints[i];
                // var go = Instantiate(plantPrefab, p.position, p.rotation);
                var parent = plotPoints[i];
                var go = Instantiate(plantPrefab, parent);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale    = plantPrefab.transform.localScale;

                var growth = go.GetComponent<Thuan_23127_PlantGrowth>();
                if (!growth) growth = go.AddComponent<Thuan_23127_PlantGrowth>();

                var readerForThis = fillAll ? null : jsonReader;

                // Gọi đúng Init theo loại
                if (plantData != null)       growth.Init(plantData,  this, i, readerForThis);
                else if (animalData != null) growth.Init(animalData, this, i, readerForThis);
                else if (fishData != null)   growth.Init(fishData,   this, i, readerForThis);

                isPlanted[i] = true;
                if (!fillAll) break;
            }
        }
    }
    
    public void FreePlot(int index)
    {
        if (index >= 0 && index < isPlanted.Length) isPlanted[index] = false;
    }
    
    public void ResetAllPlots()
    {
        for (var i = 0; i < plotPoints.Length; i++)
        {
            var p = plotPoints[i];
            if (p == null) continue;

            // Xoá mọi cây đang bám vào plotPoints
            for (var c = p.childCount - 1; c >= 0; c--)
            {
                var child = p.GetChild(c);
                Destroy(child.gameObject);
            }

            isPlanted[i] = false;
        }
    }
}
