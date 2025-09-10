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

    // Start is called before the first frame update
    void Start()
    {
        playGame = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (playGame == true)
		{
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
			}
			else if (timeRemaining > 90 && timeRemaining <=160)
			{
				//Debug.Log("Mùa Khô bắt đầu!");
				Weather_Rain.SetActive(false);
				Rain_image.SetActive(false);
				Sun_image.SetActive(true);
				RenderSettings.skybox = Skybox_Sun;
        		DynamicGI.UpdateEnvironment();
			}
			else if (timeRemaining > 160 && timeRemaining<=180)
			{
				//Debug.Log("Mùa Mưa Trở Lại!");
				Weather_Rain.SetActive(true);
				Rain_image.SetActive(true);
				Sun_image.SetActive(false);
				RenderSettings.skybox = Skybox_Rain;
        		DynamicGI.UpdateEnvironment();
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
}
