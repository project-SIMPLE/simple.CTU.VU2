using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FarmArea : MonoBehaviour
{
   
    [Header("Setup")]
    //public GameObject plantPrefab;       // Prefab cây trồng
    public Transform[] plotPoints;       // Các ô đất
    // public Button plantButton;           // Nút trồng cây

    private bool[] isPlanted;            // Đánh dấu ô nào đã có cây

    void Start()
    {
        // Khởi tạo mảng kiểm tra
        isPlanted = new bool[plotPoints.Length];

        // Gán sự kiện khi nhấn nút
        // plantButton.onClick.AddListener(Plant);
    }

    //public void Plant(PlantDialogue dialogue)
    //{
    //    // Tìm ô đất trống đầu tiên
    //    for (int i = 0; i < plotPoints.Length; i++)
    //    {
    //        if (!isPlanted[i])
    //        {
    //            GameObject plant =  Instantiate(dialogue.smallPrefab, plotPoints[i].position, Quaternion.identity);
    //            PlantGrowth growth = plant.AddComponent<PlantGrowth>();
    //            growth.plantData = dialogue;
    //            isPlanted[i] = true;
    //            //break; // Trồng xong thì dừng
    //        }
    //    }
    //}
    public void Plant(PlantDialogue dialogue)
    {
        // Tìm ô đất trống đầu tiên
        for (int i = 0; i < plotPoints.Length; i++)
        {
            if (!isPlanted[i])
            {
                // Tạo object rỗng PlantRoot
                GameObject plantRoot = new GameObject("PlantRoot");
                plantRoot.transform.position = plotPoints[i].position;

                // Thêm script PlantGrowth
                PlantGrowth growth = plantRoot.AddComponent<PlantGrowth>();
                growth.plantData = dialogue;

                isPlanted[i] = true;

                PlantProgress progressUI = FindObjectOfType<PlantProgress>();
                if (progressUI != null)
                {
                    progressUI.ShowPlantInfo(growth);
                }
                //break;
            }
        }
    }
    }
