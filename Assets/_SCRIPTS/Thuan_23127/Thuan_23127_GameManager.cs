using System;
using UnityEngine;

public class Thuan_23127_GameManager : MonoBehaviour
{
    public static Thuan_23127_GameManager Instance;

    public int Score { get; private set; } // Lưu điểm hiện tại của người chơi
    public event Action<int> OnScoreChanged;

    [Header("Refs")]
    public Thuan_23127_JsonReader jsonReader;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Tính điểm 
    /// </summary>
    /// <param name="value"></param>
    public void AddScore(int value)
    {
        Score += value;
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