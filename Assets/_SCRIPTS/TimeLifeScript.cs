using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class TimeLifeScript : MonoBehaviour
{
	public float timeRemaining = 180;
	public bool timerIsRunning = false;
	public TMP_Text clockText;

    // Start is called before the first frame update
    void Start()
    {
        // Starts the timer automatically
		timerIsRunning = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (timerIsRunning)
		{
			if (timeRemaining > 0)
			{
				timeRemaining -= Time.deltaTime;
				DisplayTime(timeRemaining);
			}
			else
			{
				Debug.Log("Time has run out!");

				timeRemaining = 0;
				timerIsRunning = false;
			}
		}
		
    }
	
	void DisplayTime(float timeToDisplay)
	{
		timeToDisplay += 1;
		float minutes = Mathf.FloorToInt(timeToDisplay / 60);
		float seconds = Mathf.FloorToInt(timeToDisplay % 60);
		clockText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
	}
	
}
