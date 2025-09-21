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

    // Start is called before the first frame update
    void Start()
    {
        playGame = true;

		audioSource = GetComponent<AudioSource>();
        //PlayMusic(normalMusic);

		target.transform.position = pointA;
		timer = 0f;
    }

    // Update is called once per frame
    void Update()
    {
		// Nhấn Space để bắt đầu di chuyển
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
        }

        

        if (playGame == true)
		{
			//saltwaterIntrusionObject.StartMove();
			timeRemaining += Time.deltaTime;
			DisplayTime(timeRemaining);

			if (timeRemaining <=90)
			{
				//Debug.Log("Mùa Mưa bắt đầu!");
				Weather_Rain.SetActive(true);
				Rain_image.SetActive(true);
				Sun_image.SetActive(false);
				RenderSettings.skybox = Skybox_Rain;
        		DynamicGI.UpdateEnvironment();
				PlayMusic(rainMusic);
				//saltwaterIntrusionObject.StartMove();
				

			}
			else if (timeRemaining > 90 && timeRemaining <=160)
			{
				//Debug.Log("Mùa Khô bắt đầu!");
				Weather_Rain.SetActive(false);
				Rain_image.SetActive(false);
				Sun_image.SetActive(true);
				RenderSettings.skybox = Skybox_Sun;
        		DynamicGI.UpdateEnvironment();
				PlayMusic(normalMusic);
				moving = true;
				

			}
			else if (timeRemaining > 160 && timeRemaining<=180)
			{
				//Debug.Log("Mùa Mưa Trở Lại!");
				Weather_Rain.SetActive(true);
				Rain_image.SetActive(true);
				Sun_image.SetActive(false);
				RenderSettings.skybox = Skybox_Rain;
        		DynamicGI.UpdateEnvironment();
				target.transform.position = pointA;
			}

			else
			{
				//Debug.Log("Màn chơi kết thúc.");
				playGame = false;
				Weather_Rain.SetActive(false);
				Rain_image.SetActive(false);
				Sun_image.SetActive(true);
				RenderSettings.skybox = Skybox_Sun;
        		DynamicGI.UpdateEnvironment();
				PlayMusic(rainMusic);
			}

			if (moving)
			{
				timer += Time.deltaTime;
				float t = timer / moveTime;

				target.transform.position = Vector3.Lerp(pointA, pointB, t);

				if (t >= 1f)
					moving = false;
			}
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

	
}
