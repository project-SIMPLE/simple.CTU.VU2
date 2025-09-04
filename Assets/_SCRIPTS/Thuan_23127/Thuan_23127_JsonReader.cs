using System.Collections.Generic;
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
    public string fileName = "data";  // bắt buộc phải bỏ đuôi json mới nhận file
    public string currentLang = "en";

    public Root root;

    protected virtual void Start()
    {
        LoadJsonFromResources();
        ApplyLanguage();
    }

    private void LoadJsonFromResources()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(fileName);
        if (jsonFile == null)
        {
            // Debug.LogError("[JsonReader] Không tìm thấy file trong Resources: " + fileName);
            return;
        }

        root = JsonUtility.FromJson<Root>(jsonFile.text);
        if (root == null)
        {
            // Debug.LogError("[JsonReader] Parse JSON thất bại");
        }
    }

    public void SetLanguageByIndex(int index)
    {
        switch (index)
        {
            case 0: currentLang = "en"; break;
            case 1: currentLang = "vi"; break;
            default: currentLang = "en"; break;
        }
        ApplyLanguage();
    }

    public Lang GetCurrentLangData()
    {
        if (root == null) return null;

        var fi = typeof(Root).GetField(currentLang,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (fi != null && fi.GetValue(root) is Lang langObj) return langObj;

        return root.en ?? root.vi;
    }

    public List<Plant> GetCurrentLangPlants()
    {
        var lang = GetCurrentLangData();
        return lang?.plants;
    }

    private void ApplyLanguage()
    {
        if (root == null) return;
        var l = GetCurrentLangData();
        if (l == null) return;

        if (infoText)  infoText.text  = l.labels?.info ?? "INFO";
        if (nameText)  nameText.text  = $"{(l.labels?.name ?? "Name")}:  {l.gameplay?.name}";
        if (levelText) levelText.text = $"{(l.labels?.level ?? "Level")}: {l.gameplay?.level}";
        if (scoreText) scoreText.text = $"{(l.labels?.score ?? "Score")}: {l.gameplay?.score}";
    }
}
