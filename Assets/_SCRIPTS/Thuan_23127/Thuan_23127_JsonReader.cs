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
    public string fileName = "data";
    public string currentLang = "en";
    public Root root;
    private string jsonString;


    protected virtual void Start()
    {
        string resourceName = Path.GetFileNameWithoutExtension(fileName);
        TextAsset jsonFile = Resources.Load<TextAsset>(resourceName);
        // if (jsonFile == null)
        // {
        //     Debug.LogError($"Không tìm thấy file JSON trong Resources: {resourceName}");
        //     return;
        // }
        jsonString = jsonFile.text;
        Debug.Log(jsonString);
        root = JsonUtility.FromJson<Root>(jsonString);
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
                break;
        }
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

        if (infoText)  infoText.text  = l.labels?.info ?? "INFO";
        if (nameText)  nameText.text  = $"{l.labels?.name ?? "Name"}:  {l.gameplay?.name}";
        if (levelText) levelText.text = $"{l.labels?.level ?? "Level"}: {l.gameplay?.level}";
        if (scoreText) scoreText.text = $"{l.labels?.score ?? "Score"}: {l.gameplay?.score}";
    }
}
