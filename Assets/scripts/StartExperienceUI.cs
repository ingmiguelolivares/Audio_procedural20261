using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartExperienceUI : MonoBehaviour
{
    [TextArea(3, 8)]
    public string introMessage = "<size=115%><b>Audio Procedural Runner</b></size>\n\nEste proyecto demuestra <b>audio procedural en tiempo real</b> con síntesis generada por código nativo, compatible con <b>WebGL, desktop y móviles</b>.\n\n<b>Objetivo</b>\nRecoge los elementos correctos para mezclar la canción.\n\n<b>Controles</b>\n• Flechas izquierda y derecha o botones en pantalla para cambiar de carril.\n• Flecha arriba para saltar.\n\nPulsa <b>Iniciar</b> para activar el audio y comenzar la experiencia.";

    public GameObject introPanel;
    public TextMeshProUGUI introText;
    public Button startButton;
    public StartCounter startCounter;

    private bool hasStarted;
    private bool isWaitingForAudio;

    void Awake()
    {
        if (introText != null)
        {
            introText.SetText(introMessage);
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartPressed);
        }

        if (introPanel != null)
        {
            introPanel.SetActive(true);
        }
    }

    void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartPressed);
        }
    }

    public void OnStartPressed()
    {
        if (hasStarted || isWaitingForAudio)
        {
            return;
        }

        ProceduralSynthVoice.TryUnlockWebAudio();
        StartCoroutine(BeginExperience());
    }

    public void BeginFromExternalTap()
    {
        if (hasStarted || isWaitingForAudio)
        {
            return;
        }

        ProceduralSynthVoice.TryUnlockWebAudio();
        StartCoroutine(BeginExperience());
    }

    private IEnumerator BeginExperience()
    {
        isWaitingForAudio = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (introText != null)
        {
            introText.SetText("Activando audio...\n\nSi el navegador lo pide, vuelve a pulsar Iniciar.");
        }

        while (!ProceduralSynthVoice.IsWebAudioUnlocked())
        {
            yield return null;
        }
#else
        yield return null;
#endif

        hasStarted = true;
        isWaitingForAudio = false;

        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }

        if (startCounter != null)
        {
            startCounter.BeginCountdown();
        }
    }
}
