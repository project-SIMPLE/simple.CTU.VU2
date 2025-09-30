using System;
using System.Collections.Generic;
using UnityEngine;

public class Thuan_23127_GameManager : MonoBehaviour
{
    public static Thuan_23127_GameManager Instance;

    public int Score { get; private set; }
    public event Action<int> OnScoreChanged;
    
    [Header("Refs")]
    public Thuan_23127_JsonReader jsonReader;

    //(plantId) chỉ được cộng điểm đúng 1 lần
    private readonly HashSet<int> scoredPlantIds = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Cộng điểm cho mot cay duy nhất
    /// Trả về true nếu cộng thành công, false nếu loại đó đã cộng rồi.
    /// </summary>
    public bool AddScoreForPlant(int plantId, int value)
    {
        if (scoredPlantIds.Contains(plantId)) return false;

        scoredPlantIds.Add(plantId);
        Score += value;
        OnScoreChanged?.Invoke(Score);
        UpdateScoreUI();
        return true;
    }

    /// <summary>
    /// Cong full diem tung cay
    /// </summary>
    // public void AddScore(int value)
    // {
    //     Score += value;
    //     OnScoreChanged?.Invoke(Score);
    //     UpdateScoreUI();
    // }

    private void UpdateScoreUI()
    {
        if (jsonReader && jsonReader.scoreText)
        {
            var l = jsonReader.GetCurrentLangData();
            var scoreLabel = l?.labels?.score ?? "Score";
            jsonReader.scoreText.text = $"{scoreLabel}: {Score}";
        }
    }

    /// <summary>
    /// Reset
    /// </summary>
    private void ResetAll()
    {
        Score = 0;
        scoredPlantIds.Clear();
        OnScoreChanged?.Invoke(Score);
        UpdateScoreUI();
    }

    /// <summary>
    /// Reset loai cay da tinh diem
    /// Dùng qua man moi khi muon giu diem
    /// </summary>
    public void ResetCountedPlantTypesOnly()
    {
        scoredPlantIds.Clear();
    }

    /// <summary>
    /// Giữ tương thích với code cũ nếu bạn đang gọi ResetScore() ở nơi khác.
    /// </summary>
    public void ResetScore() => ResetAll();
}
