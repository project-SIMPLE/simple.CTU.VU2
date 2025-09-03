using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPlantDialogue", menuName = "Plant Dialogue")]
public class PlantDialogue : ScriptableObject
{
    [Header("Thông tin chung")]
    public int id;              // id cây
    public float saltTolerance;           // Độ chịu mặn

    [Header("Giai đoạn phát triển")]
    public GameObject smallPrefab;
    public float timeStage1;              // Thời gian phát triển từ nhỏ -> vừa

    public GameObject mediumPrefab;       // Prefab vừa
    public float timeStage2;              // Thời gian phát triển từ vừa -> lớn

    public GameObject largePrefab;        // Prefab lớn

    [Header("Trạng thái đặc biệt")]
    public GameObject sickPrefab;
    public GameObject deadPrefab;         // Prefab khi cây chết
}
