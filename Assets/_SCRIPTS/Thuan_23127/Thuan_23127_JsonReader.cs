using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
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
        var resourceName = Path.GetFileNameWithoutExtension(fileName);
        var jsonFile = Resources.Load<TextAsset>(resourceName);
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
            case 2: currentLang = "an"; break;
            case 3: currentLang = "tl"; break;
            default: currentLang = "en"; break;
        }
        ApplyLanguage();
    }

    public Lang GetCurrentLangData()
    {
        if (root == null) return null;
        var fi = typeof(Root).GetField(currentLang, BindingFlags.Public | BindingFlags.Instance);
        if (fi != null && fi.GetValue(root) is Lang langObj) return langObj;
        if (root.en != null) return root.en;
        if (root.vi != null) return root.vi;
        return null;
    }

    public string GetCurrentLangCode() => string.IsNullOrEmpty(currentLang) ? "en" : currentLang;

    public List<Plant>  GetCurrentLangPlants()    => GetCurrentLangData()?.plants;
    public List<Animal> GetCurrentLangAnimals()   => GetCurrentLangData()?.livestock;
    public List<Fish>   GetCurrentLangFish()      => GetCurrentLangData()?.fish;

    // ======= theo ID =======
    public Plant  GetPlantById(int id)     => GetCurrentLangPlants()?.FirstOrDefault(p => p.id == id);
    public Animal GetLivestockById(int id) => GetCurrentLangAnimals()?.FirstOrDefault(a => a.id == id);
    public Fish   GetFishById(int id)      => GetCurrentLangFish()?.FirstOrDefault(f => f.id == id);

    private void ApplyLanguage()
    {
        if (root == null) return;
        var l = GetCurrentLangData();
        if (l == null) return;

        if (infoText)  infoText.text  = l.labels?.info  ?? "INFO";
        if (nameText)  nameText.text  = $"{l.labels?.name ?? "Name"}: {l.gameplay?.name}";
        if (levelText) levelText.text = $"{l.labels?.level ?? "Level"}: {l.gameplay?.level}";

        if (!scoreText) return;
        var gm = Thuan_23127_GameManager.Instance;
        var label = l.labels?.score ?? "Score";
        var currentScore = gm ? gm.Score : 0;
        Debug.Log(label);
        scoreText.text = $"{label}: {currentScore}";
    }
}
