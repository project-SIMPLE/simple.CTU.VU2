using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public enum ScoreFlow { GrowthTime, Seasonal }

public class RulesoftheGame_VU2_1 : MonoBehaviour
{
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

    public static float Saltwater_Intrusion = 0.0f;
    private static SeasonPhase _currentPhase = SeasonPhase.Rainy1;
    public static event System.Action<SeasonPhase> OnPhaseChanged;
    private static SeasonPhase _cachedPhase = (SeasonPhase)(-1);

    public static bool GameActive { get; private set; } = false;

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

    [Header("XR Move Lock (VR)")]
    public ActionBasedContinuousMoveProvider moveProvider;
    public LocomotionSystem locomotionSystem;                 
    public ActionBasedContinuousTurnProvider turnProvider;   
    public bool lockTurningToo = false;

    [Header("Player Start")]
    public Transform player;
    private Vector3 _playerStartPos;
    private bool _playerStartSaved = false;
    
    [Header("UI Root (optional)")]
    public GameObject GameplayUIRoot; 
    public GameObject GameUIRoot;

    [Header("Scoring Mode")]
    public ScoreFlow scoringMode = ScoreFlow.Seasonal;
    
    public static ScoreFlow CurrentScoringMode { get; private set; }

    /// <summary>
    /// Khởi tạo tham chiếu (move/turn/locomotion, audio) và đồng bộ chế độ chấm điểm ban đầu.
    /// </summary>
    private void Awake()
    {
        if (!moveProvider) moveProvider = FindObjectOfType<ActionBasedContinuousMoveProvider>(true);
        if (!locomotionSystem) locomotionSystem = FindObjectOfType<LocomotionSystem>(true);
        if (!turnProvider) turnProvider = FindObjectOfType<ActionBasedContinuousTurnProvider>(true);
        _audioSource = GetComponent<AudioSource>();
        CurrentScoringMode = scoringMode;
    }

    /// <summary>
    /// Thiết lập trạng thái UI/FX khi chưa bắt đầu game (khóa di chuyển, bật menu, set sky/music mặc định).
    /// </summary>
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

        SetMovementLocked(true);
        GameplayUIRoot.SetActive(true);
        GameUIRoot.SetActive(true);
    }

    /// <summary>
    /// Vòng lặp chính: cập nhật thời gian, chuyển pha theo mốc (Mưa 0–120s, Khô 120–240s), 
    /// điều khiển thời tiết/skybox/nhạc, di chuyển nước và kết thúc game khi quá 240s.
    /// </summary>
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
        else
        {
            _rainning = false;
            Weather_Rain.SetActive(false);
            Rain_image.SetActive(false);
            Sun_image.SetActive(true);
            RenderSettings.skybox = Skybox_Sun; DynamicGI.UpdateEnvironment();
            PlayMusic(normalMusic);

            playGame = false;
            GameActive = false;

            _moving = false;
            _applyMoveThisFrame = false;

            
            // can toi uu
            var farms = FindObjectsOfType<FarmArea>(true);
            foreach (var a in farms) a.FreezeHUD();

            var boards = FindObjectsOfType<Thuan_23127_TotalBoard>(true);
            foreach (var b in boards) b.Freeze(true);

            SetMovementLocked(true);

            if (_audioSource && messageSfx) _audioSource.PlayOneShot(messageSfx);
            ResultMenu.SetActive(true);
            GameplayUIRoot.SetActive(false);
            GameUIRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Bước cập nhật sau Update: thực hiện bước di chuyển nước nếu frame này được bật cờ.
    /// </summary>
    private void LateUpdate()
    {
        if (_applyMoveThisFrame) StepMove();
    }

    /// <summary>
    /// Nội suy vị trí nước từ _fromPos sang _toPos theo moveTime (smooth) trong mỗi pha di chuyển.
    /// </summary>
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

    /// <summary>
    /// Hiển thị thời gian mm:ss lên đồng hồ HUD.
    /// </summary>
    void DisplayTime(float t)
    {
        float m = Mathf.FloorToInt(t / 60);
        float s = Mathf.FloorToInt(t % 60);
        clockText.text = $"{m:00}:{s:00}";
    }

    /// <summary>
    /// Phát nhạc nền nếu clip hợp lệ và khác clip hiện tại.
    /// </summary>
    void PlayMusic(AudioClip clip)
    {
        if (!clip) return;
        if (!_audioSource) _audioSource = GetComponent<AudioSource>();
        if (!_audioSource) return;
        if (_audioSource.clip == clip) return;
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    /// <summary>
    /// Bắt đầu ván chơi: reset thời gian, mở HUD gameplay, mở di chuyển, lưu vị trí player/điểm A lần đầu.
    /// </summary>
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

        SetMovementLocked(false);
        GameplayUIRoot.SetActive(true);
        GameUIRoot.SetActive(true);
    }

    /// <summary>
    /// Khởi động lại ván chơi: Reload scene để trả về trạng thái ban đầu hoàn toàn.
    /// </summary>
    public void RestartGame()
    {
        // Reload scene để trở về trạng thái ban đầu như khi mới run game
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    /// <summary>
    /// Mở bảng chi tiết điểm cuối trận, ẩn menu kết quả và UI cho VR.
    /// </summary>
    public void ShowResultDetailsScore()
    {
        ResultDetailsScore.SetActive(true);
        ResultMenu.SetActive(false);
        UIForVR.SetActive(false);
    }

    /// <summary>
    /// Đóng bảng chi tiết điểm, quay lại menu kết quả và bật lại UI cho VR.
    /// </summary>
    public void CloseResultDetailsScore()
    {
        ResultDetailsScore.SetActive(false);
        ResultMenu.SetActive(true);
        UIForVR.SetActive(true);
    }

    /// <summary>
    /// Chuyển pha mùa: kết sổ (nếu Seasonal), cập nhật cờ mùa & Saltwater_Intrusion, bắn sự kiện và refresh salinity/UI.
    /// </summary>
    private void SetPhase(SeasonPhase phase)
    {
        if (_cachedPhase == phase) return;

        if (InstanceExistsAndSeasonal())
            SettleAllFarmsForNewSeason();

        _cachedPhase = phase;
        _currentPhase = phase;
        Saltwater_Intrusion = (phase == SeasonPhase.Dry) ? 1f : 0f;
        
        OnPhaseChanged?.Invoke(_currentPhase);

        foreach (var t in FindObjectsOfType<Thuan_23127_PlantGrowth>())
            t.UpdateSalinityEvent();

        var gm = Thuan_23127_GameManager.Instance;
        if (gm && gm.jsonReader) gm.jsonReader.UpdateSalinityUI(gm.GetSeasonSalinity());
    }
    
    /// <summary>
    /// Khóa/Mở khả năng di chuyển (và xoay nếu cấu hình) của người chơi trong XR.
    /// </summary>
    private void SetMovementLocked(bool locked)
    {
        if (moveProvider) moveProvider.enabled = !locked;
        if (lockTurningToo && turnProvider) turnProvider.enabled = !locked;
        if (locomotionSystem) locomotionSystem.enabled = !locked;
    }

    /// <summary>
    /// Kiểm tra có đang chạy chế độ chấm điểm theo mùa (Seasonal) hay không.
    /// </summary>
    private bool InstanceExistsAndSeasonal()
    {
        return scoringMode == ScoreFlow.Seasonal;
    }

    /// <summary>
    /// Yêu cầu tất cả FarmArea chốt điểm đối tượng đang còn và dọn slot để sang mùa mới.
    /// </summary>
    private void SettleAllFarmsForNewSeason()
    {
        var farms = FindObjectsOfType<FarmArea>(true);
        foreach (var a in farms) a.SettleAndClearForNewSeason();
    }
}
