using System;
using UnityEngine;

public class Thuan_23127_GameManager : MonoBehaviour
{
    public static Thuan_23127_GameManager Instance;

    public int Score { get; private set; } // Lưu điểm hiện tại của người chơi
    public event Action<int> OnScoreChanged;

    [Header("Refs")]
    public Thuan_23127_JsonReader jsonReader;
    
    [Header("Salinity Config")]
    [Tooltip("Độ mặn gốc (‰) – nếu không đọc từ JSON thì dùng giá trị này")]
    public float salinityBase = 1.0f;
    [Tooltip("Hệ số mùa mưa")]
    public float rainyFactor = 0.3f;
    [Tooltip("Hệ số mùa bình thường")]
    public float normalFactor = 1.0f;
    [Tooltip("Hệ số mùa khô")]
    public float dryFactor = 1.5f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip harvestClip;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Độ mặn hiện tại 
    /// </summary>
    /// <returns>(theo mùa)</returns>
    public float GetSeasonSalinity()
    {
        var k = RulesoftheGame_VU2_1.Saltwater_Intrusion switch
        {
            0f => rainyFactor,
            1f => normalFactor,
            2f => dryFactor,
            _  => normalFactor
        };
        return Mathf.Max(0f, salinityBase * k);
    }

    private int ApplySalinityToScore(int baseValue, float plantThreshold)
    {
        float sal = GetSeasonSalinity();
        if (plantThreshold <= 0f) return baseValue;             // không set threshold → không giảm
        if (sal <= plantThreshold) return baseValue;            // chưa vượt ngưỡng → giữ nguyên

        float ratio = Mathf.Clamp01(plantThreshold / sal);      // vượt ngưỡng → giảm theo tỉ lệ
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * ratio));
    }
    
    // Cộng điểm cho plant có nguong mặn 
    public void AddScoreForPlant(int baseValue, Plant plant)
    {
        var value = (plant != null)
            ? ApplySalinityToScore(baseValue, plant.salinity_threshold)
            : baseValue;
        AddScore(value);
    }
    // Cộng điểm cho animal có nguong mặn 
    public void AddScoreForAnimal(int baseValue, Animal animal)
    {
        var value = (animal != null)
            ? ApplySalinityToScore(baseValue, animal.salinity_threshold)
            : baseValue;
        AddScore(value);
    }
    // Cộng điểm cho fish có nguong mặn 
    public void AddScoreForFish(int baseValue, Fish fish)
    {
        var value = (fish != null)
            ? ApplySalinityToScore(baseValue, fish.salinity_threshold)
            : baseValue;
        AddScore(value);
    }

    /// <summary>
    /// Tính điểm 
    /// </summary>
    // sầu riêng (T=0.8):
    // Mùa mưa S=0.30 ⇒ S ≤ T ⇒ factor=1.0 ⇒ econ=4 ⇒ +4 điểm.
    // Mùa khô S=1.50 ⇒ S > T ⇒ factor=0.8/1.5≈0.53 ⇒ 4×0.53≈2.1 ⇒ +2 điểm.
    // </param>
    public void AddScore(int value)
    {
        Score += value;
        audioSource.PlayOneShot(harvestClip);
        OnScoreChanged?.Invoke(Score);

        if (!jsonReader) jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();

        if (!jsonReader) return;
        var l = jsonReader.GetCurrentLangData();
        var scoreLabel = l?.labels?.score ?? "Score";

        if (jsonReader.scoreText)
            jsonReader.scoreText.text = $"{scoreLabel}: {Score}";
        if (jsonReader.scoreTextEndGame)
            jsonReader.scoreTextEndGame.text = $"{scoreLabel}: {Score}";
    }

    /// <summary>
    /// Reset điểm về
    /// </summary>
    public void ResetScore()
    {
        Score = 0;

        if (!jsonReader)
            jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();

        if (jsonReader)
        {
            var l = jsonReader.GetCurrentLangData();
            var scoreLabel = l?.labels?.score ?? "Score";

            if (jsonReader.scoreText)
                jsonReader.scoreText.text = $"{scoreLabel}: {Score}";
            if (jsonReader.scoreTextEndGame)
                jsonReader.scoreTextEndGame.text = $"{scoreLabel}: {Score}";
        }

        OnScoreChanged?.Invoke(Score);
    }
}