using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimeCounter : MonoBehaviour
{
  
    //public Text timeText;  // Reference to a UI Text component to display the time
    private float elapsedTime = 0f;  // Time passed since the counter started

    public TextMeshProUGUI timer;
    void Update()
    {
        // Increment the elapsed time by the time passed since the last frame
        elapsedTime += Time.deltaTime;

        // Convert the elapsed time to minutes and seconds
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        // Display the formatted time in the UI Text component
        //timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        if (seconds < 60) timer.SetText(seconds.ToString());

        if (minutes >= 1) Time.timeScale = 0;
      
    }
}
