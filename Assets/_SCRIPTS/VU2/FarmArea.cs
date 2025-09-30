// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
//
// public class FarmArea : MonoBehaviour
// {
//    
//     [Header("Setup")]
//     //public GameObject plantPrefab;       // Prefab cây trồng
//     public Transform[] plotPoints;       // Các ô đất
//     // public Button plantButton;           // Nút trồng cây
//
//     [Header("Refs")]
//     public Thuan_23127_JsonReader jsonReader;    // kéo JsonReader trong scene
//     
//     private bool[] isPlanted;            // Đánh dấu ô nào đã có cây
//
//     void Start()
//     {
//         // Khởi tạo mảng kiểm tra
//         isPlanted = new bool[plotPoints.Length];
//
//         // Gán sự kiện khi nhấn nút
//         // plantButton.onClick.AddListener(Plant);
//     }
//
//     public void Plant(GameObject plantPrefab)
//     {
//         // Tìm ô đất trống đầu tiên
//         // for (int i = 0; i < plotPoints.Length; i++)
//         // {
//         //     if (!isPlanted[i])
//         //     {
//         //         Instantiate(plantPrefab, plotPoints[i].position, Quaternion.identity);
//         //         isPlanted[i] = true;
//         //         //break; // Trồng xong thì dừng
//         //     }
//         // }
//         
//         if (plantPrefab == null || jsonReader == null) return;
//
//         var tag = plantPrefab.GetComponent<Thuan_23127_SeedTag>();
//         if (tag == null) { Debug.LogWarning("Prefab thiếu SeedTag (plantId)."); return; }
//
//         var plantData = jsonReader.GetPlantById(tag.plantId);
//         if (plantData == null) { Debug.LogWarning($"Không tìm thấy plant id {tag.plantId} trong JSON."); return; }
//
//         for (int i = 0; i < plotPoints.Length; i++)
//         {
//             if (!isPlanted[i])
//             {
//                 var p = plotPoints[i];
//                 var go = Instantiate(plantPrefab, p.position, p.rotation);
//
//                 var growth = go.GetComponent<Thuan_23127_PlantGrowth>();
//                 if (!growth) growth = go.AddComponent<Thuan_23127_PlantGrowth>();
//
//                 growth.Init(plantData, this, i, jsonReader); // truyền về để thu hoạch trả ô
//                 isPlanted[i] = true;
//                 break;
//             }
//         }
//     }
//     
//     public void FreePlot(int index)
//     {
//         if (index >= 0 && index < isPlanted.Length) isPlanted[index] = false;
//     }
// }
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public void Plant(GameObject plantPrefab)
    {
        PlantInternal(plantPrefab, fillAll: false);
    }

    //trồng tất cả ô trống
    public void PlantAll(GameObject plantPrefab)
    {
        PlantInternal(plantPrefab, fillAll: true);
    }

    private void PlantInternal(GameObject plantPrefab, bool fillAll)
    {
        if (plantPrefab == null || jsonReader == null) return;

        var tag = plantPrefab.GetComponent<Thuan_23127_SeedTag>();
        if (tag == null) { Debug.LogWarning("Prefab thiếu SeedTag (plantId)."); return; }

        var plantData = jsonReader.GetPlantById(tag.plantId);
        if (plantData == null) { Debug.LogWarning($"Không tìm thấy plant id {tag.plantId} trong JSON."); return; }

        for (int i = 0; i < plotPoints.Length; i++)
        {
            if (!isPlanted[i])
            {
                var p = plotPoints[i];
                var go = Instantiate(plantPrefab, p.position, p.rotation);

                var growth = go.GetComponent<Thuan_23127_PlantGrowth>();
                if (!growth) growth = go.AddComponent<Thuan_23127_PlantGrowth>();

                growth.Init(plantData, this, i, jsonReader);
                isPlanted[i] = true;

                if (!fillAll) break; // như cũ: chỉ trồng 1 ô rồi dừng
                // nếu fillAll=true: tiếp tục vòng for để lấp hết ô trống
            }
        }
    }

    public void FreePlot(int index)
    {
        if (index >= 0 && index < isPlanted.Length) isPlanted[index] = false;
    }
}
