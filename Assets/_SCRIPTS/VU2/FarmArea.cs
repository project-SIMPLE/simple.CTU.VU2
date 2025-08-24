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

    public void Plant(GameObject plantPrefab)
    {
        // Tìm ô đất trống đầu tiên
        for (int i = 0; i < plotPoints.Length; i++)
        {
            if (!isPlanted[i])
            {
                Instantiate(plantPrefab, plotPoints[i].position, Quaternion.identity);
                isPlanted[i] = true;
                //break; // Trồng xong thì dừng
            }
        }
    }
}
