using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ProceduralMusicGenerationUI : MonoBehaviour
{
    [Header("Target")]
    public ProceduralMusicManager musicManager;

    [Header("Generation button")]
    public Button generateButton;

    [Header("Attribute")]
    public TMP_Dropdown attributeDropdown;
    public TextMeshProUGUI attributeValueText;

    [Header("Tempo")]
    public Slider tempoSlider;
    public TextMeshProUGUI tempoValueText;
    [Range(30, 240)] public int minTempo = 50;
    [Range(30, 240)] public int maxTempo = 180;

    [Header("Meter toggles")]
    public Toggle fourFourToggle;
    public Toggle threeFourToggle;
    public Toggle sixEightToggle;
    public TextMeshProUGUI meterValueText;

    [Header("Section bars")]
    public Slider barsPartASlider;
    public Slider barsPartBSlider;
    public TextMeshProUGUI barsPartAValueText;
    public TextMeshProUGUI barsPartBValueText;
    [Range(1, 64)] public int minBars = 1;
    [Range(1, 64)] public int maxBars = 16;

    [Header("Behavior")]
    public bool autoWireControls = true;
    public bool skipAutoWireWhenControlHasPersistentEvents = true;
    public bool syncControlsOnEnable = true;
    public bool stopBeforeGenerate = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        SetupAttributeDropdown();
        AddListeners();

        if (syncControlsOnEnable)
            SyncControlsFromManager();
        else
            PushControlsToManager();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void Generate()
    {
        ResolveReferences();

        if (musicManager == null)
        {
            Debug.LogWarning("[ProceduralMusicGenerationUI] No ProceduralMusicManager assigned.");
            return;
        }

        if (stopBeforeGenerate)
            musicManager.Stop();

        musicManager.GenerateFromCurrentSettings();
    }

    public void SetTempo(float value)
    {
        ResolveReferences();

        int bpm = Mathf.RoundToInt(value);
        bpm = Mathf.Clamp(bpm, minTempo, maxTempo);

        if (musicManager != null)
            musicManager.SetTempo(bpm);

        UpdateTempoLabel(bpm);
    }

    public void SetAttribute(int value)
    {
        ExpressiveMusicAttribute attribute = IndexToAttribute(value);

        if (musicManager != null)
            musicManager.SetAttribute(attribute);

        UpdateAttributeLabel(attribute);
    }

    public void SetFourFour(bool isOn)
    {
        if (isOn)
            SetMeter(MusicalMeter.FourFour);
    }

    public void SetThreeFour(bool isOn)
    {
        if (isOn)
            SetMeter(MusicalMeter.ThreeFour);
    }

    public void SetSixEight(bool isOn)
    {
        if (isOn)
            SetMeter(MusicalMeter.SixEight);
    }

    public void SetBarsPartA(float value)
    {
        ResolveReferences();

        int bars = Mathf.Clamp(Mathf.RoundToInt(value), minBars, maxBars);

        if (musicManager != null)
            musicManager.SetBarsPartA(bars);

        UpdateBarsLabel(barsPartAValueText, bars);
    }

    public void SetBarsPartB(float value)
    {
        ResolveReferences();

        int bars = Mathf.Clamp(Mathf.RoundToInt(value), minBars, maxBars);

        if (musicManager != null)
            musicManager.SetBarsPartB(bars);

        UpdateBarsLabel(barsPartBValueText, bars);
    }

    public void SyncControlsFromManager()
    {
        ResolveReferences();

        if (musicManager == null || musicManager.settings == null)
            return;

        SetupSliderRanges();

        SetDropdownWithoutNotify(attributeDropdown, (int)musicManager.settings.selectedAttribute);
        SetSliderWithoutNotify(tempoSlider, musicManager.settings.bpm);
        SetSliderWithoutNotify(barsPartASlider, musicManager.settings.barsPartA);
        SetSliderWithoutNotify(barsPartBSlider, musicManager.settings.barsPartB);
        SetMeterTogglesWithoutNotify(musicManager.settings.meter);

        UpdateAttributeLabel(musicManager.settings.selectedAttribute);
        UpdateTempoLabel(musicManager.settings.bpm);
        UpdateBarsLabel(barsPartAValueText, musicManager.settings.barsPartA);
        UpdateBarsLabel(barsPartBValueText, musicManager.settings.barsPartB);
        UpdateMeterLabel(musicManager.settings.meter);
    }

    public void PushControlsToManager()
    {
        ResolveReferences();

        if (musicManager == null)
            return;

        SetupSliderRanges();

        if (attributeDropdown != null) SetAttribute(attributeDropdown.value);
        if (tempoSlider != null) SetTempo(tempoSlider.value);
        if (barsPartASlider != null) SetBarsPartA(barsPartASlider.value);
        if (barsPartBSlider != null) SetBarsPartB(barsPartBSlider.value);

        if (fourFourToggle != null && fourFourToggle.isOn) SetMeter(MusicalMeter.FourFour);
        else if (threeFourToggle != null && threeFourToggle.isOn) SetMeter(MusicalMeter.ThreeFour);
        else if (sixEightToggle != null && sixEightToggle.isOn) SetMeter(MusicalMeter.SixEight);
    }

    private void SetMeter(MusicalMeter meter)
    {
        ResolveReferences();

        if (musicManager != null)
            musicManager.SetMeter(meter);

        SetMeterTogglesWithoutNotify(meter);
        UpdateMeterLabel(meter);
    }

    private void ResolveReferences()
    {
        if (musicManager == null)
            musicManager = FindFirstObjectByType<ProceduralMusicManager>();
    }

    private void AddListeners()
    {
        if (!autoWireControls)
            return;

        AddButtonListener(generateButton, Generate);

        if (attributeDropdown != null) attributeDropdown.onValueChanged.AddListener(SetAttribute);
        if (tempoSlider != null) tempoSlider.onValueChanged.AddListener(SetTempo);
        if (barsPartASlider != null) barsPartASlider.onValueChanged.AddListener(SetBarsPartA);
        if (barsPartBSlider != null) barsPartBSlider.onValueChanged.AddListener(SetBarsPartB);
        if (fourFourToggle != null) fourFourToggle.onValueChanged.AddListener(SetFourFour);
        if (threeFourToggle != null) threeFourToggle.onValueChanged.AddListener(SetThreeFour);
        if (sixEightToggle != null) sixEightToggle.onValueChanged.AddListener(SetSixEight);
    }

    private void RemoveListeners()
    {
        if (generateButton != null) generateButton.onClick.RemoveListener(Generate);
        if (attributeDropdown != null) attributeDropdown.onValueChanged.RemoveListener(SetAttribute);
        if (tempoSlider != null) tempoSlider.onValueChanged.RemoveListener(SetTempo);
        if (barsPartASlider != null) barsPartASlider.onValueChanged.RemoveListener(SetBarsPartA);
        if (barsPartBSlider != null) barsPartBSlider.onValueChanged.RemoveListener(SetBarsPartB);
        if (fourFourToggle != null) fourFourToggle.onValueChanged.RemoveListener(SetFourFour);
        if (threeFourToggle != null) threeFourToggle.onValueChanged.RemoveListener(SetThreeFour);
        if (sixEightToggle != null) sixEightToggle.onValueChanged.RemoveListener(SetSixEight);
    }

    private void AddButtonListener(Button button, UnityAction action)
    {
        if (button == null)
            return;

        if (skipAutoWireWhenControlHasPersistentEvents && button.onClick.GetPersistentEventCount() > 0)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void SetupSliderRanges()
    {
        if (minTempo > maxTempo)
        {
            int temp = minTempo;
            minTempo = maxTempo;
            maxTempo = temp;
        }

        if (minBars > maxBars)
        {
            int temp = minBars;
            minBars = maxBars;
            maxBars = temp;
        }

        SetupSlider(tempoSlider, minTempo, maxTempo, true);
        SetupSlider(barsPartASlider, minBars, maxBars, true);
        SetupSlider(barsPartBSlider, minBars, maxBars, true);
    }

    private static void SetupSlider(Slider slider, int min, int max, bool wholeNumbers)
    {
        if (slider == null)
            return;

        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
    }

    private static void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
            return;

        slider.SetValueWithoutNotify(Mathf.Clamp(value, slider.minValue, slider.maxValue));
    }

    private void SetupAttributeDropdown()
    {
        if (attributeDropdown == null)
            return;

        int count = System.Enum.GetValues(typeof(ExpressiveMusicAttribute)).Length;
        if (attributeDropdown.options.Count == count)
            return;

        List<string> options = new List<string>();
        for (int i = 0; i < count; i++)
            options.Add(GetAttributeDisplayName((ExpressiveMusicAttribute)i));

        attributeDropdown.ClearOptions();
        attributeDropdown.AddOptions(options);
    }

    private static ExpressiveMusicAttribute IndexToAttribute(int index)
    {
        ExpressiveMusicAttribute[] values = (ExpressiveMusicAttribute[])System.Enum.GetValues(typeof(ExpressiveMusicAttribute));
        if (values.Length == 0)
            return ExpressiveMusicAttribute.Happy;

        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    private static void SetDropdownWithoutNotify(TMP_Dropdown dropdown, int value)
    {
        if (dropdown == null)
            return;

        int maxValue = Mathf.Max(0, dropdown.options.Count - 1);
        dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, maxValue));
    }

    private void SetMeterTogglesWithoutNotify(MusicalMeter meter)
    {
        if (fourFourToggle != null) fourFourToggle.SetIsOnWithoutNotify(meter == MusicalMeter.FourFour);
        if (threeFourToggle != null) threeFourToggle.SetIsOnWithoutNotify(meter == MusicalMeter.ThreeFour);
        if (sixEightToggle != null) sixEightToggle.SetIsOnWithoutNotify(meter == MusicalMeter.SixEight);
    }

    private void UpdateTempoLabel(int bpm)
    {
        if (tempoValueText != null)
            tempoValueText.SetText(bpm.ToString() + " BPM");
    }

    private void UpdateAttributeLabel(ExpressiveMusicAttribute attribute)
    {
        if (attributeValueText != null)
            attributeValueText.SetText(GetAttributeDisplayName(attribute));
    }

    private static string GetAttributeDisplayName(ExpressiveMusicAttribute attribute)
    {
        switch (attribute)
        {
            case ExpressiveMusicAttribute.Happy:
                return "Alegre";
            case ExpressiveMusicAttribute.Sad:
                return "Triste";
            case ExpressiveMusicAttribute.Mysterious:
                return "Misterioso";
            case ExpressiveMusicAttribute.Heroic:
                return "Heroico";
            case ExpressiveMusicAttribute.Tender:
                return "Tierno";
            case ExpressiveMusicAttribute.Dark:
                return "Oscuro";
            case ExpressiveMusicAttribute.Energetic:
                return "Energético";
            case ExpressiveMusicAttribute.Calm:
                return "Calmado";
            case ExpressiveMusicAttribute.Threatening:
                return "Amenazante";
            case ExpressiveMusicAttribute.Magical:
                return "Mágico";
            case ExpressiveMusicAttribute.Childlike:
                return "Infantil";
            case ExpressiveMusicAttribute.Epic:
                return "Épico";
            default:
                return attribute.ToString();
        }
    }

    private void UpdateBarsLabel(TextMeshProUGUI label, int bars)
    {
        if (label != null)
            label.SetText(bars.ToString());
    }

    private void UpdateMeterLabel(MusicalMeter meter)
    {
        if (meterValueText != null)
            meterValueText.SetText(MusicalMeterUtility.GetDisplayName(meter));
    }
}
