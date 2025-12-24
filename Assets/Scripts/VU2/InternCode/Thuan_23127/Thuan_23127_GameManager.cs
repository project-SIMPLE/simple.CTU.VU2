using System;
using System.Collections.Generic;
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
    [Tooltip("Hệ số mùa khô")]
    public float dryFactor = 1.5f;
    private RulesoftheGame_VU2_1 _rules;
    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip harvestClip;

    // Send harvest report to server
    [SerializeField] private string harvestMessageName = "harvest_report";
    private readonly List<FarmArea> _cachedAreas = new List<FarmArea>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public float GetSeasonSalinity()
    {
        var k = (Mathf.Approximately(RulesoftheGame_VU2_1.Saltwater_Intrusion, 1f)) ? dryFactor : rainyFactor;
        return Mathf.Max(0f, salinityBase * k);
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
        if (!RulesoftheGame_VU2_1.GameActive) return;
        
        if (!_rules)
            _rules = FindObjectOfType<RulesoftheGame_VU2_1>();

        if (_rules && !_rules.playGame)
        {
            return;
        }
        
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
        if (jsonReader.scoreTextDetails)
            jsonReader.scoreTextDetails.text = $"{scoreLabel}: {Score}";
        // SendHarvestMessageToServer(value); // Bỏ ra để dùng đc gửi mess 
        
        // {
        //     "event": "harvest",
        //     "total_score": "28",
        //     "last_gain": "4",
        //     "season_salinity": "1.50",
        //     "area_north_salinity": "1.50",
        //     "area_south_salinity": "0.30"
        // }
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
            if(jsonReader.scoreTextDetails)
                jsonReader.scoreTextDetails.text = $"{scoreLabel}: {Score}";
        }

        OnScoreChanged?.Invoke(Score);
    }

    // Cach ham dung cho server
    private void Start()
    {
        CacheFarmAreas();
    }

    public void RefreshFarmAreaCache()
    {
        CacheFarmAreas();
    }

    private void CacheFarmAreas()
    {
        _cachedAreas.Clear();
        _cachedAreas.AddRange(FindObjectsOfType<FarmArea>());
    }

    private void SendHarvestMessageToServer(int lastGain)
    {
        var conn = ConnectionManager.Instance;
        if (ConnectionManager.Instance == null || !conn.IsConnectionState(ConnectionState.AUTHENTICATED))
            return;

        if (_cachedAreas.Count == 0)
            CacheFarmAreas();

        var payload = new Dictionary<string, string>
        {
            { "event", "harvest" },
            { "total_score", Score.ToString() },
            { "last_gain", lastGain.ToString() },
            { "season_salinity", GetSeasonSalinity().ToString("F2") }
        };

        foreach (var area in _cachedAreas)
        {
            if (!area) continue;
            var id = area.GetServerAreaId();
            payload[$"area_{id}_salinity"] = area.GetCurrentSalinityForServer().ToString("F2");
        }

        conn.SendExecutableAsk(harvestMessageName, payload);
    }
}