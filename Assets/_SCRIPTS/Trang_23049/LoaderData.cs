using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LoaderData : MonoBehaviour
{
    [Header("Config")]
    public string fileName = "data.json";

    private string language = "en";

    void Start()
    {
        List<PlantData> data = LoadJson();

        foreach (var plant in data)
        {
            Debug.Log($"ID: {plant.id}, Name: {plant.tag_name}, Info: {plant.information}");
        }
    }

    // Setter để đổi ngôn ngữ từ script khác
    public void SetLanguage(string lang)
    {
        language = lang;
    }

    // Getter để lấy ngôn ngữ hiện tại
    public string GetLanguage()
    {
        return language;
    }

    public List<PlantData> LoadJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (File.Exists(path))
        {
            string jsonString = File.ReadAllText(path);
            RootData root = JsonUtility.FromJson<RootData>(jsonString);

            if (language == "vi" && root.vi != null)
            {
                return root.vi.plants;
            }
            else if (language == "en" && root.en != null)
            {
                return root.en.plants;
            }
            else
            {
                Debug.LogError("Ngôn ngữ không hợp lệ hoặc dữ liệu trống!");
                return new List<PlantData>();
            }
        }
        else
        {
            Debug.LogError("Không tìm thấy file JSON tại: " + path);
            return new List<PlantData>();
        }
    }
}
