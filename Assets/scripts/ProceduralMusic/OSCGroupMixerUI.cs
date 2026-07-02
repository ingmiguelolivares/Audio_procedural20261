using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class OSCGroupMixerUI : MonoBehaviour
{
    [Header("Target")]
    public OSCController oscController;

    [Header("Sliders")]
    public Slider melodySlider;
    public Slider harmonySlider;
    public Slider bassSlider;
    public Slider kickSlider;
    public Slider snareSlider;
    public Slider hiHatSlider;

    [Header("Melodic articulation sliders")]
    public Slider melodyPercussiveSlider;
    public Slider melodySustainSlider;
    public Slider bassPercussiveSlider;
    public Slider bassSustainSlider;
    public Slider harmonyPercussiveSlider;
    public Slider harmonySustainSlider;

    [Header("Optional value labels")]
    public TextMeshProUGUI melodyValueText;
    public TextMeshProUGUI harmonyValueText;
    public TextMeshProUGUI bassValueText;
    public TextMeshProUGUI kickValueText;
    public TextMeshProUGUI snareValueText;
    public TextMeshProUGUI hiHatValueText;

    [Header("Optional articulation labels")]
    public TextMeshProUGUI melodyPercussiveValueText;
    public TextMeshProUGUI melodySustainValueText;
    public TextMeshProUGUI bassPercussiveValueText;
    public TextMeshProUGUI bassSustainValueText;
    public TextMeshProUGUI harmonyPercussiveValueText;
    public TextMeshProUGUI harmonySustainValueText;

    [Header("Melodic sound dropdowns")]
    public TMP_Dropdown melodyWaveformDropdown;
    public TMP_Dropdown harmonyWaveformDropdown;
    public TMP_Dropdown bassWaveformDropdown;

    [Header("Melodic sound sliders")]
    public Slider melodyDetuneSlider;
    public Slider harmonyDetuneSlider;
    public Slider bassDetuneSlider;
    public Slider melodyFilterLevelSlider;
    public Slider harmonyFilterLevelSlider;
    public Slider bassFilterLevelSlider;
    public Slider melodyFMSlider;
    public Slider harmonyFMSlider;
    public Slider bassFMSlider;
    public Slider melodyDistortionLevelSlider;
    public Slider harmonyDistortionLevelSlider;
    public Slider bassDistortionLevelSlider;

    [Header("Drum effect sliders")]
    public Slider kickDistortionLevelSlider;
    public Slider snareDistortionLevelSlider;
    public Slider hiHatDistortionLevelSlider;

    [Header("Drum layer balance sliders")]
    public Slider kickNoiseHarmonicBalanceSlider;
    public Slider snareHighLowNoiseBalanceSlider;

    [Header("Melodic octave sliders")]
    public Slider melodyOctaveSlider;
    public Slider harmonyOctaveSlider;

    [Header("Melodic LFO sliders")]
    [FormerlySerializedAs("melodyLfoFrequencySlider")]
    public Slider melodyTremLfoFrequencySlider;
    public Slider melodyVibLfoFrequencySlider;
    [FormerlySerializedAs("harmonyLfoFrequencySlider")]
    public Slider harmonyTremLfoFrequencySlider;
    public Slider harmonyVibLfoFrequencySlider;

    [Header("Melodic sound toggles")]
    public Toggle melodyFilterToggle;
    public Toggle harmonyFilterToggle;
    public Toggle bassFilterToggle;
    public Toggle melodyDistortionToggle;
    public Toggle harmonyDistortionToggle;
    public Toggle bassDistortionToggle;

    [Header("Optional sound labels")]
    public TextMeshProUGUI melodyDetuneValueText;
    public TextMeshProUGUI harmonyDetuneValueText;
    public TextMeshProUGUI bassDetuneValueText;
    public TextMeshProUGUI melodyFilterLevelValueText;
    public TextMeshProUGUI harmonyFilterLevelValueText;
    public TextMeshProUGUI bassFilterLevelValueText;
    public TextMeshProUGUI melodyFMValueText;
    public TextMeshProUGUI harmonyFMValueText;
    public TextMeshProUGUI bassFMValueText;
    public TextMeshProUGUI melodyDistortionLevelValueText;
    public TextMeshProUGUI harmonyDistortionLevelValueText;
    public TextMeshProUGUI bassDistortionLevelValueText;
    public TextMeshProUGUI kickDistortionLevelValueText;
    public TextMeshProUGUI snareDistortionLevelValueText;
    public TextMeshProUGUI hiHatDistortionLevelValueText;
    public TextMeshProUGUI kickNoiseHarmonicBalanceValueText;
    public TextMeshProUGUI snareHighLowNoiseBalanceValueText;
    public TextMeshProUGUI melodyOctaveValueText;
    public TextMeshProUGUI harmonyOctaveValueText;
    [FormerlySerializedAs("melodyLfoFrequencyValueText")]
    public TextMeshProUGUI melodyTremLfoFrequencyValueText;
    public TextMeshProUGUI melodyVibLfoFrequencyValueText;
    [FormerlySerializedAs("harmonyLfoFrequencyValueText")]
    public TextMeshProUGUI harmonyTremLfoFrequencyValueText;
    public TextMeshProUGUI harmonyVibLfoFrequencyValueText;

    [Header("Optional FM control roots")]
    public GameObject melodyFMControlRoot;
    public GameObject harmonyFMControlRoot;
    public GameObject bassFMControlRoot;

    [Header("Behavior")]
    public bool syncSlidersOnStart = true;
    public bool showPercentLabels = true;
    [Range(1f, 1200f)] public float detuneSliderRangeCents = 100f;
    [Range(1f, 4f)] public float distortionSliderMax = 4f;
    [Range(1f, 5f)] public float drumDistortionSliderMax = 5f;
    [Range(1f, 24f)] public float lfoFrequencySliderMax = 12f;
    [HideInInspector]
    [Range(1, 4096)] public int detuneSliderRangeFrames = 512;

    private void Awake()
    {
        if (oscController == null)
            oscController = FindFirstObjectByType<OSCController>();
    }

    private void OnEnable()
    {
        SetupWaveformDropdowns();
        ConfigureEffectSliderRanges();
        AddListeners();

        if (syncSlidersOnStart)
            SyncSlidersFromController();
        else
            PushSliderValuesToController();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void SetMelodyVolume(float value)
    {
        SetVolume(OSCGroupRole.Melody, value, melodyValueText);
    }

    public void SetHarmonyVolume(float value)
    {
        SetVolume(OSCGroupRole.Harmony, value, harmonyValueText);
    }

    public void SetBassVolume(float value)
    {
        SetVolume(OSCGroupRole.Bass, value, bassValueText);
    }

    public void SetKickVolume(float value)
    {
        SetVolume(OSCGroupRole.Kick, value, kickValueText);
    }

    public void SetSnareVolume(float value)
    {
        SetVolume(OSCGroupRole.Snare, value, snareValueText);
    }

    public void SetHiHatVolume(float value)
    {
        SetVolume(OSCGroupRole.HiHat, value, hiHatValueText);
    }

    public void SetMelodyPercussive(float value)
    {
        SetPercussive(OSCGroupRole.Melody, value, melodyPercussiveValueText);
    }

    public void SetMelodySustain(float value)
    {
        SetSustain(OSCGroupRole.Melody, value, melodySustainValueText);
    }

    public void SetBassPercussive(float value)
    {
        SetPercussive(OSCGroupRole.Bass, value, bassPercussiveValueText);
    }

    public void SetBassSustain(float value)
    {
        SetSustain(OSCGroupRole.Bass, value, bassSustainValueText);
    }

    public void SetHarmonyPercussive(float value)
    {
        SetPercussive(OSCGroupRole.Harmony, value, harmonyPercussiveValueText);
    }

    public void SetHarmonySustain(float value)
    {
        SetSustain(OSCGroupRole.Harmony, value, harmonySustainValueText);
    }

    public void SetMelodyWaveform(int value)
    {
        SetWaveform(OSCGroupRole.Melody, value);
    }

    public void SetHarmonyWaveform(int value)
    {
        SetWaveform(OSCGroupRole.Harmony, value);
    }

    public void SetBassWaveform(int value)
    {
        SetWaveform(OSCGroupRole.Bass, value);
    }

    public void SetMelodyDetune(float value)
    {
        SetDetune(OSCGroupRole.Melody, value, melodyDetuneValueText);
    }

    public void SetHarmonyDetune(float value)
    {
        SetDetune(OSCGroupRole.Harmony, value, harmonyDetuneValueText);
    }

    public void SetBassDetune(float value)
    {
        SetDetune(OSCGroupRole.Bass, value, bassDetuneValueText);
    }

    public void SetMelodyFilterModulation(bool enabled)
    {
        SetFilterModulation(OSCGroupRole.Melody, enabled);
    }

    public void SetHarmonyFilterModulation(bool enabled)
    {
        SetFilterModulation(OSCGroupRole.Harmony, enabled);
    }

    public void SetBassFilterModulation(bool enabled)
    {
        SetFilterModulation(OSCGroupRole.Bass, enabled);
    }

    public void SetMelodyFilterLevel(float value)
    {
        SetFilterLevel(OSCGroupRole.Melody, value, melodyFilterLevelValueText);
    }

    public void SetHarmonyFilterLevel(float value)
    {
        SetFilterLevel(OSCGroupRole.Harmony, value, harmonyFilterLevelValueText);
    }

    public void SetBassFilterLevel(float value)
    {
        SetFilterLevel(OSCGroupRole.Bass, value, bassFilterLevelValueText);
    }

    public void SetMelodyFMLevel(float value)
    {
        SetFMLevel(OSCGroupRole.Melody, value, melodyFMValueText);
    }

    public void SetHarmonyFMLevel(float value)
    {
        SetFMLevel(OSCGroupRole.Harmony, value, harmonyFMValueText);
    }

    public void SetBassFMLevel(float value)
    {
        SetFMLevel(OSCGroupRole.Bass, value, bassFMValueText);
    }

    public void SetMelodyDistortion(bool enabled)
    {
        SetDistortion(OSCGroupRole.Melody, enabled);
    }

    public void SetHarmonyDistortion(bool enabled)
    {
        SetDistortion(OSCGroupRole.Harmony, enabled);
    }

    public void SetBassDistortion(bool enabled)
    {
        SetDistortion(OSCGroupRole.Bass, enabled);
    }

    public void SetMelodyDistortionLevel(float value)
    {
        SetDistortionLevel(OSCGroupRole.Melody, value, melodyDistortionLevelValueText);
    }

    public void SetHarmonyDistortionLevel(float value)
    {
        SetDistortionLevel(OSCGroupRole.Harmony, value, harmonyDistortionLevelValueText);
    }

    public void SetBassDistortionLevel(float value)
    {
        SetDistortionLevel(OSCGroupRole.Bass, value, bassDistortionLevelValueText);
    }

    public void SetKickDistortionLevel(float value)
    {
        SetDrumDistortionLevel(OSCGroupRole.Kick, value, kickDistortionLevelValueText);
    }

    public void SetSnareDistortionLevel(float value)
    {
        SetDrumDistortionLevel(OSCGroupRole.Snare, value, snareDistortionLevelValueText);
    }

    public void SetHiHatDistortionLevel(float value)
    {
        SetDrumDistortionLevel(OSCGroupRole.HiHat, value, hiHatDistortionLevelValueText);
    }

    public void SetKickNoiseHarmonicBalance(float value)
    {
        value = Mathf.Clamp01(value);

        if (oscController != null)
            oscController.SetKickNoiseHarmonicBalance(value);

        UpdateBalanceLabel(kickNoiseHarmonicBalanceValueText, value);
    }

    public void SetSnareHighLowNoiseBalance(float value)
    {
        value = Mathf.Clamp01(value);

        if (oscController != null)
            oscController.SetSnareHighLowNoiseBalance(value);

        UpdateBalanceLabel(snareHighLowNoiseBalanceValueText, value);
    }

    public void SetMelodyOctave(float value)
    {
        SetPlaybackOctave(OSCGroupRole.Melody, value, melodyOctaveValueText);
    }

    public void SetHarmonyOctave(float value)
    {
        SetPlaybackOctave(OSCGroupRole.Harmony, value, harmonyOctaveValueText);
    }

    public void SetMelodyLfoFrequency(float value)
    {
        SetLfoFrequency(OSCGroupRole.Melody, value, value, melodyTremLfoFrequencyValueText, melodyVibLfoFrequencyValueText);
    }

    public void SetHarmonyLfoFrequency(float value)
    {
        SetLfoFrequency(OSCGroupRole.Harmony, value, value, harmonyTremLfoFrequencyValueText, harmonyVibLfoFrequencyValueText);
    }

    public void SetMelodyTremLfoFrequency(float value)
    {
        SetTremLfoFrequency(OSCGroupRole.Melody, value, melodyTremLfoFrequencyValueText);
    }

    public void SetMelodyVibLfoFrequency(float value)
    {
        SetVibLfoFrequency(OSCGroupRole.Melody, value, melodyVibLfoFrequencyValueText);
    }

    public void SetHarmonyTremLfoFrequency(float value)
    {
        SetTremLfoFrequency(OSCGroupRole.Harmony, value, harmonyTremLfoFrequencyValueText);
    }

    public void SetHarmonyVibLfoFrequency(float value)
    {
        SetVibLfoFrequency(OSCGroupRole.Harmony, value, harmonyVibLfoFrequencyValueText);
    }

    public void SyncSlidersFromController()
    {
        if (oscController == null)
            return;

        SetSliderWithoutNotify(melodySlider, oscController.GetGroupVolume(OSCGroupRole.Melody));
        SetSliderWithoutNotify(harmonySlider, oscController.GetGroupVolume(OSCGroupRole.Harmony));
        SetSliderWithoutNotify(bassSlider, oscController.GetGroupVolume(OSCGroupRole.Bass));
        SetSliderWithoutNotify(kickSlider, oscController.GetGroupVolume(OSCGroupRole.Kick));
        SetSliderWithoutNotify(snareSlider, oscController.GetGroupVolume(OSCGroupRole.Snare));
        SetSliderWithoutNotify(hiHatSlider, oscController.GetGroupVolume(OSCGroupRole.HiHat));

        SetSliderWithoutNotify(melodyPercussiveSlider, oscController.GetInstrumentPercussiveAmount(OSCGroupRole.Melody));
        SetSliderWithoutNotify(melodySustainSlider, oscController.GetInstrumentSustainAmount(OSCGroupRole.Melody));
        SetSliderWithoutNotify(bassPercussiveSlider, oscController.GetInstrumentPercussiveAmount(OSCGroupRole.Bass));
        SetSliderWithoutNotify(bassSustainSlider, oscController.GetInstrumentSustainAmount(OSCGroupRole.Bass));
        SetSliderWithoutNotify(harmonyPercussiveSlider, oscController.GetInstrumentPercussiveAmount(OSCGroupRole.Harmony));
        SetSliderWithoutNotify(harmonySustainSlider, oscController.GetInstrumentSustainAmount(OSCGroupRole.Harmony));

        SetDropdownWithoutNotify(melodyWaveformDropdown, (int)oscController.GetGroupWaveform(OSCGroupRole.Melody));
        SetDropdownWithoutNotify(harmonyWaveformDropdown, (int)oscController.GetGroupWaveform(OSCGroupRole.Harmony));
        SetDropdownWithoutNotify(bassWaveformDropdown, (int)oscController.GetGroupWaveform(OSCGroupRole.Bass));

        SetDetuneSliderWithoutNotify(melodyDetuneSlider, oscController.GetGroupDetuneCents(OSCGroupRole.Melody));
        SetDetuneSliderWithoutNotify(harmonyDetuneSlider, oscController.GetGroupDetuneCents(OSCGroupRole.Harmony));
        SetDetuneSliderWithoutNotify(bassDetuneSlider, oscController.GetGroupDetuneCents(OSCGroupRole.Bass));
        SetSliderWithoutNotify(melodyFilterLevelSlider, oscController.GetGroupFilterLevel(OSCGroupRole.Melody));
        SetSliderWithoutNotify(harmonyFilterLevelSlider, oscController.GetGroupFilterLevel(OSCGroupRole.Harmony));
        SetSliderWithoutNotify(bassFilterLevelSlider, oscController.GetGroupFilterLevel(OSCGroupRole.Bass));
        SetSliderWithoutNotify(melodyFMSlider, oscController.GetGroupFMLevel(OSCGroupRole.Melody));
        SetSliderWithoutNotify(harmonyFMSlider, oscController.GetGroupFMLevel(OSCGroupRole.Harmony));
        SetSliderWithoutNotify(bassFMSlider, oscController.GetGroupFMLevel(OSCGroupRole.Bass));
        SetDistortionSliderWithoutNotify(melodyDistortionLevelSlider, oscController.GetGroupDistortionAmount(OSCGroupRole.Melody));
        SetDistortionSliderWithoutNotify(harmonyDistortionLevelSlider, oscController.GetGroupDistortionAmount(OSCGroupRole.Harmony));
        SetDistortionSliderWithoutNotify(bassDistortionLevelSlider, oscController.GetGroupDistortionAmount(OSCGroupRole.Bass));
        SetDrumDistortionSliderWithoutNotify(kickDistortionLevelSlider, oscController.GetDrumDistortionAmount(OSCGroupRole.Kick));
        SetDrumDistortionSliderWithoutNotify(snareDistortionLevelSlider, oscController.GetDrumDistortionAmount(OSCGroupRole.Snare));
        SetDrumDistortionSliderWithoutNotify(hiHatDistortionLevelSlider, oscController.GetDrumDistortionAmount(OSCGroupRole.HiHat));
        SetSliderWithoutNotify(kickNoiseHarmonicBalanceSlider, oscController.GetKickNoiseHarmonicBalance());
        SetSliderWithoutNotify(snareHighLowNoiseBalanceSlider, oscController.GetSnareHighLowNoiseBalance());
        SetOctaveSliderWithoutNotify(melodyOctaveSlider, oscController.GetGroupPlaybackOctave(OSCGroupRole.Melody));
        SetOctaveSliderWithoutNotify(harmonyOctaveSlider, oscController.GetGroupPlaybackOctave(OSCGroupRole.Harmony));
        SetLfoSliderWithoutNotify(melodyTremLfoFrequencySlider, oscController.GetGroupTremLfoFrequency(OSCGroupRole.Melody));
        SetLfoSliderWithoutNotify(melodyVibLfoFrequencySlider, oscController.GetGroupVibLfoFrequency(OSCGroupRole.Melody));
        SetLfoSliderWithoutNotify(harmonyTremLfoFrequencySlider, oscController.GetGroupTremLfoFrequency(OSCGroupRole.Harmony));
        SetLfoSliderWithoutNotify(harmonyVibLfoFrequencySlider, oscController.GetGroupVibLfoFrequency(OSCGroupRole.Harmony));

        SetToggleWithoutNotify(melodyFilterToggle, oscController.GetGroupFilterModulation(OSCGroupRole.Melody));
        SetToggleWithoutNotify(harmonyFilterToggle, oscController.GetGroupFilterModulation(OSCGroupRole.Harmony));
        SetToggleWithoutNotify(bassFilterToggle, oscController.GetGroupFilterModulation(OSCGroupRole.Bass));
        SetToggleWithoutNotify(melodyDistortionToggle, oscController.GetGroupDistortion(OSCGroupRole.Melody));
        SetToggleWithoutNotify(harmonyDistortionToggle, oscController.GetGroupDistortion(OSCGroupRole.Harmony));
        SetToggleWithoutNotify(bassDistortionToggle, oscController.GetGroupDistortion(OSCGroupRole.Bass));

        RefreshAllLabels();
        UpdateAllFMControlVisibility();
    }

    public void PushSliderValuesToController()
    {
        if (oscController == null)
            return;

        if (melodySlider != null) SetMelodyVolume(melodySlider.value);
        if (harmonySlider != null) SetHarmonyVolume(harmonySlider.value);
        if (bassSlider != null) SetBassVolume(bassSlider.value);
        if (kickSlider != null) SetKickVolume(kickSlider.value);
        if (snareSlider != null) SetSnareVolume(snareSlider.value);
        if (hiHatSlider != null) SetHiHatVolume(hiHatSlider.value);

        if (melodyPercussiveSlider != null) SetMelodyPercussive(melodyPercussiveSlider.value);
        if (melodySustainSlider != null) SetMelodySustain(melodySustainSlider.value);
        if (bassPercussiveSlider != null) SetBassPercussive(bassPercussiveSlider.value);
        if (bassSustainSlider != null) SetBassSustain(bassSustainSlider.value);
        if (harmonyPercussiveSlider != null) SetHarmonyPercussive(harmonyPercussiveSlider.value);
        if (harmonySustainSlider != null) SetHarmonySustain(harmonySustainSlider.value);

        if (melodyWaveformDropdown != null) SetMelodyWaveform(melodyWaveformDropdown.value);
        if (harmonyWaveformDropdown != null) SetHarmonyWaveform(harmonyWaveformDropdown.value);
        if (bassWaveformDropdown != null) SetBassWaveform(bassWaveformDropdown.value);
        if (melodyDetuneSlider != null) SetMelodyDetune(melodyDetuneSlider.value);
        if (harmonyDetuneSlider != null) SetHarmonyDetune(harmonyDetuneSlider.value);
        if (bassDetuneSlider != null) SetBassDetune(bassDetuneSlider.value);
        if (melodyFilterToggle != null) SetMelodyFilterModulation(melodyFilterToggle.isOn);
        if (harmonyFilterToggle != null) SetHarmonyFilterModulation(harmonyFilterToggle.isOn);
        if (bassFilterToggle != null) SetBassFilterModulation(bassFilterToggle.isOn);
        if (melodyFilterLevelSlider != null) SetMelodyFilterLevel(melodyFilterLevelSlider.value);
        if (harmonyFilterLevelSlider != null) SetHarmonyFilterLevel(harmonyFilterLevelSlider.value);
        if (bassFilterLevelSlider != null) SetBassFilterLevel(bassFilterLevelSlider.value);
        if (melodyFMSlider != null) SetMelodyFMLevel(melodyFMSlider.value);
        if (harmonyFMSlider != null) SetHarmonyFMLevel(harmonyFMSlider.value);
        if (bassFMSlider != null) SetBassFMLevel(bassFMSlider.value);
        if (melodyDistortionToggle != null) SetMelodyDistortion(melodyDistortionToggle.isOn);
        if (harmonyDistortionToggle != null) SetHarmonyDistortion(harmonyDistortionToggle.isOn);
        if (bassDistortionToggle != null) SetBassDistortion(bassDistortionToggle.isOn);
        if (melodyDistortionLevelSlider != null) SetMelodyDistortionLevel(melodyDistortionLevelSlider.value);
        if (harmonyDistortionLevelSlider != null) SetHarmonyDistortionLevel(harmonyDistortionLevelSlider.value);
        if (bassDistortionLevelSlider != null) SetBassDistortionLevel(bassDistortionLevelSlider.value);
        if (kickDistortionLevelSlider != null) SetKickDistortionLevel(kickDistortionLevelSlider.value);
        if (snareDistortionLevelSlider != null) SetSnareDistortionLevel(snareDistortionLevelSlider.value);
        if (hiHatDistortionLevelSlider != null) SetHiHatDistortionLevel(hiHatDistortionLevelSlider.value);
        if (kickNoiseHarmonicBalanceSlider != null) SetKickNoiseHarmonicBalance(kickNoiseHarmonicBalanceSlider.value);
        if (snareHighLowNoiseBalanceSlider != null) SetSnareHighLowNoiseBalance(snareHighLowNoiseBalanceSlider.value);
        if (melodyOctaveSlider != null) SetMelodyOctave(melodyOctaveSlider.value);
        if (harmonyOctaveSlider != null) SetHarmonyOctave(harmonyOctaveSlider.value);
        if (melodyTremLfoFrequencySlider != null) SetMelodyTremLfoFrequency(melodyTremLfoFrequencySlider.value);
        if (melodyVibLfoFrequencySlider != null) SetMelodyVibLfoFrequency(melodyVibLfoFrequencySlider.value);
        if (harmonyTremLfoFrequencySlider != null) SetHarmonyTremLfoFrequency(harmonyTremLfoFrequencySlider.value);
        if (harmonyVibLfoFrequencySlider != null) SetHarmonyVibLfoFrequency(harmonyVibLfoFrequencySlider.value);

        UpdateAllFMControlVisibility();
    }

    private void SetVolume(OSCGroupRole groupRole, float value, TextMeshProUGUI label)
    {
        value = Mathf.Clamp01(value);

        if (oscController != null)
            oscController.SetGroupVolume(groupRole, value);

        UpdateLabel(label, value);
    }

    private void SetPercussive(OSCGroupRole groupRole, float value, TextMeshProUGUI label)
    {
        value = Mathf.Clamp01(value);

        if (oscController != null)
            oscController.SetInstrumentPercussiveAmount(groupRole, value);

        UpdateLabel(label, value);
    }

    private void SetSustain(OSCGroupRole groupRole, float value, TextMeshProUGUI label)
    {
        value = Mathf.Clamp01(value);

        if (oscController != null)
            oscController.SetInstrumentSustainAmount(groupRole, value);

        UpdateLabel(label, value);
    }

    private void SetWaveform(OSCGroupRole groupRole, int value)
    {
        if (oscController != null)
            oscController.SetGroupWaveform(groupRole, value);

        Osc.WaveFormType waveform = oscController != null
            ? oscController.GetGroupWaveform(groupRole)
            : GetWaveformFromIndex(value);

        UpdateFMControlVisibility(groupRole, waveform);
    }

    private void SetDetune(OSCGroupRole groupRole, float value, TextMeshProUGUI label)
    {
        float range = Mathf.Abs(detuneSliderRangeCents);
        value = Mathf.Clamp(value, -range, range);

        if (oscController != null)
            oscController.SetGroupDetuneCents(groupRole, value);

        UpdateDetuneLabel(label, value);
    }

    private void SetFilterModulation(OSCGroupRole groupRole, bool enabled)
    {
        if (oscController != null)
            oscController.SetGroupFilterModulation(groupRole, enabled);
    }

    private void SetFilterLevel(OSCGroupRole groupRole, float value, TextMeshProUGUI label)
    {
        value = Mathf.Clamp01(value);

        if (oscController != null)
            oscController.SetGroupFilterLevel(groupRole, value);

        UpdateLabel(label, value);
    }

    private void SetFMLevel(OSCGroupRole groupRole, float value, TextMeshProUGUI label)
    {
        value = Mathf.Clamp01(value);

        if (oscController != null)
            oscController.SetGroupFMLevel(groupRole, value);

        UpdateLabel(label, value);
    }

    private void SetDistortion(OSCGroupRole groupRole, bool enabled)
    {
        if (oscController != null)
            oscController.SetGroupDistortion(groupRole, enabled);
    }

    private void SetDistortionLevel(OSCGroupRole groupRole, float value, TextMeshProUGUI label)
    {
        value = Mathf.Clamp(value, 0f, GetDistortionSliderMax());

        if (oscController != null)
            oscController.SetGroupDistortionAmount(groupRole, value);

        UpdateDistortionLabel(label, value);
    }

    private void SetDrumDistortionLevel(OSCGroupRole groupRole, float value, TextMeshProUGUI label)
    {
        value = Mathf.Clamp(value, 0f, GetDrumDistortionSliderMax());

        if (oscController != null)
            oscController.SetDrumDistortionAmount(groupRole, value);

        UpdateDistortionLabel(label, value);
    }

    private void SetPlaybackOctave(OSCGroupRole groupRole, float value, TextMeshProUGUI label)
    {
        int octave = Mathf.Clamp(Mathf.RoundToInt(value), 3, 8);

        if (oscController != null)
            oscController.SetGroupPlaybackOctave(groupRole, octave);

        UpdateOctaveLabel(label, octave);
    }

    private void SetLfoFrequency(OSCGroupRole groupRole, float tremValue, float vibValue, TextMeshProUGUI tremLabel, TextMeshProUGUI vibLabel)
    {
        tremValue = Mathf.Clamp(tremValue, 0f, GetLfoFrequencySliderMax());
        vibValue = Mathf.Clamp(vibValue, 0f, GetLfoFrequencySliderMax());

        if (oscController != null)
        {
            oscController.SetGroupTremLfoFrequency(groupRole, tremValue);
            oscController.SetGroupVibLfoFrequency(groupRole, vibValue);
        }

        UpdateLfoFrequencyLabel(tremLabel, tremValue);
        UpdateLfoFrequencyLabel(vibLabel, vibValue);
    }

    private void SetTremLfoFrequency(OSCGroupRole groupRole, float value, TextMeshProUGUI label)
    {
        value = Mathf.Clamp(value, 0f, GetLfoFrequencySliderMax());

        if (oscController != null)
            oscController.SetGroupTremLfoFrequency(groupRole, value);

        UpdateLfoFrequencyLabel(label, value);
    }

    private void SetVibLfoFrequency(OSCGroupRole groupRole, float value, TextMeshProUGUI label)
    {
        value = Mathf.Clamp(value, 0f, GetLfoFrequencySliderMax());

        if (oscController != null)
            oscController.SetGroupVibLfoFrequency(groupRole, value);

        UpdateLfoFrequencyLabel(label, value);
    }

    private void AddListeners()
    {
        if (melodySlider != null) melodySlider.onValueChanged.AddListener(SetMelodyVolume);
        if (harmonySlider != null) harmonySlider.onValueChanged.AddListener(SetHarmonyVolume);
        if (bassSlider != null) bassSlider.onValueChanged.AddListener(SetBassVolume);
        if (kickSlider != null) kickSlider.onValueChanged.AddListener(SetKickVolume);
        if (snareSlider != null) snareSlider.onValueChanged.AddListener(SetSnareVolume);
        if (hiHatSlider != null) hiHatSlider.onValueChanged.AddListener(SetHiHatVolume);

        if (melodyPercussiveSlider != null) melodyPercussiveSlider.onValueChanged.AddListener(SetMelodyPercussive);
        if (melodySustainSlider != null) melodySustainSlider.onValueChanged.AddListener(SetMelodySustain);
        if (bassPercussiveSlider != null) bassPercussiveSlider.onValueChanged.AddListener(SetBassPercussive);
        if (bassSustainSlider != null) bassSustainSlider.onValueChanged.AddListener(SetBassSustain);
        if (harmonyPercussiveSlider != null) harmonyPercussiveSlider.onValueChanged.AddListener(SetHarmonyPercussive);
        if (harmonySustainSlider != null) harmonySustainSlider.onValueChanged.AddListener(SetHarmonySustain);

        if (melodyWaveformDropdown != null) melodyWaveformDropdown.onValueChanged.AddListener(SetMelodyWaveform);
        if (harmonyWaveformDropdown != null) harmonyWaveformDropdown.onValueChanged.AddListener(SetHarmonyWaveform);
        if (bassWaveformDropdown != null) bassWaveformDropdown.onValueChanged.AddListener(SetBassWaveform);
        if (melodyDetuneSlider != null) melodyDetuneSlider.onValueChanged.AddListener(SetMelodyDetune);
        if (harmonyDetuneSlider != null) harmonyDetuneSlider.onValueChanged.AddListener(SetHarmonyDetune);
        if (bassDetuneSlider != null) bassDetuneSlider.onValueChanged.AddListener(SetBassDetune);
        if (melodyFilterToggle != null) melodyFilterToggle.onValueChanged.AddListener(SetMelodyFilterModulation);
        if (harmonyFilterToggle != null) harmonyFilterToggle.onValueChanged.AddListener(SetHarmonyFilterModulation);
        if (bassFilterToggle != null) bassFilterToggle.onValueChanged.AddListener(SetBassFilterModulation);
        if (melodyFilterLevelSlider != null) melodyFilterLevelSlider.onValueChanged.AddListener(SetMelodyFilterLevel);
        if (harmonyFilterLevelSlider != null) harmonyFilterLevelSlider.onValueChanged.AddListener(SetHarmonyFilterLevel);
        if (bassFilterLevelSlider != null) bassFilterLevelSlider.onValueChanged.AddListener(SetBassFilterLevel);
        if (melodyFMSlider != null) melodyFMSlider.onValueChanged.AddListener(SetMelodyFMLevel);
        if (harmonyFMSlider != null) harmonyFMSlider.onValueChanged.AddListener(SetHarmonyFMLevel);
        if (bassFMSlider != null) bassFMSlider.onValueChanged.AddListener(SetBassFMLevel);
        if (melodyDistortionToggle != null) melodyDistortionToggle.onValueChanged.AddListener(SetMelodyDistortion);
        if (harmonyDistortionToggle != null) harmonyDistortionToggle.onValueChanged.AddListener(SetHarmonyDistortion);
        if (bassDistortionToggle != null) bassDistortionToggle.onValueChanged.AddListener(SetBassDistortion);
        if (melodyDistortionLevelSlider != null) melodyDistortionLevelSlider.onValueChanged.AddListener(SetMelodyDistortionLevel);
        if (harmonyDistortionLevelSlider != null) harmonyDistortionLevelSlider.onValueChanged.AddListener(SetHarmonyDistortionLevel);
        if (bassDistortionLevelSlider != null) bassDistortionLevelSlider.onValueChanged.AddListener(SetBassDistortionLevel);
        if (kickDistortionLevelSlider != null) kickDistortionLevelSlider.onValueChanged.AddListener(SetKickDistortionLevel);
        if (snareDistortionLevelSlider != null) snareDistortionLevelSlider.onValueChanged.AddListener(SetSnareDistortionLevel);
        if (hiHatDistortionLevelSlider != null) hiHatDistortionLevelSlider.onValueChanged.AddListener(SetHiHatDistortionLevel);
        if (kickNoiseHarmonicBalanceSlider != null) kickNoiseHarmonicBalanceSlider.onValueChanged.AddListener(SetKickNoiseHarmonicBalance);
        if (snareHighLowNoiseBalanceSlider != null) snareHighLowNoiseBalanceSlider.onValueChanged.AddListener(SetSnareHighLowNoiseBalance);
        if (melodyOctaveSlider != null) melodyOctaveSlider.onValueChanged.AddListener(SetMelodyOctave);
        if (harmonyOctaveSlider != null) harmonyOctaveSlider.onValueChanged.AddListener(SetHarmonyOctave);
        if (melodyTremLfoFrequencySlider != null) melodyTremLfoFrequencySlider.onValueChanged.AddListener(SetMelodyTremLfoFrequency);
        if (melodyVibLfoFrequencySlider != null) melodyVibLfoFrequencySlider.onValueChanged.AddListener(SetMelodyVibLfoFrequency);
        if (harmonyTremLfoFrequencySlider != null) harmonyTremLfoFrequencySlider.onValueChanged.AddListener(SetHarmonyTremLfoFrequency);
        if (harmonyVibLfoFrequencySlider != null) harmonyVibLfoFrequencySlider.onValueChanged.AddListener(SetHarmonyVibLfoFrequency);
    }

    private void RemoveListeners()
    {
        if (melodySlider != null) melodySlider.onValueChanged.RemoveListener(SetMelodyVolume);
        if (harmonySlider != null) harmonySlider.onValueChanged.RemoveListener(SetHarmonyVolume);
        if (bassSlider != null) bassSlider.onValueChanged.RemoveListener(SetBassVolume);
        if (kickSlider != null) kickSlider.onValueChanged.RemoveListener(SetKickVolume);
        if (snareSlider != null) snareSlider.onValueChanged.RemoveListener(SetSnareVolume);
        if (hiHatSlider != null) hiHatSlider.onValueChanged.RemoveListener(SetHiHatVolume);

        if (melodyPercussiveSlider != null) melodyPercussiveSlider.onValueChanged.RemoveListener(SetMelodyPercussive);
        if (melodySustainSlider != null) melodySustainSlider.onValueChanged.RemoveListener(SetMelodySustain);
        if (bassPercussiveSlider != null) bassPercussiveSlider.onValueChanged.RemoveListener(SetBassPercussive);
        if (bassSustainSlider != null) bassSustainSlider.onValueChanged.RemoveListener(SetBassSustain);
        if (harmonyPercussiveSlider != null) harmonyPercussiveSlider.onValueChanged.RemoveListener(SetHarmonyPercussive);
        if (harmonySustainSlider != null) harmonySustainSlider.onValueChanged.RemoveListener(SetHarmonySustain);

        if (melodyWaveformDropdown != null) melodyWaveformDropdown.onValueChanged.RemoveListener(SetMelodyWaveform);
        if (harmonyWaveformDropdown != null) harmonyWaveformDropdown.onValueChanged.RemoveListener(SetHarmonyWaveform);
        if (bassWaveformDropdown != null) bassWaveformDropdown.onValueChanged.RemoveListener(SetBassWaveform);
        if (melodyDetuneSlider != null) melodyDetuneSlider.onValueChanged.RemoveListener(SetMelodyDetune);
        if (harmonyDetuneSlider != null) harmonyDetuneSlider.onValueChanged.RemoveListener(SetHarmonyDetune);
        if (bassDetuneSlider != null) bassDetuneSlider.onValueChanged.RemoveListener(SetBassDetune);
        if (melodyFilterToggle != null) melodyFilterToggle.onValueChanged.RemoveListener(SetMelodyFilterModulation);
        if (harmonyFilterToggle != null) harmonyFilterToggle.onValueChanged.RemoveListener(SetHarmonyFilterModulation);
        if (bassFilterToggle != null) bassFilterToggle.onValueChanged.RemoveListener(SetBassFilterModulation);
        if (melodyFilterLevelSlider != null) melodyFilterLevelSlider.onValueChanged.RemoveListener(SetMelodyFilterLevel);
        if (harmonyFilterLevelSlider != null) harmonyFilterLevelSlider.onValueChanged.RemoveListener(SetHarmonyFilterLevel);
        if (bassFilterLevelSlider != null) bassFilterLevelSlider.onValueChanged.RemoveListener(SetBassFilterLevel);
        if (melodyFMSlider != null) melodyFMSlider.onValueChanged.RemoveListener(SetMelodyFMLevel);
        if (harmonyFMSlider != null) harmonyFMSlider.onValueChanged.RemoveListener(SetHarmonyFMLevel);
        if (bassFMSlider != null) bassFMSlider.onValueChanged.RemoveListener(SetBassFMLevel);
        if (melodyDistortionToggle != null) melodyDistortionToggle.onValueChanged.RemoveListener(SetMelodyDistortion);
        if (harmonyDistortionToggle != null) harmonyDistortionToggle.onValueChanged.RemoveListener(SetHarmonyDistortion);
        if (bassDistortionToggle != null) bassDistortionToggle.onValueChanged.RemoveListener(SetBassDistortion);
        if (melodyDistortionLevelSlider != null) melodyDistortionLevelSlider.onValueChanged.RemoveListener(SetMelodyDistortionLevel);
        if (harmonyDistortionLevelSlider != null) harmonyDistortionLevelSlider.onValueChanged.RemoveListener(SetHarmonyDistortionLevel);
        if (bassDistortionLevelSlider != null) bassDistortionLevelSlider.onValueChanged.RemoveListener(SetBassDistortionLevel);
        if (kickDistortionLevelSlider != null) kickDistortionLevelSlider.onValueChanged.RemoveListener(SetKickDistortionLevel);
        if (snareDistortionLevelSlider != null) snareDistortionLevelSlider.onValueChanged.RemoveListener(SetSnareDistortionLevel);
        if (hiHatDistortionLevelSlider != null) hiHatDistortionLevelSlider.onValueChanged.RemoveListener(SetHiHatDistortionLevel);
        if (kickNoiseHarmonicBalanceSlider != null) kickNoiseHarmonicBalanceSlider.onValueChanged.RemoveListener(SetKickNoiseHarmonicBalance);
        if (snareHighLowNoiseBalanceSlider != null) snareHighLowNoiseBalanceSlider.onValueChanged.RemoveListener(SetSnareHighLowNoiseBalance);
        if (melodyOctaveSlider != null) melodyOctaveSlider.onValueChanged.RemoveListener(SetMelodyOctave);
        if (harmonyOctaveSlider != null) harmonyOctaveSlider.onValueChanged.RemoveListener(SetHarmonyOctave);
        if (melodyTremLfoFrequencySlider != null) melodyTremLfoFrequencySlider.onValueChanged.RemoveListener(SetMelodyTremLfoFrequency);
        if (melodyVibLfoFrequencySlider != null) melodyVibLfoFrequencySlider.onValueChanged.RemoveListener(SetMelodyVibLfoFrequency);
        if (harmonyTremLfoFrequencySlider != null) harmonyTremLfoFrequencySlider.onValueChanged.RemoveListener(SetHarmonyTremLfoFrequency);
        if (harmonyVibLfoFrequencySlider != null) harmonyVibLfoFrequencySlider.onValueChanged.RemoveListener(SetHarmonyVibLfoFrequency);
    }

    private void RefreshAllLabels()
    {
        if (oscController == null)
            return;

        UpdateLabel(melodyValueText, oscController.GetGroupVolume(OSCGroupRole.Melody));
        UpdateLabel(harmonyValueText, oscController.GetGroupVolume(OSCGroupRole.Harmony));
        UpdateLabel(bassValueText, oscController.GetGroupVolume(OSCGroupRole.Bass));
        UpdateLabel(kickValueText, oscController.GetGroupVolume(OSCGroupRole.Kick));
        UpdateLabel(snareValueText, oscController.GetGroupVolume(OSCGroupRole.Snare));
        UpdateLabel(hiHatValueText, oscController.GetGroupVolume(OSCGroupRole.HiHat));
        UpdateLabel(melodyPercussiveValueText, oscController.GetInstrumentPercussiveAmount(OSCGroupRole.Melody));
        UpdateLabel(melodySustainValueText, oscController.GetInstrumentSustainAmount(OSCGroupRole.Melody));
        UpdateLabel(bassPercussiveValueText, oscController.GetInstrumentPercussiveAmount(OSCGroupRole.Bass));
        UpdateLabel(bassSustainValueText, oscController.GetInstrumentSustainAmount(OSCGroupRole.Bass));
        UpdateLabel(harmonyPercussiveValueText, oscController.GetInstrumentPercussiveAmount(OSCGroupRole.Harmony));
        UpdateLabel(harmonySustainValueText, oscController.GetInstrumentSustainAmount(OSCGroupRole.Harmony));
        UpdateDetuneLabel(melodyDetuneValueText, oscController.GetGroupDetuneCents(OSCGroupRole.Melody));
        UpdateDetuneLabel(harmonyDetuneValueText, oscController.GetGroupDetuneCents(OSCGroupRole.Harmony));
        UpdateDetuneLabel(bassDetuneValueText, oscController.GetGroupDetuneCents(OSCGroupRole.Bass));
        UpdateLabel(melodyFilterLevelValueText, oscController.GetGroupFilterLevel(OSCGroupRole.Melody));
        UpdateLabel(harmonyFilterLevelValueText, oscController.GetGroupFilterLevel(OSCGroupRole.Harmony));
        UpdateLabel(bassFilterLevelValueText, oscController.GetGroupFilterLevel(OSCGroupRole.Bass));
        UpdateLabel(melodyFMValueText, oscController.GetGroupFMLevel(OSCGroupRole.Melody));
        UpdateLabel(harmonyFMValueText, oscController.GetGroupFMLevel(OSCGroupRole.Harmony));
        UpdateLabel(bassFMValueText, oscController.GetGroupFMLevel(OSCGroupRole.Bass));
        UpdateDistortionLabel(melodyDistortionLevelValueText, oscController.GetGroupDistortionAmount(OSCGroupRole.Melody));
        UpdateDistortionLabel(harmonyDistortionLevelValueText, oscController.GetGroupDistortionAmount(OSCGroupRole.Harmony));
        UpdateDistortionLabel(bassDistortionLevelValueText, oscController.GetGroupDistortionAmount(OSCGroupRole.Bass));
        UpdateDistortionLabel(kickDistortionLevelValueText, oscController.GetDrumDistortionAmount(OSCGroupRole.Kick));
        UpdateDistortionLabel(snareDistortionLevelValueText, oscController.GetDrumDistortionAmount(OSCGroupRole.Snare));
        UpdateDistortionLabel(hiHatDistortionLevelValueText, oscController.GetDrumDistortionAmount(OSCGroupRole.HiHat));
        UpdateBalanceLabel(kickNoiseHarmonicBalanceValueText, oscController.GetKickNoiseHarmonicBalance());
        UpdateBalanceLabel(snareHighLowNoiseBalanceValueText, oscController.GetSnareHighLowNoiseBalance());
        UpdateOctaveLabel(melodyOctaveValueText, oscController.GetGroupPlaybackOctave(OSCGroupRole.Melody));
        UpdateOctaveLabel(harmonyOctaveValueText, oscController.GetGroupPlaybackOctave(OSCGroupRole.Harmony));
        UpdateLfoFrequencyLabel(melodyTremLfoFrequencyValueText, oscController.GetGroupTremLfoFrequency(OSCGroupRole.Melody));
        UpdateLfoFrequencyLabel(melodyVibLfoFrequencyValueText, oscController.GetGroupVibLfoFrequency(OSCGroupRole.Melody));
        UpdateLfoFrequencyLabel(harmonyTremLfoFrequencyValueText, oscController.GetGroupTremLfoFrequency(OSCGroupRole.Harmony));
        UpdateLfoFrequencyLabel(harmonyVibLfoFrequencyValueText, oscController.GetGroupVibLfoFrequency(OSCGroupRole.Harmony));
    }

    private void UpdateLabel(TextMeshProUGUI label, float value)
    {
        if (label == null)
            return;

        if (showPercentLabels)
            label.SetText(Mathf.RoundToInt(value * 100f).ToString() + "%");
        else
            label.SetText(value.ToString("0.00"));
    }

    private void UpdateDetuneLabel(TextMeshProUGUI label, float cents)
    {
        if (label == null)
            return;

        label.SetText(cents.ToString("+0;-0;0") + " c");
    }

    private static void UpdateDistortionLabel(TextMeshProUGUI label, float value)
    {
        if (label == null)
            return;

        label.SetText(value.ToString("0.00"));
    }

    private static void UpdateBalanceLabel(TextMeshProUGUI label, float value)
    {
        if (label == null)
            return;

        label.SetText(Mathf.RoundToInt(Mathf.Clamp01(value) * 100f).ToString() + "%");
    }

    private void UpdateOctaveLabel(TextMeshProUGUI label, int octave)
    {
        if (label == null)
            return;

        label.SetText(octave.ToString());
    }

    private static void UpdateLfoFrequencyLabel(TextMeshProUGUI label, float value)
    {
        if (label == null)
            return;

        label.SetText(value.ToString("0.0") + " Hz");
    }

    private void SetupWaveformDropdowns()
    {
        SetupWaveformDropdown(melodyWaveformDropdown);
        SetupWaveformDropdown(harmonyWaveformDropdown);
        SetupWaveformDropdown(bassWaveformDropdown);
    }

    private void UpdateAllFMControlVisibility()
    {
        UpdateFMControlVisibility(OSCGroupRole.Melody, GetCurrentWaveform(OSCGroupRole.Melody, melodyWaveformDropdown));
        UpdateFMControlVisibility(OSCGroupRole.Harmony, GetCurrentWaveform(OSCGroupRole.Harmony, harmonyWaveformDropdown));
        UpdateFMControlVisibility(OSCGroupRole.Bass, GetCurrentWaveform(OSCGroupRole.Bass, bassWaveformDropdown));
    }

    private void UpdateFMControlVisibility(OSCGroupRole groupRole, Osc.WaveFormType waveform)
    {
        bool active = waveform == Osc.WaveFormType.FM;

        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                SetFMControlActive(melodyFMControlRoot, melodyFMSlider, melodyFMValueText, active);
                break;

            case OSCGroupRole.Harmony:
                SetFMControlActive(harmonyFMControlRoot, harmonyFMSlider, harmonyFMValueText, active);
                break;

            case OSCGroupRole.Bass:
                SetFMControlActive(bassFMControlRoot, bassFMSlider, bassFMValueText, active);
                break;
        }
    }

    private Osc.WaveFormType GetCurrentWaveform(OSCGroupRole groupRole, TMP_Dropdown dropdown)
    {
        if (oscController != null)
            return oscController.GetGroupWaveform(groupRole);

        if (dropdown != null)
            return GetWaveformFromIndex(dropdown.value);

        return Osc.WaveFormType.Sine;
    }

    private static Osc.WaveFormType GetWaveformFromIndex(int value)
    {
        Osc.WaveFormType[] values = (Osc.WaveFormType[])System.Enum.GetValues(typeof(Osc.WaveFormType));
        int index = Mathf.Clamp(value, 0, values.Length - 1);
        return values[index];
    }

    private static void SetFMControlActive(GameObject root, Slider slider, TextMeshProUGUI label, bool active)
    {
        if (root != null)
        {
            root.SetActive(active);
            return;
        }

        if (slider != null)
            slider.gameObject.SetActive(active);

        if (label != null)
            label.gameObject.SetActive(active);
    }

    private static void SetupWaveformDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
            return;

        string[] names = System.Enum.GetNames(typeof(Osc.WaveFormType));
        if (dropdown.options.Count == names.Length)
            return;

        List<string> options = new List<string>();
        for (int i = 0; i < names.Length; i++)
            options.Add(names[i]);

        dropdown.ClearOptions();
        dropdown.AddOptions(options);
    }

    private static void SetDropdownWithoutNotify(TMP_Dropdown dropdown, int value)
    {
        if (dropdown == null)
            return;

        int maxValue = Mathf.Max(0, dropdown.options.Count - 1);
        dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, maxValue));
    }

    private static void SetToggleWithoutNotify(Toggle toggle, bool value)
    {
        if (toggle == null)
            return;

        toggle.SetIsOnWithoutNotify(value);
    }

    private void SetDetuneSliderWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
            return;

        float range = Mathf.Abs(detuneSliderRangeCents);
        slider.minValue = -range;
        slider.maxValue = range;
        slider.SetValueWithoutNotify(Mathf.Clamp(value, -range, range));
    }

    private void SetDistortionSliderWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
            return;

        float max = GetDistortionSliderMax();
        slider.minValue = 0f;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(Mathf.Clamp(value, 0f, max));
    }

    private void SetDrumDistortionSliderWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
            return;

        float max = GetDrumDistortionSliderMax();
        slider.minValue = 0f;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(Mathf.Clamp(value, 0f, max));
    }

    private void SetLfoSliderWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
            return;

        float max = GetLfoFrequencySliderMax();
        slider.minValue = 0f;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(Mathf.Clamp(value, 0f, max));
    }

    private void ConfigureEffectSliderRanges()
    {
        ConfigureNormalizedSliderRange(melodyFMSlider);
        ConfigureNormalizedSliderRange(harmonyFMSlider);
        ConfigureNormalizedSliderRange(bassFMSlider);
        ConfigureLfoSliderRange(melodyTremLfoFrequencySlider);
        ConfigureLfoSliderRange(melodyVibLfoFrequencySlider);
        ConfigureLfoSliderRange(harmonyTremLfoFrequencySlider);
        ConfigureLfoSliderRange(harmonyVibLfoFrequencySlider);
        ConfigureDistortionSliderRange(melodyDistortionLevelSlider);
        ConfigureDistortionSliderRange(harmonyDistortionLevelSlider);
        ConfigureDistortionSliderRange(bassDistortionLevelSlider);
        ConfigureDrumDistortionSliderRange(kickDistortionLevelSlider);
        ConfigureDrumDistortionSliderRange(snareDistortionLevelSlider);
        ConfigureDrumDistortionSliderRange(hiHatDistortionLevelSlider);
        ConfigureNormalizedSliderRange(kickNoiseHarmonicBalanceSlider);
        ConfigureNormalizedSliderRange(snareHighLowNoiseBalanceSlider);
    }

    private static void ConfigureNormalizedSliderRange(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
    }

    private void ConfigureLfoSliderRange(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = GetLfoFrequencySliderMax();
    }

    private void ConfigureDistortionSliderRange(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = GetDistortionSliderMax();
    }

    private void ConfigureDrumDistortionSliderRange(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = GetDrumDistortionSliderMax();
    }

    private float GetDistortionSliderMax()
    {
        if (oscController != null)
            return Mathf.Max(0f, oscController.distortionEffectMax);

        return Mathf.Max(0f, distortionSliderMax);
    }

    private float GetDrumDistortionSliderMax()
    {
        if (oscController != null)
            return Mathf.Max(0f, oscController.drumDistortionEffectMax);

        return Mathf.Max(0f, drumDistortionSliderMax);
    }

    private float GetLfoFrequencySliderMax()
    {
        if (oscController != null)
            return Mathf.Max(0f, oscController.lfoFrequencySliderMax);

        return Mathf.Max(0f, lfoFrequencySliderMax);
    }

    private static void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }

    private static void SetOctaveSliderWithoutNotify(Slider slider, int value)
    {
        if (slider == null)
            return;

        slider.minValue = 3f;
        slider.maxValue = 8f;
        slider.wholeNumbers = true;
        slider.SetValueWithoutNotify(Mathf.Clamp(value, 3, 8));
    }
}
