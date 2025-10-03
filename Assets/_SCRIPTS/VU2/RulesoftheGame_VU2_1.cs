using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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

	public Material Skybox_Rain;
	public Material Skybox_Sun;



	// Music
	public AudioClip rainMusic;
	public AudioClip normalMusic;
	private AudioSource audioSource;

	// saltwater intrusion
	//public Saltwater_Intrusion saltwaterIntrusionObject;
	public GameObject target;       // Object cần di chuyển
	public Vector3 pointA;          // Vị trí bắt đầu
	public Vector3 pointB;          // Vị trí kết thúc
	public float moveTime = 3f;     // Thời gian di chuyển (giây)

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
		audioSource = GetComponent<AudioSource>();
		PlayMusic(normalMusic);

		target.transform.position = pointA;
		timer = 0f;
		rainning = false;
		moving = false;
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
			else if (timeRemaining > 90 && timeRemaining <= 160)
			{
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
			else if (timeRemaining > 160 && timeRemaining <= 180)
			{
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
	}

	public void RestartGame()
	{
		Debug.Log("Restart Game.");
		playGame = true;
		ResultMenu.SetActive(false);
		StartMenu.SetActive(true);
		timeRemaining = 0;
		Weather_Rain.SetActive(false);
		Rain_image.SetActive(false);
		Sun_image.SetActive(true);
	}
	

}
