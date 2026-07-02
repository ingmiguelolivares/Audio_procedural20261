using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InstrumentPanelNavigator : MonoBehaviour
{
    [Header("Open buttons")]
    public Button openMelodyButton;
    public Button openHarmonyButton;
    public Button openBassButton;
    public Button openDrumsButton;

    [Header("Panel canvases")]
    public GameObject melodyPanelCanvas;
    public GameObject harmonyPanelCanvas;
    public GameObject bassPanelCanvas;
    public GameObject drumsPanelCanvas;

    [Header("Close buttons")]
    public Button closeMelodyButton;
    public Button closeHarmonyButton;
    public Button closeBassButton;
    public Button closeDrumsButton;

    [Header("Behavior")]
    public bool autoWireButtons = true;
    public bool skipAutoWireWhenButtonHasPersistentEvents = true;
    public bool hidePanelsOnStart = true;
    public bool closeOtherPanelsWhenOpening = true;

    private void Start()
    {
        if (hidePanelsOnStart)
            CloseAllPanels();
    }

    private void OnEnable()
    {
        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void OpenMelodyPanel()
    {
        OpenPanel(melodyPanelCanvas);
    }

    public void OpenHarmonyPanel()
    {
        OpenPanel(harmonyPanelCanvas);
    }

    public void OpenBassPanel()
    {
        OpenPanel(bassPanelCanvas);
    }

    public void OpenDrumsPanel()
    {
        OpenPanel(drumsPanelCanvas);
    }

    public void CloseMelodyPanel()
    {
        SetPanelActive(melodyPanelCanvas, false);
    }

    public void CloseHarmonyPanel()
    {
        SetPanelActive(harmonyPanelCanvas, false);
    }

    public void CloseBassPanel()
    {
        SetPanelActive(bassPanelCanvas, false);
    }

    public void CloseDrumsPanel()
    {
        SetPanelActive(drumsPanelCanvas, false);
    }

    public void CloseAllPanels()
    {
        SetPanelActive(melodyPanelCanvas, false);
        SetPanelActive(harmonyPanelCanvas, false);
        SetPanelActive(bassPanelCanvas, false);
        SetPanelActive(drumsPanelCanvas, false);
    }

    private void OpenPanel(GameObject panelCanvas)
    {
        if (panelCanvas == null)
            return;

        if (closeOtherPanelsWhenOpening)
            CloseAllPanels();

        SetPanelActive(panelCanvas, true);
    }

    private void AddListeners()
    {
        if (!autoWireButtons)
            return;

        AddButtonListener(openMelodyButton, OpenMelodyPanel);
        AddButtonListener(openHarmonyButton, OpenHarmonyPanel);
        AddButtonListener(openBassButton, OpenBassPanel);
        AddButtonListener(openDrumsButton, OpenDrumsPanel);

        AddButtonListener(closeMelodyButton, CloseMelodyPanel);
        AddButtonListener(closeHarmonyButton, CloseHarmonyPanel);
        AddButtonListener(closeBassButton, CloseBassPanel);
        AddButtonListener(closeDrumsButton, CloseDrumsPanel);
    }

    private void RemoveListeners()
    {
        RemoveButtonListener(openMelodyButton, OpenMelodyPanel);
        RemoveButtonListener(openHarmonyButton, OpenHarmonyPanel);
        RemoveButtonListener(openBassButton, OpenBassPanel);
        RemoveButtonListener(openDrumsButton, OpenDrumsPanel);

        RemoveButtonListener(closeMelodyButton, CloseMelodyPanel);
        RemoveButtonListener(closeHarmonyButton, CloseHarmonyPanel);
        RemoveButtonListener(closeBassButton, CloseBassPanel);
        RemoveButtonListener(closeDrumsButton, CloseDrumsPanel);
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

    private static void RemoveButtonListener(Button button, UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

    private static void SetPanelActive(GameObject panelCanvas, bool active)
    {
        if (panelCanvas != null)
            panelCanvas.SetActive(active);
    }
}
