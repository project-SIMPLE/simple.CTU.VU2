using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;      // Thư viện TextMeshPro
using System.IO;


public class JsonReader : MonoBehaviour
{
	[Header("UI Setup")]
    public Text sceneNameText;
    public Text levelText;
    public Text scoreText;
	
    [Header("Config")]
    public string fileName = "data.json";

    void Start()
    {
        // Đọc dữ liệu khi game bắt đầu
        PlayerData data = LoadJson();
        if (data != null)
        {
            Debug.Log("Tên màn chơi: " + data.sceneName);
            Debug.Log("Level: " + data.level);
            Debug.Log("Score: " + data.score);
			if (sceneNameText != null) sceneNameText.text = "" + data.sceneName;
			if (levelText != null) levelText.text = "Level: " + data.level;
			if (scoreText != null) scoreText.text = "Score: " + data.score;
        }
    }

    PlayerData LoadJson()
    {
        // Xác định đường dẫn file (Assets/StreamingAssets/data.json)
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (File.Exists(path))
        {
            string jsonString = File.ReadAllText(path);
            PlayerData data = JsonUtility.FromJson<PlayerData>(jsonString);
            return data;
        }
        else
        {
            Debug.LogError("Không tìm thấy file JSON tại: " + path);
            return null;
        }
    }
	
}
