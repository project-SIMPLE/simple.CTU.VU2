using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    public string currentLang = "en";

    public Root root;

    protected virtual void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!File.Exists(path))
        {
            // Debug.LogError("Không tìm thấy file JSON tại: " + path);
            return;
        }

        var jsonString = File.ReadAllText(path);
        root = JsonUtility.FromJson<Root>(jsonString);
        // if (Root == null) { Debug.LogError("Parse JSON thất bại"); return; }

        ApplyLanguage();
    }
    
    public void SetLanguageByIndex(int index)
    {
        switch (index)
        {
            case 0: currentLang = "en"; break;
            case 1: currentLang = "vi"; break;
            // case 2: currentLang = "fr"; break;
            // case 3: currentLang = "th"; break;
            default:
                currentLang = "en";
                // Debug.Log("Index không hợp lệ, set mặc định: en");
                break;
        }
        // Debug.Log($"[JsonReader] Language changed to: {currentLang}");
        ApplyLanguage();
    }

    
    public Lang GetCurrentLangData()
    {
        if (root == null) return null;

        var fi = typeof(Root).GetField(currentLang, BindingFlags.Public | BindingFlags.Instance);
        if (fi != null)
        {
            if (fi.GetValue(root) is Lang langObj) return langObj;
        }

        if (root.en != null) return root.en;

        if (root.vi != null) return root.vi;

        // if (Root.fr != null) return Root.fr;
        // if (Root.th != null) return Root.th;

        // Debug.Log("Không tìm thấy ngôn ngữ phù hợp ");
        return null;
    }

    /// <summary>
    /// // Lấy mỗi plan -> Cần được cải tiến chưa biết cải tiến như nào ??
    /// </summary>
    /// <returns>lang -> plants</returns>
    public List<Plant> GetCurrentLangPlants()
    {
        var lang = GetCurrentLangData();
        return lang?.plants;
    }

    private void ApplyLanguage()
    {
        if (root == null) return;

        var l = GetCurrentLangData();
        // if (L == null) { Debug.LogWarning("Thiếu dữ liệu cho ngôn ngữ: " + currentLang); return; }

        if (infoText)  infoText.text  = l.labels?.info ?? "INFO";
        if (nameText)  nameText.text  = $"{(l.labels?.name ?? "Name")}:  {l.gameplay?.name}";
        if (levelText) levelText.text = $"{(l.labels?.level ?? "Level")}: {l.gameplay?.level}";
        if (scoreText) scoreText.text = $"{(l.labels?.score ?? "Score")}: {l.gameplay?.score}";
    }
}
