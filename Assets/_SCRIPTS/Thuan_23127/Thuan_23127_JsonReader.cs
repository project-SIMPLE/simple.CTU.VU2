using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Thuan_23127_JsonReader : MonoBehaviour
{
    [Header("UI Setup")]
    public Text nameText; 
    public Text levelText;   
    public Text scoreText;   
    public Text infoText;   
    
    [Header("Config")]
    public string fileName = "data.json";
    [Tooltip("en hoặc vi")]
    public string currentLang = "en"; // Default

    private Root _root;

    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError("Không tìm thấy file JSON tại: " + path);
            return;
        }

        var jsonString = File.ReadAllText(path);
        _root = JsonUtility.FromJson<Root>(jsonString);
        if (_root == null) { Debug.LogError("Parse JSON thất bại"); return; }

        ApplyLanguage();
    }
    
    public void SetLanguageByIndex(int index)
    {
        switch (index)
        {
            case 0:
                currentLang = "en"; // English
                break;
            case 1:
                currentLang = "vi"; // Vietnamese
                break;
            // case 2:
            //     currentLang = "fr"; // French
            //     break;
            // case 3:
            //     currentLang = "jp"; // Japanese
            //     break;
            default:
                currentLang = "en"; // Default fallback
                Debug.LogWarning("Index không hợp lệ, set mặc định: en");
                break;
        }

        Debug.Log($"[JsonReader] Language changed to: {currentLang}");
        ApplyLanguage();
    }

    public List<Plant> GetCurrentLangPlants()
    {
        Lang lang = (currentLang == "en") ? _root.en : _root.vi;
        return lang?.plants;
    }

    private void ApplyLanguage()
    {
        if (_root == null) return;

        Lang L = (currentLang == "en") ? _root.en : _root.vi;
        if (L == null) { Debug.LogWarning("Thiếu nhánh lang: " + currentLang); return; }

        var info  = L.labels?.info;
        var name  = L.labels?.name;
        var level = L.labels?.level;
        var score = L.labels?.score;

        var n   = L.gameplay?.name;   
        var lvl = L.gameplay?.level;
        var sc  = L.gameplay?.score;

        if (infoText)  infoText.text  = info;
        if (nameText)  nameText.text  = $"{name}:  {n}";
        if (levelText) levelText.text = $"{level}: {lvl}";
        if (scoreText) scoreText.text = $"{score}: {sc}";
    }
}