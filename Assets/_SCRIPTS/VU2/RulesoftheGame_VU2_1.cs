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
	
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (true)
		{
			timeRemaining += Time.deltaTime;
			DisplayTime(timeRemaining);

			if (timeRemaining > 150)
			{
				Debug.Log("Mùa Mưa đã đến!");
				//Weather_Rain.SetActive(true);
			}
			if (timeRemaining > 275)
			{
				Debug.Log("Thoại NPC - Hướng dẫn di chuyển tầng giữa");
				Debug.Log("Cho phép di chuyển tầng giữa");

			}
			if (timeRemaining > 105)
			{
				Debug.Log("Thoại NPC - Hướng dẫn di chuyển tầng đáy");
				Debug.Log("Cho phép di chuyển tầng đáy");
				
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
