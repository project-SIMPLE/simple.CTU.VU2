using System;
using UnityEngine;

public class Thuan_23127_GameManager : MonoBehaviour
{
    public static Thuan_23127_GameManager Instance;

    public int Score { get; private set; }
    public event Action<int> OnScoreChanged;

    [Header("Refs")]
    public Thuan_23127_JsonReader jsonReader;

    private bool hasCountedFirstPlant = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddScore(int value)
    {
        if (hasCountedFirstPlant) return;   

        Score += value;
        hasCountedFirstPlant = true;

        OnScoreChanged?.Invoke(Score);

        if (jsonReader && jsonReader.scoreText)
        {
            var l = jsonReader.GetCurrentLangData();
            var scoreLabel = l?.labels?.score ?? "Score";
            jsonReader.scoreText.text = $"{scoreLabel}: {Score}";
        }
    }

    public void ResetScore()
    {
        Score = 0;
        hasCountedFirstPlant = false;

        if (jsonReader && jsonReader.scoreText)
        {
            var l = jsonReader.GetCurrentLangData();
            var scoreLabel = l?.labels?.score ?? "Score";
            jsonReader.scoreText.text = $"{scoreLabel}: {Score}";
        }
        OnScoreChanged?.Invoke(Score);
    }
}