using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public enum ScoreFlow { GrowthTime, Seasonal }

public class RulesoftheGame_VU2_1 : MonoBehaviour
{
    // ==== UI & FX ====
    public GameObject Weather_Rain;
    public Text clockText;
    public float timeRemaining = 0;
    public bool playGame = false;
    public GameObject Rain_image;
    public GameObject Sun_image;

    public GameObject StartMenu;
    public GameObject ResultMenu;
    public GameObject ResultDetailsScore;
    public GameObject UIForVR;

    public GameObject NPC_Talk;
    public Material Skybox_Rain;
    public Material Skybox_Sun;

    // ==== Season ====
    public static float Saltwater_Intrusion = 0.0f;
    private static SeasonPhase _currentPhase = SeasonPhase.Rainy1;
    public static event System.Action<SeasonPhase> OnPhaseChanged;
    private static SeasonPhase _cachedPhase = (SeasonPhase)(-1);

    // ==== Global Game State ====
    public static bool GameActive { get; private set; } = false;

    // ==== Water move ====
    public GameObject target;
    public Vector3 pointA;
    public Vector3 pointB;
    public float moveTime = 3f;

    [Header("Music")]
    public AudioClip rainMusic;
    public AudioClip normalMusic;
    [FormerlySerializedAs("messageSFX")] public AudioClip messageSfx;
    private AudioSource _audioSource;

    private bool _moving, _rainning;
    private bool _enteredDry = false, _enteredRainy2 = false;
    private float _phaseStartTime = 0f;
    private Vector3 _fromPos, _toPos;
    private bool _applyMoveThisFrame = false;
    private bool _didSnapPointA = false;

    // ==== Player (lock chỉ di chuyển) ====
    [Header("XR Move Lock (VR)")]
    public ActionBasedContinuousMoveProvider moveProvider;
    public LocomotionSystem locomotionSystem;                 
    public ActionBasedContinuousTurnProvider turnProvider;   
    public bool lockTurningToo = false;

    [Header("Player Start")]
    public Transform player;                 // XR Origin (Transform)
    private Vector3 _playerStartPos;
    private bool _playerStartSaved = false;
    
    [Header("UI Root (optional)")]
    public GameObject GameplayUIRoot; 
    public GameObject GameUIRoot;
    // ========================================SeasonFlow=====================================================
    [Header("Scoring Mode")]
    public ScoreFlow scoringMode = ScoreFlow.Seasonal; // Flow cũ mặc định

    private void Awake()
    {
        if (!moveProvider) moveProvider = FindObjectOfType<ActionBasedContinuousMoveProvider>(true);
        if (!locomotionSystem) locomotionSystem = FindObjectOfType<LocomotionSystem>(true);
        if (!turnProvider) turnProvider = FindObjectOfType<ActionBasedContinuousTurnProvider>(true);
        _audioSource = GetComponent<AudioSource>();
    }

    public void Start()
    {
        playGame = false;
        GameActive = false;

        ResultMenu.SetActive(false);
        StartMenu.SetActive(true);
        Weather_Rain.SetActive(false);
        Rain_image.SetActive(false);
        Sun_image.SetActive(true);
        NPC_Talk.SetActive(false);
        PlayMusic(normalMusic);

        _moving = false;
        _enteredDry = _enteredRainy2 = false;
        _applyMoveThisFrame = false;

        // Khoá di chuyển khi chưa chơi
        SetMovementLocked(true);
        GameplayUIRoot.SetActive(true);
        GameUIRoot.SetActive(true);
    }

    public void Update()
    {
        _applyMoveThisFrame = false;
        if (!playGame) return;

        timeRemaining += Time.deltaTime;
        DisplayTime(timeRemaining);

        if (timeRemaining <= 90f)
        {
            SetPhase(SeasonPhase.Rainy1);
            _rainning = true;
            Weather_Rain.SetActive(true);
            Rain_image.SetActive(true);
            Sun_image.SetActive(false);
            RenderSettings.skybox = Skybox_Rain; DynamicGI.UpdateEnvironment();
            PlayMusic(rainMusic);

            _moving = false;
            _enteredDry = false;
        }
        else if (timeRemaining > 90f && timeRemaining <= 180f)
        {
            SetPhase(SeasonPhase.Dry);
            _rainning = false;
            Weather_Rain.SetActive(false);
            Rain_image.SetActive(false);
            Sun_image.SetActive(true);
            RenderSettings.skybox = Skybox_Sun; DynamicGI.UpdateEnvironment();
            PlayMusic(normalMusic);

            if (!_enteredDry)
            {
                _enteredDry = true; _enteredRainy2 = false;
                _phaseStartTime = timeRemaining;
                _fromPos = target ? target.transform.position : pointA;
                _toPos = pointB;
                _moving = true;
            }
            if (_moving && target) _applyMoveThisFrame = true;
        }
        else if (timeRemaining > 180f && timeRemaining <= 270f)
        {
            SetPhase(SeasonPhase.Rainy2);
            _rainning = true;
            Weather_Rain.SetActive(true);
            Rain_image.SetActive(true);
            Sun_image.SetActive(false);
            RenderSettings.skybox = Skybox_Rain; DynamicGI.UpdateEnvironment();
            PlayMusic(rainMusic);

            if (!_enteredRainy2)
            {
                _enteredRainy2 = true;
                _phaseStartTime = timeRemaining;
                _fromPos = target ? target.transform.position : pointB;
                _toPos = pointA;
                _moving = true;
            }
            if (_moving && target) _applyMoveThisFrame = true;
        }
        else
        {
            // ===== END GAME =====
            _rainning = false;
            Weather_Rain.SetActive(false);
            Rain_image.SetActive(false);
            Sun_image.SetActive(true);
            RenderSettings.skybox = Skybox_Sun; DynamicGI.UpdateEnvironment();
            PlayMusic(normalMusic);

            playGame = false;
            GameActive = false;

            // Dừng nước + dừng HUD (và không còn nhận sự kiện)
            _moving = false;
            _applyMoveThisFrame = false;

            // Ẩn & đóng băng tất cả HUD + TotalBoard
            var farms = FindObjectsOfType<FarmArea>(true);
            foreach (var a in farms) a.FreezeHUD();

            var boards = FindObjectsOfType<Thuan_23127_TotalBoard>(true);
            foreach (var b in boards) b.Freeze(true);

            // Khoá DI CHUYỂN của player (ray/nhặt vẫn OK)
            SetMovementLocked(true);

            if (_audioSource && messageSfx) _audioSource.PlayOneShot(messageSfx);
            ResultMenu.SetActive(true);
            GameplayUIRoot.SetActive(false);
            GameUIRoot.SetActive(false);
            
        }
    }

    private void LateUpdate()
    {
        if (_applyMoveThisFrame) StepMove();
    }

    private void StepMove()
    {
        if (!target) return;
        if ((_toPos - _fromPos).sqrMagnitude < 1e-6f) { _moving = false; return; }

        float elapsed = timeRemaining - _phaseStartTime;
        float t = (moveTime <= 0f) ? 1f : Mathf.Clamp01(elapsed / moveTime);
        t = Mathf.SmoothStep(0f, 1f, t);
        target.transform.position = Vector3.Lerp(_fromPos, _toPos, t);
        if (t >= 1f) _moving = false;
    }

    void DisplayTime(float t)
    {
        float m = Mathf.FloorToInt(t / 60);
        float s = Mathf.FloorToInt(t % 60);
        clockText.text = $"{m:00}:{s:00}";
    }

    void PlayMusic(AudioClip clip)
    {
        if (!clip) return;
        if (!_audioSource) _audioSource = GetComponent<AudioSource>();
        if (!_audioSource) return;
        if (_audioSource.clip == clip) return;
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    public void StartGame()
    {
        playGame = true;
        GameActive = true;

        StartMenu.SetActive(false);
        ResultMenu.SetActive(false);
        timeRemaining = 0f;
        NPC_Talk.SetActive(true);
        ResultDetailsScore.SetActive(false);

        if (!_playerStartSaved && player)
        {
            _playerStartPos = player.position;
            _playerStartSaved = true;
        }

        if (!_didSnapPointA && target)
        {
            pointA = target.transform.position;
            _didSnapPointA = true;
        }

        _enteredDry = _enteredRainy2 = false;
        _moving = false; _applyMoveThisFrame = false;
        _phaseStartTime = timeRemaining;

        SetMovementLocked(false); // mở di chuyển
        GameplayUIRoot.SetActive(true);
        GameUIRoot.SetActive(true);

    }

    public void RestartGame()
    {
        Thuan_23127_GameManager.Instance?.ResetScore();
        var sum = Thuan_23127_SeasonalSummary.Instance;
        if (sum) sum.ResetAllData();

        // Reset plots
        foreach (var farm in FindObjectsOfType<FarmArea>()) farm.ResetAllPlots();

        // Bảng tổng: mở đóng băng + rebuild
        var boards = FindObjectsOfType<Thuan_23127_TotalBoard>(true);
        foreach (var b in boards) { b.Freeze(false); b.Rebuild(); }

        // HUD về mặc định
        var huds = FindObjectsOfType<Thuan_23127_AreaHUD>(true);
        foreach (var h in huds) h.ResetHUDToDefaults();

        // UI & sky
        StartMenu.SetActive(false);
        ResultMenu.SetActive(false);
        Weather_Rain.SetActive(false);
        Rain_image.SetActive(false);
        Sun_image.SetActive(true);
        RenderSettings.skybox = Skybox_Sun; DynamicGI.UpdateEnvironment();
        PlayMusic(normalMusic);

        // Nước về A
        if (target) target.transform.position = pointA;

        // Player về vị trí gốc trước lần Start đầu
        if (player && _playerStartSaved) player.position = _playerStartPos;

        // Reset state & bật chơi
        timeRemaining = 0f;
        _enteredDry = _enteredRainy2 = false;
        _moving = false; _applyMoveThisFrame = false;
        _phaseStartTime = timeRemaining;

        playGame = true;
        GameActive = true;
        GameplayUIRoot.SetActive(true);
        GameUIRoot.SetActive(true);
        // SetMovementLocked(false); //khoa di chuyen
    }

    public void ShowResultDetailsScore()
    {
        ResultDetailsScore.SetActive(true);
        ResultMenu.SetActive(false);
        UIForVR.SetActive(false);
    }
    public void CloseResultDetailsScore()
    {
        ResultDetailsScore.SetActive(false);
        ResultMenu.SetActive(true);
        UIForVR.SetActive(true);
    }

    // Season switch + notify
    private void SetPhase(SeasonPhase phase)
    {
        if (_cachedPhase == phase) return;
        _cachedPhase = phase;
        _currentPhase = phase;

        Saltwater_Intrusion = (phase == SeasonPhase.Rainy1) ? 0f :
                              (phase == SeasonPhase.Dry)    ? 1f : 2f;

        OnPhaseChanged?.Invoke(_currentPhase);
        
        
        if (InstanceExistsAndSeasonal())
        {
            // 1) Kết sổ: tính điểm tất cả cây/con/cá đang có theo mùa hiện tại
            // 2) Dọn sạch ô để sang mùa trồng mới
            SettleAllFarmsForNewSeason();
        }
        // Refresh salinity on every growth
        var all = FindObjectsOfType<Thuan_23127_PlantGrowth>();
        foreach (var t in all)
            t.UpdateSalinityEvent(); 

        var gm = Thuan_23127_GameManager.Instance;
        if (gm && gm.jsonReader) gm.jsonReader.UpdateSalinityUI(gm.GetSeasonSalinity());
    }

    // ==== Lock/mở CHỈ di chuyển (VR) ====
    private void SetMovementLocked(bool locked)
    {
        if (moveProvider) moveProvider.enabled = !locked;

        if (lockTurningToo && turnProvider) turnProvider.enabled = !locked;
        if (locomotionSystem) locomotionSystem.enabled = !locked;
    }

    private bool InstanceExistsAndSeasonal()
    {
        return scoringMode == ScoreFlow.Seasonal;
    }

    private void SettleAllFarmsForNewSeason()
    {
        var farms = FindObjectsOfType<FarmArea>(true);
        foreach (var a in farms) a.SettleAndClearForNewSeason();
    }
}
