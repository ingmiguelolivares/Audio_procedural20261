using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class ExperimentFlowManager : MonoBehaviour
{
    [Header("Logger")]
    public ExperimentInteractionLogger logger;

    [Header("Intro canvas")]
    public GameObject introCanvas;
    public TextMeshProUGUI introText;
    public Button continueButton;
    public Button showImagesButton;
    [TextArea(3, 8)] public List<string> introPages = new List<string>
    {
        "Lee las instrucciones antes de comenzar.",
        "Observa la imagen del personaje y ajusta el audio con la interfaz.",
        "Cuando termines de ajustar el audio, presiona Finalizar para continuar."
    };

    [Header("Image canvas")]
    public GameObject imageCanvas;
    public Image characterImage;
    public TextMeshProUGUI imageCounterText;
    public TextMeshProUGUI imageNameText;
    public Button finishImageButton;
    public List<Sprite> imageSequence = new List<Sprite>();
    [Range(1, 12)] public int requiredTrials = 3;

    [Header("Optional canvases")]
    public GameObject completionCanvas;
    public TextMeshProUGUI completionText;
    public Button restartButton;

    [Header("Behavior")]
    public bool autoWireButtons = true;
    public bool skipAutoWireWhenButtonHasPersistentEvents = true;
    public bool hideIntroAfterStart = true;
    public bool hideImageCanvasOnComplete = true;
    public bool exportCsvOnComplete = true;

    [Header("Restart")]
    public bool reloadActiveSceneOnRestart = true;
    public bool exportCsvBeforeRestart = true;
    public bool resetLoggerSessionWhenNotReloading = true;

    private int introPageIndex;
    private int currentImageIndex = -1;
    private float currentTrialStartTime;
    private bool experimentStarted;
    private bool experimentCompleted;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResetFlow();
    }

    private void OnEnable()
    {
        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void ResetFlow()
    {
        ResolveReferences();

        introPageIndex = 0;
        currentImageIndex = -1;
        experimentStarted = false;
        experimentCompleted = false;

        SetActive(introCanvas, true);
        SetActive(imageCanvas, false);
        SetActive(completionCanvas, false);
        SetActive(showImagesButton != null ? showImagesButton.gameObject : null, introPages == null || introPages.Count == 0);
        SetActive(continueButton != null ? continueButton.gameObject : null, introPages != null && introPages.Count > 0);

        UpdateIntroText();

        if (logger != null)
            logger.SetContext("Intro", -1, string.Empty);
    }

    public void ContinueIntro()
    {
        ResolveReferences();

        if (logger != null)
            logger.RecordEvent("Intro", "Experiment", "ContinueIntro", introPageIndex.ToString(), 0f);

        if (introPages == null || introPages.Count == 0)
        {
            ShowReadyToStart();
            return;
        }

        introPageIndex++;

        if (introPageIndex >= introPages.Count)
        {
            ShowReadyToStart();
            return;
        }

        UpdateIntroText();
    }

    public void ShowImageCanvas()
    {
        ResolveReferences();

        if (experimentStarted)
            return;

        experimentStarted = true;
        experimentCompleted = false;
        currentImageIndex = 0;

        if (hideIntroAfterStart)
            SetActive(introCanvas, false);

        SetActive(imageCanvas, true);
        SetActive(completionCanvas, false);

        if (logger != null)
            logger.RecordEvent("Images", "Experiment", "ShowImageCanvas", string.Empty, 0f);

        ShowCurrentImage();
    }

    public void FinishCurrentImage()
    {
        ResolveReferences();

        if (!experimentStarted || experimentCompleted)
            return;

        float duration = Time.realtimeSinceStartup - currentTrialStartTime;
        string imageName = GetCurrentImageName();

        if (logger != null)
            logger.RecordTrialEnd(currentImageIndex + 1, imageName, duration);

        currentImageIndex++;

        if (currentImageIndex >= GetTrialCount())
        {
            CompleteExperiment();
            return;
        }

        ShowCurrentImage();
    }

    public void RestartApp()
    {
        ResolveReferences();

        string restartMode = reloadActiveSceneOnRestart ? "ReloadActiveScene" : "ResetFlow";
        if (logger != null)
        {
            logger.RecordEvent("Experiment", "Experiment", "RestartApp", restartMode, 0f);

            if (exportCsvBeforeRestart)
                logger.ExportCsv();
        }

        if (reloadActiveSceneOnRestart && TryReloadActiveScene())
            return;

        if (logger != null && resetLoggerSessionWhenNotReloading)
            logger.ResetSessionData();

        ResetFlow();
    }

    private void ShowReadyToStart()
    {
        introPageIndex = Mathf.Max(0, introPages != null ? introPages.Count - 1 : 0);
        SetActive(continueButton != null ? continueButton.gameObject : null, false);
        SetActive(showImagesButton != null ? showImagesButton.gameObject : null, true);

        if (logger != null)
            logger.SetContext("Ready", -1, string.Empty);
    }

    private void ShowCurrentImage()
    {
        int trialCount = GetTrialCount();
        if (trialCount <= 0)
        {
            Debug.LogWarning("[ExperimentFlowManager] No images assigned for the experiment.");
            CompleteExperiment();
            return;
        }

        currentImageIndex = Mathf.Clamp(currentImageIndex, 0, trialCount - 1);
        Sprite sprite = imageSequence[currentImageIndex];

        if (characterImage != null)
            characterImage.sprite = sprite;

        string imageName = GetCurrentImageName();
        currentTrialStartTime = Time.realtimeSinceStartup;

        if (imageCounterText != null)
            imageCounterText.SetText((currentImageIndex + 1).ToString() + " / " + trialCount.ToString());

        if (imageNameText != null)
            imageNameText.SetText(imageName);

        if (logger != null)
            logger.RecordTrialStart(currentImageIndex + 1, imageName);
    }

    private void CompleteExperiment()
    {
        experimentCompleted = true;
        experimentStarted = false;

        if (hideImageCanvasOnComplete)
            SetActive(imageCanvas, false);

        SetActive(completionCanvas, true);

        string exportPath = string.Empty;
        if (logger != null)
        {
            logger.SetContext("Complete", -1, string.Empty);
            logger.RecordEvent("Experiment", "Experiment", "Complete", string.Empty, 0f);

            if (exportCsvOnComplete)
                exportPath = logger.ExportCsv();
        }

        if (completionText != null)
        {
            if (string.IsNullOrEmpty(exportPath))
                completionText.SetText("Experimento finalizado.");
            else
                completionText.SetText("Experimento finalizado.\nCSV: " + exportPath);
        }
    }

    private void UpdateIntroText()
    {
        if (introText == null)
            return;

        if (introPages == null || introPages.Count == 0)
        {
            introText.SetText(string.Empty);
            return;
        }

        int index = Mathf.Clamp(introPageIndex, 0, introPages.Count - 1);
        introText.SetText(introPages[index]);
    }

    private int GetTrialCount()
    {
        if (imageSequence == null || imageSequence.Count == 0)
            return 0;

        return Mathf.Clamp(requiredTrials, 1, imageSequence.Count);
    }

    private string GetCurrentImageName()
    {
        if (imageSequence == null || currentImageIndex < 0 || currentImageIndex >= imageSequence.Count || imageSequence[currentImageIndex] == null)
            return string.Empty;

        return imageSequence[currentImageIndex].name;
    }

    private void ResolveReferences()
    {
        if (logger == null)
            logger = FindFirstObjectByType<ExperimentInteractionLogger>();
    }

    private void AddListeners()
    {
        if (!autoWireButtons)
            return;

        AddButtonListener(continueButton, ContinueIntro);
        AddButtonListener(showImagesButton, ShowImageCanvas);
        AddButtonListener(finishImageButton, FinishCurrentImage);
        AddButtonListener(restartButton, RestartApp);
    }

    private void RemoveListeners()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(ContinueIntro);
        if (showImagesButton != null) showImagesButton.onClick.RemoveListener(ShowImageCanvas);
        if (finishImageButton != null) finishImageButton.onClick.RemoveListener(FinishCurrentImage);
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartApp);
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

    private bool TryReloadActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            Debug.LogWarning("[ExperimentFlowManager] Active scene is not valid. Resetting experiment flow instead.");
            return false;
        }

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            EditorSceneManager.LoadScene(activeScene.path);
            return true;
        }
#endif

        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return true;
        }

        if (!string.IsNullOrEmpty(activeScene.name))
        {
            SceneManager.LoadScene(activeScene.name);
            return true;
        }

        Debug.LogWarning("[ExperimentFlowManager] Could not reload active scene. Resetting experiment flow instead.");
        return false;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
