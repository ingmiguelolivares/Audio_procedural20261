using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartCounter : MonoBehaviour
{
    
    public MonoBehaviour[] scriptsToActivate;  // Array of scripts to activate
    private float countdownTime = 5f;          // Time to countdown from

    public TextMeshProUGUI StartText;

    public OSC OSC1, OSC2, OSC3;
    void Awake()
    {
        // Disable all scripts at the start
        foreach (MonoBehaviour script in scriptsToActivate)
        {
            script.enabled = false;
        }

        OSC1.Octava = 5;
        OSC1.waveformType = OSC.WaveformType.SA;
        OSC1.Narmonicos = 10;
        
        // Start the countdown
        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(1f); 
        while (countdownTime > 0)
        {
            //Debug.Log("Countdown: " + countdownTime);
            OSC1.KeyboardDown("E");
            StartText.SetText(countdownTime.ToString());
            yield return new WaitForSeconds(1f);  // Wait for 1 second
            OSC1.KeyboardUp();
            countdownTime--;
        }

        // After countdown reaches 0, activate all scripts
         OSC1.KeyboardDown("B");
        StartText.SetText("Go!");
        //Debug.Log("Countdown finished. Activating scripts.");
        foreach (MonoBehaviour script in scriptsToActivate)
        {
            script.enabled = true;
        }
        yield return new WaitForSeconds(1f); 
        OSC1.KeyboardUp();
        StartText.enabled = false;
    }
}
