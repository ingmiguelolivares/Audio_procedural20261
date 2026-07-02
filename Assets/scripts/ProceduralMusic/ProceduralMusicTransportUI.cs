using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ProceduralMusicTransportUI : MonoBehaviour
{
    [Header("Targets")]
    public ProceduralMusicManager musicManager;
    public OSCController oscController;

    [Header("Transport buttons")]
    public Button playButton;
    public Button stopButton;

    [Header("Drum preview buttons")]
    public Button kickButton;
    public Button snareButton;
    public Button drumButton;
    public DrumType drumButtonType = DrumType.HiHat;

    [Header("Preview")]
    public float previewDurationSeconds = 0.18f;
    [Range(0f, 1f)] public float previewVelocity = 0.9f;
    public float previewRetriggerGuardSeconds = 0.08f;

    [Header("Button wiring")]
    public bool autoWireButtons = true;
    public bool skipAutoWireWhenButtonHasPersistentEvents = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void PlayGeneratedSong()
    {
        ResolveReferences();

        if (musicManager == null)
        {
            Debug.LogWarning("[ProceduralMusicTransportUI] No ProceduralMusicManager assigned.");
            return;
        }

        musicManager.PlayGeneratedSong();
    }

    public void StopGeneratedSong()
    {
        ResolveReferences();

        if (musicManager != null)
        {
            musicManager.Stop();
            return;
        }

        if (oscController != null)
            oscController.StopAll();
    }

    public void PreviewKick()
    {
        ResolveReferences();

        if (oscController == null)
        {
            Debug.LogWarning("[ProceduralMusicTransportUI] No OSCController assigned.");
            return;
        }

        oscController.PlayKick(Mathf.Max(0.02f, previewDurationSeconds), previewVelocity);
    }

    public void PreviewSnare()
    {
        ResolveReferences();

        if (oscController == null)
        {
            Debug.LogWarning("[ProceduralMusicTransportUI] No OSCController assigned.");
            return;
        }

        oscController.PlaySnare(Mathf.Max(0.02f, previewDurationSeconds), previewVelocity);
    }

    public void PreviewDrumButton()
    {
        PreviewDrum(drumButtonType);
    }

    public void PreviewHiHat()
    {
        ResolveReferences();

        if (oscController == null)
        {
            Debug.LogWarning("[ProceduralMusicTransportUI] No OSCController assigned.");
            return;
        }

        oscController.PlayHiHat(Mathf.Max(0.02f, previewDurationSeconds), previewVelocity);
    }

    public void PreviewDrum(DrumType drumType)
    {
        switch (drumType)
        {
            case DrumType.Snare:
                PreviewSnare();
                break;
            case DrumType.HiHat:
                PreviewHiHat();
                break;
            default:
                PreviewKick();
                break;
        }
    }

    private void ResolveReferences()
    {
        if (musicManager == null)
            musicManager = FindFirstObjectByType<ProceduralMusicManager>();

        if (oscController == null)
        {
            if (musicManager != null && musicManager.oscController != null)
                oscController = musicManager.oscController;
            else
                oscController = FindFirstObjectByType<OSCController>();
        }
    }

    private void AddListeners()
    {
        if (!autoWireButtons)
            return;

        AddButtonListener(playButton, PlayGeneratedSong);
        AddButtonListener(stopButton, StopGeneratedSong);
        AddButtonListener(kickButton, PreviewKick);
        AddButtonListener(snareButton, PreviewSnare);
        AddButtonListener(drumButton, PreviewDrumButton);
    }

    private void RemoveListeners()
    {
        if (playButton != null) playButton.onClick.RemoveListener(PlayGeneratedSong);
        if (stopButton != null) stopButton.onClick.RemoveListener(StopGeneratedSong);
        if (kickButton != null) kickButton.onClick.RemoveListener(PreviewKick);
        if (snareButton != null) snareButton.onClick.RemoveListener(PreviewSnare);
        if (drumButton != null) drumButton.onClick.RemoveListener(PreviewDrumButton);
    }

    private void AddButtonListener(Button button, UnityAction action)
    {
        if (button == null)
            return;

        if (skipAutoWireWhenButtonHasPersistentEvents && button.onClick.GetPersistentEventCount() > 0)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

}
