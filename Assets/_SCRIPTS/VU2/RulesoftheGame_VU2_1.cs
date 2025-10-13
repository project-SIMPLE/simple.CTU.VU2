using UnityEngine;
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
	private static Season _currentSeason = Season.Normal;
	public static event System.Action<Season> OnSeasonChanged;
	
	private static Season _cachedSeasonEnum = (Season)(-1);

	// singleton instance and local reference
	public static RulesoftheGame_VU2_1 Instance;
	private RulesoftheGame_VU2_1 rules;
	
	// saltwater intrusion
	//public Saltwater_Intrusion saltwaterIntrusionObject;
	public GameObject target;       // Object cần di chuyển
	public Vector3 pointA;          // Vị trí bắt đầu
	public Vector3 pointB;          // Vị trí kết thúc
	public float moveTime = 3f;     // Thời gian di chuyển (giây)

	[Header("Music")]
	// Music
	public AudioClip rainMusic;
	public AudioClip normalMusic;
	public AudioClip messageSFX;
	private AudioSource audioSource;

	private float timer;
	private bool moving;
	private bool rainning;

	// Start is called before the first frame update
	public void Start()
	{
		playGame = false;

		ResultMenu.SetActive(false);
		StartMenu.SetActive(true);
		Weather_Rain.SetActive(false);
		Rain_image.SetActive(false);
		Sun_image.SetActive(true);
		NPC_Talk.SetActive(false);
		audioSource = GetComponent<AudioSource>();
		PlayMusic(normalMusic);

		target.transform.position = pointA;
		timer = 0f;
		rainning = false;
		moving = false;
	}


	private void Awake()
	{
		Instance = this;
		DontDestroyOnLoad(gameObject);
		rules = FindObjectOfType<RulesoftheGame_VU2_1>();
	}

	// Update is called once per frame
	public void Update()
	{

		if (playGame == true)
		{
			//saltwaterIntrusionObject.StartMove();
			timeRemaining += Time.deltaTime;
			DisplayTime(timeRemaining);

			if (timeRemaining <= 90)
			{
				SetSeason(0.0f); // mùa mưa
								 //Debug.Log("Mùa Mưa bắt đầu!");
				rainning = true;
				Weather_Rain.SetActive(true);
				Rain_image.SetActive(true);
				Sun_image.SetActive(false);
				RenderSettings.skybox = Skybox_Rain;
				DynamicGI.UpdateEnvironment();
				PlayMusic(rainMusic);
				//saltwaterIntrusionObject.StartMove();


			}
			else if (timeRemaining > 90 && timeRemaining <= 180)
			{
				SetSeason(2.0f); // mùa khô
								 //Debug.Log("Mùa Khô bắt đầu!");
				rainning = false;
				Weather_Rain.SetActive(false);
				Rain_image.SetActive(false);
				Sun_image.SetActive(true);
				RenderSettings.skybox = Skybox_Sun;
				DynamicGI.UpdateEnvironment();
				PlayMusic(normalMusic);
				moving = false;
				//saltwaterIntrusionObject.StartMove();
				//target.transform.position = pointA;

				//Debug.Log("Di chuyển nước .");

				timer += Time.deltaTime;
				float t = timer / moveTime;

				target.transform.position = Vector3.Lerp(pointA, pointB, t);
			}
			else if (timeRemaining > 180 && timeRemaining <= 270)
			{
				SetSeason(1.0f); // bình thường
								 //Debug.Log("Mùa Mưa Trở Lại!");
				rainning = true;
				moving = true;
				Weather_Rain.SetActive(true);
				Rain_image.SetActive(true);
				Sun_image.SetActive(false);
				RenderSettings.skybox = Skybox_Rain;
				DynamicGI.UpdateEnvironment();
				target.transform.position = pointB;
				PlayMusic(rainMusic);

				timer += Time.deltaTime;
				float t = timer / moveTime;

				target.transform.position = Vector3.Lerp(pointB, pointA, t);
			}

			else
			{
				//Debug.Log("Màn chơi kết thúc.");
				playGame = false;
				rainning = false;
				Weather_Rain.SetActive(false);
				Rain_image.SetActive(false);
				Sun_image.SetActive(true);
				RenderSettings.skybox = Skybox_Sun;
				DynamicGI.UpdateEnvironment();
				PlayMusic(normalMusic);

				audioSource.PlayOneShot(messageSFX);
				ResultMenu.SetActive(true);

			}

			// if (moving== true && rainning == false)
			// {
			// 	Debug.Log("Di chuyển nước .");
			// 	timer += Time.deltaTime;
			// 	float t = timer / moveTime;

			// 	target.transform.position = Vector3.Lerp(pointA, pointB, t);

			// 	if (t >= 1f)
			// 		moving = false;
			// }
			// if (moving== true && rainning == true)
			// {
			// 	timer += Time.deltaTime;
			// 	float t = timer / moveTime;

			// 	target.transform.position = Vector3.Lerp(pointB, pointA, t);

			// 	if (t >= 1f)
			// 		moving = false;
			// }
		}
	}

	void DisplayTime(float timeToDisplay)
	{
		// timeToDisplay += 1;
		float minutes = Mathf.FloorToInt(timeToDisplay / 60);
		float seconds = Mathf.FloorToInt(timeToDisplay % 60);
		clockText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
	}

	void PlayMusic(AudioClip clip)
	{
		if (clip == null) return;
		if (audioSource.clip == clip) return; // nếu đang phát rồi thì bỏ qua

		audioSource.clip = clip;
		audioSource.Play();
	}

	public void StartGame()
	{
		Debug.Log("Play Game.");
		playGame = true;
		StartMenu.SetActive(false);
		timeRemaining = 0;
		NPC_Talk.SetActive(true);
		ResultDetailsScore.SetActive(false);
	}

	public void RestartGame()
	{
		Debug.Log("Restart Game.");
		
		Thuan_23127_GameManager.Instance?.ResetScore(); // reset diem
		ResetAllPlots(); // reset treê animal va fish
		StartMenu.SetActive(true);
		playGame = true;
		ResultMenu.SetActive(false);
		timeRemaining = 0;
		Weather_Rain.SetActive(false);
		Rain_image.SetActive(false);
		Sun_image.SetActive(true);
		ResultDetailsScore.SetActive(false); // Ẩn đi 
		
		target.transform.position = pointA; // reset vi tri ban dau

		PlayMusic(normalMusic);
		var hud = FindObjectsOfType<Thuan_23127_AreaHUD>(true); 
		foreach (var h in hud)
			h.ResetHUDToDefaults();
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

	public void PlaySFX(AudioClip audioClip)
	{
		if (audioClip == null) return;
		audioSource.PlayOneShot(audioClip);
	}

	private static void ResetAllPlots()
	{
		foreach (var farm in FindObjectsOfType<FarmArea>())
		{
			farm.ResetAllPlots();
		}
	}
	
	private static void SetSeason(float season)
	{
		if (Mathf.Approximately(_cachedSeason, season)) return;

		Saltwater_Intrusion = season;
		_cachedSeason = season;

		Season newSeason;
		if (Mathf.Approximately(season, 0f))      newSeason = Season.Rainy;
		else if (Mathf.Approximately(season, 1f)) newSeason = Season.Normal;
		else                                       newSeason = Season.Dry;

		if (_cachedSeasonEnum != newSeason)
		{
			_cachedSeasonEnum = newSeason;
			_currentSeason = newSeason;
			OnSeasonChanged?.Invoke(_currentSeason);
		}

		// cập nhật salinity trên từng cây + UI global
		var all = FindObjectsOfType<Thuan_23127_PlantGrowth>();
		for (int i = 0; i < all.Length; i++)
			all[i].UpdateSalinityEvent();

		var gm = Thuan_23127_GameManager.Instance;
		if (gm && gm.jsonReader)
			gm.jsonReader.UpdateSalinityUI(gm.GetSeasonSalinity());
	}
}
