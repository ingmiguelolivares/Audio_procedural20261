using System.Collections;
using UnityEngine;
using TMPro;

public class StartCounter : MonoBehaviour
{
    public MonoBehaviour[] scriptsToActivate;
    private const float InitialCountdownTime = 5f;
    private float countdownTime = InitialCountdownTime;
    private bool hasStarted;

    public TextMeshProUGUI StartText;

    public Osc OSC1, OSC2, OSC3;

    void Awake()
    {
        foreach (MonoBehaviour script in scriptsToActivate)
        {
            script.enabled = false;
        }

        OSC1.OctaveChange(5);
        OSC1.ArmonicosChange(10);
        OSC1.WaveFormChange(Osc.WaveFormType.SA);

        if (StartText != null)
        {
            StartText.enabled = true;
            StartText.SetText("Ready");
        }
    }

    public void BeginCountdown()
    {
        if (hasStarted)
        {
            return;
        }

        hasStarted = true;
        countdownTime = InitialCountdownTime;
        StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        yield return new WaitForSeconds(1f); 
        while (countdownTime > 0)
        {
            OSC1.KeyboardDown("E");
            if (StartText != null)
            {
                StartText.enabled = true;
                StartText.SetText(countdownTime.ToString());
            }
            yield return new WaitForSeconds(1f);  // Wait for 1 second
            OSC1.KeyboardUp();
            countdownTime--;
        }

        OSC1.KeyboardDown("B");
        if (StartText != null)
        {
            StartText.enabled = true;
            StartText.SetText("Go!");
        }

        foreach (MonoBehaviour script in scriptsToActivate)
        {
            script.enabled = true;
        }

        yield return new WaitForSeconds(1f); 
        OSC1.KeyboardUp();
        if (StartText != null)
        {
            StartText.enabled = false;
        }
    }
}
