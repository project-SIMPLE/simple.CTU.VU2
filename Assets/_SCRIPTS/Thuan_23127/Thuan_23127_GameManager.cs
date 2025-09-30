using System;
using UnityEngine;

public class Thuan_23127_GameManager : MonoBehaviour
{
    public static Thuan_23127_GameManager Instance;

    private int Score { get; set; }
    public event Action<int> OnScoreChanged;

    [Header("Refs")]
    public Thuan_23127_JsonReader jsonReader; 

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddScore(int value)
    {
        Score += value;
        OnScoreChanged?.Invoke(Score);

        if (jsonReader && jsonReader.scoreText)
        {
            var l = jsonReader.GetCurrentLangData();
            string scoreLabel = l?.labels?.score ?? "Score";
            jsonReader.scoreText.text = $"{scoreLabel}: {Score}";
        }
    }
}
