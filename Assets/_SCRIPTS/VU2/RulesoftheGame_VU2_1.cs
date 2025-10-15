using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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

    private static float _cachedSeason = -999f;

    private static SeasonPhase _currentPhase = SeasonPhase.Rainy1;
    public static event System.Action<SeasonPhase> OnPhaseChanged;
    private static SeasonPhase _cachedPhase = (SeasonPhase)(-1);

    // saltwater intrusion
    public GameObject target;       // Object nước cần di chuyển
    public Vector3 pointA;          // Vị trí A (mưa: rút về)
    public Vector3 pointB;          // Vị trí B (khô: dâng lên)
    public float moveTime = 3f;     // Thời gian di chuyển (giây)

    [Header("Music")]
    public AudioClip rainMusic;
    public AudioClip normalMusic;
    [FormerlySerializedAs("messageSFX")] public AudioClip messageSfx;
    private AudioSource _audioSource;

    private float _timer;
    private bool _moving;
    private bool _rainning;

    // ==== STATE cho di chuyển====
    private bool _enteredDry = false;      // đã vào time 90–180 chưa
    private bool _enteredRainy2 = false;   // đã vào time 180–270 chưa
    private float _phaseStartTime = 0f;    // mốc time khi bắt đầu
    private Vector3 _fromPos;              // vị trí đầu 
    private Vector3 _toPos;                // vị trí cuối 
    private bool _applyMoveThisFrame = false; // LateUpdate sẽ áp vị trí nếu true

    private bool _didSnapPointA = false;
    // ==========================================

    public void Start()
    {
        playGame = false;

        ResultMenu.SetActive(false);
        StartMenu.SetActive(true);
        Weather_Rain.SetActive(false);
        Rain_image.SetActive(false);
        Sun_image.SetActive(true);
        NPC_Talk.SetActive(false);
        _audioSource = GetComponent<AudioSource>();
        PlayMusic(normalMusic);

        _timer = 0f;
        _rainning = false;
        _moving = false;

        _enteredDry = false;
        _enteredRainy2 = false;
        _applyMoveThisFrame = false;
    }

    public void Update()
    {
        _applyMoveThisFrame = false; // reset mỗi frame

        if (!playGame) return;

        timeRemaining += Time.deltaTime;
        DisplayTime(timeRemaining);

        if (timeRemaining <= 10f)
        {
            SetPhase(SeasonPhase.Rainy1);

            _rainning = true;
            Weather_Rain.SetActive(true);
            Rain_image.SetActive(true);
            Sun_image.SetActive(false);
            RenderSettings.skybox = Skybox_Rain;
            DynamicGI.UpdateEnvironment();
            PlayMusic(rainMusic);

            // KHÔNG di chuyển ở Rainy1
            _moving = false;
            _enteredDry = false; 
        }
        else if (timeRemaining > 10f && timeRemaining <= 20f)
        {
            SetPhase(SeasonPhase.Dry);

            _rainning = false;
            Weather_Rain.SetActive(false);
            Rain_image.SetActive(false);
            Sun_image.SetActive(true);
            RenderSettings.skybox = Skybox_Sun;
            DynamicGI.UpdateEnvironment();
            PlayMusic(normalMusic);

            // vao mua Dry nuoc' bat dau di chuyen
            if (!_enteredDry)
            {
                _enteredDry = true;
                _enteredRainy2 = false;

                _phaseStartTime = timeRemaining;
                _fromPos = target ? target.transform.position : pointA;
                _toPos = pointB;

                _timer = 0f;
                _moving = true;
            }

            if (_moving && target)
            {
                _applyMoveThisFrame = true; // LateUpdate áp vị trí
            }
        }
        else if (timeRemaining > 20 && timeRemaining <= 50f)
        {
            SetPhase(SeasonPhase.Rainy2);

            _rainning = true;
            Weather_Rain.SetActive(true);
            Rain_image.SetActive(true);
            Sun_image.SetActive(false);
            RenderSettings.skybox = Skybox_Rain;
            DynamicGI.UpdateEnvironment();
            PlayMusic(rainMusic);

            if (!_enteredRainy2)
            {
                _enteredRainy2 = true;

                _phaseStartTime = timeRemaining;
                _fromPos = target ? target.transform.position : pointB;
                _toPos = pointA;

                _timer = 0f;
                _moving = true;
            }

            if (_moving && target)
            {
                _applyMoveThisFrame = true; // LateUpdate áp vị trí
            }
        }
        else
        {
            playGame = false;
            _rainning = false;
            Weather_Rain.SetActive(false);
            Rain_image.SetActive(false);
            Sun_image.SetActive(true);
            RenderSettings.skybox = Skybox_Sun;
            DynamicGI.UpdateEnvironment();
            PlayMusic(normalMusic);

            if (_audioSource && messageSfx) _audioSource.PlayOneShot(messageSfx);
            ResultMenu.SetActive(true);
        }
    }

    private void LateUpdate()
    {
        if (_applyMoveThisFrame)
            StepMove(); // áp vị trí SAU tất cả Update/Animator khác
    }

    private void StepMove()
    {
        if (!target) return;

        // Nếu A==B hoặc rất gần thì không cần di chuyển (tránh giật)
        if ((_toPos - _fromPos).sqrMagnitude < 1e-6f)
        {
            _moving = false;
            return;
        }

        float elapsed = timeRemaining - _phaseStartTime;
        float t = (moveTime <= 0f) ? 1f : Mathf.Clamp01(elapsed / moveTime);
        t = Mathf.SmoothStep(0f, 1f, t); // easing mượt

        target.transform.position = Vector3.Lerp(_fromPos, _toPos, t);

        if (t >= 1f)
            _moving = false; // tới đích thì dừng
    }

    void DisplayTime(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        clockText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void PlayMusic(AudioClip clip)
    {
        if (!clip) return;
        if (_audioSource.clip == clip) return; // đang phát rồi thì bỏ qua
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    public void StartGame()
    {
        Debug.Log("Play Game.");
        playGame = true;
        StartMenu.SetActive(false);
        timeRemaining = 0f;
        NPC_Talk.SetActive(true);
        ResultDetailsScore.SetActive(false);

        // KHÔNG ép vị trí khi Start lần đầu để tránh teleport.
        if (!_didSnapPointA && target)
        {
            pointA = target.transform.position;
            _didSnapPointA = true;
        }

        // Reset state di chuyển
        _enteredDry = false;
        _enteredRainy2 = false;
        _moving = false;
        _applyMoveThisFrame = false;
        _timer = 0f;
        _phaseStartTime = timeRemaining;
    }

    public void RestartGame()
    {
        Debug.Log("Restart Game.");
        Thuan_23127_GameManager.Instance?.ResetScore();
        var sum = Thuan_23127_SeasonalSummary.Instance;
        if (sum) sum.ResetAllData();

        var boards = FindObjectsOfType<Thuan_23127_TotalBoard>(true);
        foreach (var b in boards)
        {
            b.Rebuild();
        }
        
        Thuan_23127_GameManager.Instance?.ResetScore(); // reset điểm
        ResetAllPlots(); // reset farm/animal/fish

        // Sau restart: vào chơi ngay
        StartMenu.SetActive(false);
        playGame = true;

        ResultMenu.SetActive(false);
        timeRemaining = 0f;
        Weather_Rain.SetActive(false);
        Rain_image.SetActive(false);
        Sun_image.SetActive(true);
        ResultDetailsScore.SetActive(false);

        // Đưa nước về điểm A ngay
        if (target) target.transform.position = pointA;

        PlayMusic(normalMusic);
        var hud = FindObjectsOfType<Thuan_23127_AreaHUD>(true);
        foreach (var h in hud) h.ResetHUDToDefaults();

        _enteredDry = false;
        _enteredRainy2 = false;
        _moving = false;
        _applyMoveThisFrame = false;
        _timer = 0f;
        _phaseStartTime = timeRemaining;
        
    }

    /// <summary> Show details score </summary>
    public void ShowResultDetailsScore()
    {
        ResultDetailsScore.SetActive(true);
        ResultMenu.SetActive(false);
        UIForVR.SetActive(false);
    }

    /// <summary> Close details </summary>
    public void CloseResultDetailsScore()
    {
        ResultDetailsScore.SetActive(false);
        ResultMenu.SetActive(true);
        UIForVR.SetActive(true);
    }
    /// <summary> Audio </summary>
    public void PlaySFX(AudioClip audioClip)
    {
        if (audioClip == null) return;
        _audioSource.PlayOneShot(audioClip);
    }

    /// <summary> Reset tất cả farm/animal/fish về trạng thái ban đầu </summary>
    private static void ResetAllPlots()
    {
        foreach (var farm in FindObjectsOfType<FarmArea>())
            farm.ResetAllPlots();
    }
    /// <summary> Chuyển mùa và cập nhật Saltwater_Intrusion </summary>
    private static void SetPhase(SeasonPhase phase)
    {
        if (_cachedPhase == phase) return;
        _cachedPhase = phase;
        _currentPhase = phase;

        // 0 = Rainy1, 1 = Dry, 2 = Rainy2
        Saltwater_Intrusion = (phase == SeasonPhase.Rainy1) ? 0f :
                              (phase == SeasonPhase.Dry)    ? 1f : 2f;

        OnPhaseChanged?.Invoke(_currentPhase);

        // Cập nhật salinity hiển thị trên từng plant
        var all = UnityEngine.Object.FindObjectsOfType<Thuan_23127_PlantGrowth>();
        for (int i = 0; i < all.Length; i++)
            all[i].UpdateSalinityEvent();

        // Cập nhật UI tổng mặn (nếu có)
        var gm = Thuan_23127_GameManager.Instance;
        if (gm && gm.jsonReader)
            gm.jsonReader.UpdateSalinityUI(gm.GetSeasonSalinity());
    }
}
