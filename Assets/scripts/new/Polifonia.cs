using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Osc; // Permite acceder directamente a los elementos estáticos o públicos de la clase Osc

// Esta clase maneja la polifonía en un instrumento virtual, permitiendo instanciar múltiples Osc
// cada uno reproduciendo una nota distinta y administrar de manera centralizada los parámetros
// octava, forma de onda, envolvente ADSR, sampling, FM, wavetable, etc.
public class Polifonia : MonoBehaviour
{
    // Prefab que contiene el componente Osc.
    public GameObject OSCprefab;

    // Diccionario que relaciona el nombre de la nota con la instancia de Osc que la está reproduciendo.
    private Dictionary<string, Osc> activeOscillators = new Dictionary<string, Osc>();

    // Referencias a elementos generales de la interfaz.
    public TextMeshProUGUI OctaveText, WaveFormText, ArmonicosText, TremLFOText, VibLFOText;
    public Slider OctaveSl, WaveformSl, ArmonicosSl, TremLFOFSl, VibFOFSl;

    public Slider AttackSlider;
    public Slider DecaySlider;
    public Slider SustainSlider;
    public Slider SustainLevelSlider;
    public Slider ReleaseSlider;
    public Slider VolumeSlider;
    public Slider DetunedSlider;
    public Slider VibratoDepthSlider;
    public Slider FMModFrequencySlider;
    public Slider FMModIndexSlider;

    public TextMeshProUGUI AttackText;
    public TextMeshProUGUI DecayText;
    public TextMeshProUGUI SustainText;
    public TextMeshProUGUI SustainLevelText;
    public TextMeshProUGUI ReleaseText;
    public TextMeshProUGUI VolumeText;
    public TextMeshProUGUI DetunedText;
    public TextMeshProUGUI VibratoDepthText;
    public TextMeshProUGUI FMModFrequencyText;
    public TextMeshProUGUI FMModIndexText;

    [Header("Sampling")]
    [Tooltip("AudioClip que será usado como fuente para la forma de onda Sampling1.")]
    public AudioClip SamplingClip;

    [Tooltip("Frecuencia original de la nota grabada en el AudioClip. Por ejemplo: 440 Hz si el sample corresponde a La4.")]
    public Slider SamplingBaseFrequencySlider;

    [Tooltip("Punto normalizado de inicio del sample. Rango recomendado: 0 a 1.")]
    public Slider SamplingStartSlider;

    [Tooltip("Punto normalizado de finalización del sample. Rango recomendado: 0 a 1.")]
    public Slider SamplingEndSlider;

    public TextMeshProUGUI SamplingBaseFrequencyText;
    public TextMeshProUGUI SamplingStartText;
    public TextMeshProUGUI SamplingEndText;

    [Header("ADSR desde AudioClip")]
    [Tooltip("AudioClip que se usará para construir una envolvente positiva normalizada entre 0 y 1.")]
    public AudioClip ADSRClip;

    [Tooltip("Toggle que permite usar el AudioClip como envolvente ADSR en lugar del ADSR procedural.")]
    public Toggle UseADSRClipToggle;
    public Toggle AttackLogCurveToggle;
    public Toggle DecayLogCurveToggle;
    public Toggle SustainLogCurveToggle;
    public Toggle ReleaseLogCurveToggle;

    [Tooltip("Valor usado cuando no hay Toggle asignado en la UI.")]
    public bool UseADSRClipValue = false;
    public bool AttackLogCurveValue = false;
    public bool DecayLogCurveValue = false;
    public bool SustainLogCurveValue = false;
    public bool ReleaseLogCurveValue = false;

    public Slider[] amplitudes = new Slider[10]; // Sliders para controlar las amplitudes de los armónicos.
    public TextMeshProUGUI[] HarmonicLevelTexts = new TextMeshProUGUI[10];
    public float[] AmplitudesLv = new float[10];

    public Toggle WavetableToggle;

    [Header("Valores por código cuando no hay UI")]
    public int OctaveValue = 4;
    public int WaveformValue = 0;
    public int ArmonicosValue = 1;

    public int AttackValue = 5;
    public int DecayValue = 5;
    public int SustainValue = 5;
    public int ReleaseValue = 5;

    public float SustainLevelValue = 0.7f;
    public float VolumeValue = 0.5f;
    public float DetuneCentsValue = 0f;

    public float TremLFOValue = 0f;
    public float VibLFOValue = 0f;
    public float VibratoDepthValue = 5f;

    public float FMModFrequencyValue = 220f;
    public float FMModIndexValue = 1f;

    [Header("Valores de Sampling cuando no hay UI")]
    [Tooltip("Frecuencia base del sample. Debe coincidir con la nota original grabada.")]
    public float SamplingBaseFrequencyValue = 440f;

    [Range(0f, 1f)]
    public float SamplingStartValue = 0f;

    [Range(0f, 1f)]
    public float SamplingEndValue = 1f;

    [Tooltip("Frame calculado a partir de SamplingStartValue.")]
    public int SamplingStartFrameValue = 0;

    [Tooltip("Frame calculado a partir de SamplingEndValue.")]
    public int SamplingEndFrameValue = 0;

    public bool WavetableValue = false;

    void Start()
    {
        UpgradeLegacyDefaultPresetIfNeeded();

        // ADSR procedural
        if (AttackSlider != null)
            AttackSlider.onValueChanged.AddListener(delegate { UpdateAttack(); });

        if (DecaySlider != null)
            DecaySlider.onValueChanged.AddListener(delegate { UpdateDecay(); });

        if (SustainSlider != null)
            SustainSlider.onValueChanged.AddListener(delegate { UpdateSustain(); });

        if (SustainLevelSlider != null)
            SustainLevelSlider.onValueChanged.AddListener(delegate { UpdateSustainLevel(); });

        if (ReleaseSlider != null)
            ReleaseSlider.onValueChanged.AddListener(delegate { UpdateRelease(); });

        if (VolumeSlider != null)
            VolumeSlider.onValueChanged.AddListener(delegate { UpdateVolume(); });

        // Wavetable
        if (WavetableToggle != null)
            WavetableToggle.onValueChanged.AddListener(delegate { ToggleWavetable(); });

        // Parámetros generales
        if (OctaveSl != null)
            OctaveSl.onValueChanged.AddListener(delegate { OctaveChange(); });

        if (WaveformSl != null)
            WaveformSl.onValueChanged.AddListener(delegate { WaveFormChange(); });

        if (ArmonicosSl != null)
            ArmonicosSl.onValueChanged.AddListener(delegate { ArmonicosChange(); });

        if (amplitudes != null)
        {
            for (int i = 0; i < amplitudes.Length; i++)
            {
                if (amplitudes[i] != null)
                    amplitudes[i].onValueChanged.AddListener(delegate { AmplitudesChange(); });
            }
        }

        if (DetunedSlider != null)
            DetunedSlider.onValueChanged.AddListener(delegate { DetunedChange(); });

        if (TremLFOFSl != null)
            TremLFOFSl.onValueChanged.AddListener(delegate { TremFChange(); });

        if (VibFOFSl != null)
            VibFOFSl.onValueChanged.AddListener(delegate { VibFChange(); });

        if (VibratoDepthSlider != null)
            VibratoDepthSlider.onValueChanged.AddListener(delegate { VibratoDepthChange(); });

        // FM
        if (FMModFrequencySlider != null)
            FMModFrequencySlider.onValueChanged.AddListener(delegate { FMModFrequencyChange(); });

        if (FMModIndexSlider != null)
            FMModIndexSlider.onValueChanged.AddListener(delegate { FMModIndexChange(); });

        // Sampling
        if (SamplingBaseFrequencySlider != null)
            SamplingBaseFrequencySlider.onValueChanged.AddListener(delegate { SamplingBaseFrequencyChange(); });

        if (SamplingStartSlider != null)
            SamplingStartSlider.onValueChanged.AddListener(delegate { SamplingStartChange(); });

        if (SamplingEndSlider != null)
            SamplingEndSlider.onValueChanged.AddListener(delegate { SamplingEndChange(); });

        // ADSR desde AudioClip
        if (UseADSRClipToggle != null)
            UseADSRClipToggle.onValueChanged.AddListener(delegate { ADSRClipToggleChange(); });

        if (AttackLogCurveToggle != null)
            AttackLogCurveToggle.onValueChanged.AddListener(delegate { AttackLogCurveChange(); });

        if (DecayLogCurveToggle != null)
            DecayLogCurveToggle.onValueChanged.AddListener(delegate { DecayLogCurveChange(); });

        if (SustainLogCurveToggle != null)
            SustainLogCurveToggle.onValueChanged.AddListener(delegate { SustainLogCurveChange(); });

        if (ReleaseLogCurveToggle != null)
            ReleaseLogCurveToggle.onValueChanged.AddListener(delegate { ReleaseLogCurveChange(); });

        // Configuración del slider de forma de onda.
        if (WaveformSl != null)
        {
            WaveformSl.wholeNumbers = true;
            WaveformSl.minValue = 0;
            WaveformSl.maxValue = System.Enum.GetValues(typeof(WaveFormType)).Length - 1;
        }

        // Configuración de sliders FM.
        if (FMModFrequencySlider != null)
        {
            FMModFrequencySlider.wholeNumbers = false;
            FMModFrequencySlider.minValue = 0f;
            FMModFrequencySlider.maxValue = 1000f;
            FMModFrequencySlider.value = FMModFrequencyValue;
        }

        if (FMModIndexSlider != null)
        {
            FMModIndexSlider.wholeNumbers = false;
            FMModIndexSlider.minValue = 0f;
            FMModIndexSlider.maxValue = 8f;
            FMModIndexSlider.value = FMModIndexValue;
        }

        // Configuración de sliders Sampling.
        if (SamplingBaseFrequencySlider != null)
        {
            SamplingBaseFrequencySlider.wholeNumbers = false;
            SamplingBaseFrequencySlider.minValue = 20f;
            SamplingBaseFrequencySlider.maxValue = 2000f;
            SamplingBaseFrequencySlider.value = SamplingBaseFrequencyValue;
        }

        if (SamplingStartSlider != null)
        {
            SamplingStartSlider.wholeNumbers = false;
            SamplingStartSlider.minValue = 0f;
            SamplingStartSlider.maxValue = 1f;
            SamplingStartSlider.value = SamplingStartValue;
        }

        if (SamplingEndSlider != null)
        {
            SamplingEndSlider.wholeNumbers = false;
            SamplingEndSlider.minValue = 0f;
            SamplingEndSlider.maxValue = 1f;
            SamplingEndSlider.value = SamplingEndValue;
        }

        if (UseADSRClipToggle != null)
        {
            UseADSRClipToggle.isOn = UseADSRClipValue;
        }

        ApplyStoredValuesToUIWithoutNotify();

        // Inicialización de valores.
        UpdateVolume();
        OctaveChange();
        WaveFormChange();
        ArmonicosChange();
        AmplitudesChange();
        UpdateAttack();
        UpdateDecay();
        UpdateSustain();
        UpdateSustainLevel();
        UpdateRelease();
        DetunedChange();
        TremFChange();
        VibFChange();
        VibratoDepthChange();
        FMModFrequencyChange();
        FMModIndexChange();

        UpdateSamplingSettingsFromUI();
        SamplingBaseFrequencyChange();
        SamplingStartChange();
        SamplingEndChange();
        ADSRClipToggleChange();
        AttackLogCurveChange();
        DecayLogCurveChange();
        SustainLogCurveChange();
        ReleaseLogCurveChange();
    }

    private void UpgradeLegacyDefaultPresetIfNeeded()
    {
        if (!HasLegacyDefaultPreset())
            return;

        OctaveValue = 4;
        WaveformValue = 3;
        ArmonicosValue = 1;

        AttackValue = 10;
        DecayValue = 379;
        SustainValue = 4982;
        ReleaseValue = 499;

        SustainLevelValue = 0.69f;
        DetuneCentsValue = 1f;
        TremLFOValue = 0f;
        VibLFOValue = 0f;
        VibratoDepthValue = 0f;

        FMModFrequencyValue = 1.01f;
        FMModIndexValue = 1f;

        SamplingBaseFrequencyValue = 440f;
        SamplingStartValue = 0f;
        SamplingEndValue = 1f;
        WavetableValue = false;

        if (AmplitudesLv == null || AmplitudesLv.Length != 10)
            AmplitudesLv = new float[10];

        for (int i = 0; i < AmplitudesLv.Length; i++)
            AmplitudesLv[i] = 1f;
    }

    private bool HasLegacyDefaultPreset()
    {
        bool hasLegacyScalarValues =
            OctaveValue == 4 &&
            WaveformValue == 0 &&
            ArmonicosValue == 1 &&
            AttackValue == 5 &&
            DecayValue == 5 &&
            SustainValue == 5 &&
            ReleaseValue == 5 &&
            Mathf.Approximately(SustainLevelValue, 0.7f) &&
            Mathf.Approximately(VolumeValue, 0.5f) &&
            Mathf.Approximately(DetuneCentsValue, 0f) &&
            Mathf.Approximately(TremLFOValue, 0f) &&
            Mathf.Approximately(VibLFOValue, 0f) &&
            Mathf.Approximately(VibratoDepthValue, 5f) &&
            Mathf.Approximately(FMModFrequencyValue, 220f) &&
            Mathf.Approximately(FMModIndexValue, 1f) &&
            Mathf.Approximately(SamplingBaseFrequencyValue, 440f) &&
            Mathf.Approximately(SamplingStartValue, 0f) &&
            Mathf.Approximately(SamplingEndValue, 1f) &&
            !WavetableValue;

        if (!hasLegacyScalarValues)
            return false;

        if (AmplitudesLv == null || AmplitudesLv.Length == 0)
            return true;

        for (int i = 0; i < AmplitudesLv.Length; i++)
        {
            if (!Mathf.Approximately(AmplitudesLv[i], 0f))
                return false;
        }

        return true;
    }

    private void ApplyStoredValuesToUIWithoutNotify()
    {
        if (OctaveSl != null)
            OctaveSl.SetValueWithoutNotify(OctaveValue);

        if (WaveformSl != null)
            WaveformSl.SetValueWithoutNotify(WaveformValue);

        if (ArmonicosSl != null)
            ArmonicosSl.SetValueWithoutNotify(ArmonicosValue);

        if (AttackSlider != null)
            AttackSlider.SetValueWithoutNotify(AttackValue);

        if (DecaySlider != null)
            DecaySlider.SetValueWithoutNotify(DecayValue);

        if (SustainSlider != null)
            SustainSlider.SetValueWithoutNotify(SustainValue);

        if (ReleaseSlider != null)
            ReleaseSlider.SetValueWithoutNotify(ReleaseValue);

        if (SustainLevelSlider != null)
            SustainLevelSlider.SetValueWithoutNotify(SustainLevelValue);

        if (VolumeSlider != null)
            VolumeSlider.SetValueWithoutNotify(VolumeValue);

        if (DetunedSlider != null)
            DetunedSlider.SetValueWithoutNotify(DetuneCentsValue);

        if (TremLFOFSl != null)
            TremLFOFSl.SetValueWithoutNotify(TremLFOValue);

        if (VibFOFSl != null)
            VibFOFSl.SetValueWithoutNotify(VibLFOValue);

        if (VibratoDepthSlider != null)
            VibratoDepthSlider.SetValueWithoutNotify(VibratoDepthValue);

        if (FMModFrequencySlider != null)
            FMModFrequencySlider.SetValueWithoutNotify(FMModFrequencyValue);

        if (FMModIndexSlider != null)
            FMModIndexSlider.SetValueWithoutNotify(FMModIndexValue);

        if (SamplingBaseFrequencySlider != null)
            SamplingBaseFrequencySlider.SetValueWithoutNotify(SamplingBaseFrequencyValue);

        if (SamplingStartSlider != null)
            SamplingStartSlider.SetValueWithoutNotify(SamplingStartValue);

        if (SamplingEndSlider != null)
            SamplingEndSlider.SetValueWithoutNotify(SamplingEndValue);

        if (WavetableToggle != null)
            WavetableToggle.SetIsOnWithoutNotify(WavetableValue);

        if (UseADSRClipToggle != null)
            UseADSRClipToggle.SetIsOnWithoutNotify(UseADSRClipValue);

        if (AttackLogCurveToggle != null)
            AttackLogCurveToggle.SetIsOnWithoutNotify(AttackLogCurveValue);

        if (DecayLogCurveToggle != null)
            DecayLogCurveToggle.SetIsOnWithoutNotify(DecayLogCurveValue);

        if (SustainLogCurveToggle != null)
            SustainLogCurveToggle.SetIsOnWithoutNotify(SustainLogCurveValue);

        if (ReleaseLogCurveToggle != null)
            ReleaseLogCurveToggle.SetIsOnWithoutNotify(ReleaseLogCurveValue);

        if (amplitudes == null || AmplitudesLv == null)
            return;

        int count = Mathf.Min(amplitudes.Length, AmplitudesLv.Length);
        for (int i = 0; i < count; i++)
        {
            if (amplitudes[i] != null)
                amplitudes[i].SetValueWithoutNotify(AmplitudesLv[i]);
        }
    }

    void Update()
    {
    }

    // ------------------------------------------------------------
    // VOLUMEN
    // ------------------------------------------------------------

    public void UpdateVolume()
    {
        float value = VolumeSlider != null ? VolumeSlider.value : VolumeValue;
        VolumeValue = value;

        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateVolume(value);
        }

        if (VolumeText != null)
            VolumeText.text = value.ToString("F2");
    }

    // ------------------------------------------------------------
    // OCTAVA
    // ------------------------------------------------------------

    public void OctaveChange()
    {
        int value = OctaveSl != null ? (int)OctaveSl.value : OctaveValue;
        OctaveValue = value;

        foreach (var osc in activeOscillators.Values)
        {
            osc.OctaveChange(value);
        }

        if (OctaveText != null)
            OctaveText.SetText(value.ToString());
    }

    // ------------------------------------------------------------
    // DETUNE
    // ------------------------------------------------------------

    public void DetunedChange()
    {
        float detunedValue = DetunedSlider != null ? DetunedSlider.value : DetuneCentsValue;
        DetuneCentsValue = detunedValue;

        foreach (var osc in activeOscillators.Values)
        {
            osc.detuneCents = detunedValue;
            osc.MarkExternalBackendDirty();
        }

        if (DetunedText != null)
            DetunedText.text = detunedValue.ToString("F2");
    }

    // ------------------------------------------------------------
    // TREMOLO Y VIBRATO
    // ------------------------------------------------------------

    public void TremFChange()
    {
        float tremValue = TremLFOFSl != null ? TremLFOFSl.value : TremLFOValue;
        TremLFOValue = tremValue;

        foreach (var osc in activeOscillators.Values)
        {
            osc.tremLFOf = tremValue;
            osc.MarkExternalBackendDirty();
        }

        if (TremLFOText != null)
            TremLFOText.text = tremValue.ToString("F2");
    }

    public void VibFChange()
    {
        float vibValue = VibFOFSl != null ? VibFOFSl.value : VibLFOValue;
        VibLFOValue = vibValue;

        foreach (var osc in activeOscillators.Values)
        {
            osc.VibLFOf = vibValue;
            osc.MarkExternalBackendDirty();
        }

        if (VibLFOText != null)
            VibLFOText.text = vibValue.ToString("F2");
    }

    public void VibratoDepthChange()
    {
        float vibratoDepthValue = VibratoDepthSlider != null ? VibratoDepthSlider.value : VibratoDepthValue;
        VibratoDepthValue = vibratoDepthValue;

        foreach (var osc in activeOscillators.Values)
        {
            osc.vibratoDepth = vibratoDepthValue;
            osc.MarkExternalBackendDirty();
        }

        if (VibratoDepthText != null)
            VibratoDepthText.text = vibratoDepthValue.ToString("F2");
    }

    // ------------------------------------------------------------
    // FM
    // ------------------------------------------------------------

    public void FMModFrequencyChange()
    {
        float value = FMModFrequencySlider != null ? FMModFrequencySlider.value : FMModFrequencyValue;
        FMModFrequencyValue = value;

        foreach (var osc in activeOscillators.Values)
        {
            osc.fmModFrequency = value;
            osc.MarkExternalBackendDirty();
        }

        if (FMModFrequencyText != null)
            FMModFrequencyText.text = value.ToString("F2");
    }

    public void FMModIndexChange()
    {
        float value = FMModIndexSlider != null ? FMModIndexSlider.value : FMModIndexValue;
        FMModIndexValue = value;

        foreach (var osc in activeOscillators.Values)
        {
            osc.fmModIndex = value;
            osc.MarkExternalBackendDirty();
        }

        if (FMModIndexText != null)
            FMModIndexText.text = value.ToString("F2");
    }

    // ------------------------------------------------------------
    // SAMPLING
    // ------------------------------------------------------------

    public void SamplingBaseFrequencyChange()
    {
        UpdateSamplingSettingsFromUI();
        ApplySamplingSettingsToActiveOscillators();

        if (SamplingBaseFrequencyText != null)
            SamplingBaseFrequencyText.text = SamplingBaseFrequencyValue.ToString("F2") + " Hz";
    }

    public void SamplingStartChange()
    {
        UpdateSamplingSettingsFromUI();
        ApplySamplingSettingsToActiveOscillators();

        if (SamplingStartText != null)
        {
            float seconds = SamplingClip != null ? SamplingStartValue * SamplingClip.length : 0f;

            SamplingStartText.text =
                SamplingStartValue.ToString("F2") +
                " / " +
                seconds.ToString("F2") +
                " s / frame " +
                SamplingStartFrameValue.ToString();
        }
    }

    public void SamplingEndChange()
    {
        UpdateSamplingSettingsFromUI();
        ApplySamplingSettingsToActiveOscillators();

        if (SamplingEndText != null)
        {
            float seconds = SamplingClip != null ? SamplingEndValue * SamplingClip.length : 0f;

            SamplingEndText.text =
                SamplingEndValue.ToString("F2") +
                " / " +
                seconds.ToString("F2") +
                " s / frame " +
                SamplingEndFrameValue.ToString();
        }
    }

    private void UpdateSamplingSettingsFromUI()
    {
        if (SamplingBaseFrequencySlider != null)
            SamplingBaseFrequencyValue = Mathf.Max(1f, SamplingBaseFrequencySlider.value);
        else
            SamplingBaseFrequencyValue = Mathf.Max(1f, SamplingBaseFrequencyValue);

        if (SamplingStartSlider != null)
            SamplingStartValue = Mathf.Clamp01(SamplingStartSlider.value);
        else
            SamplingStartValue = Mathf.Clamp01(SamplingStartValue);

        if (SamplingEndSlider != null)
            SamplingEndValue = Mathf.Clamp01(SamplingEndSlider.value);
        else
            SamplingEndValue = Mathf.Clamp01(SamplingEndValue);

        if (SamplingEndValue <= SamplingStartValue)
        {
            SamplingEndValue = Mathf.Clamp01(SamplingStartValue + 0.01f);

            if (SamplingEndSlider != null)
                SamplingEndSlider.SetValueWithoutNotify(SamplingEndValue);
        }

        UpdateSamplingFrames();
    }

    private void UpdateSamplingFrames()
    {
        if (SamplingClip == null || SamplingClip.samples <= 0)
        {
            SamplingStartFrameValue = 0;
            SamplingEndFrameValue = 0;
            return;
        }

        int lastFrame = Mathf.Max(0, SamplingClip.samples - 1);

        SamplingStartFrameValue = Mathf.Clamp(
            Mathf.RoundToInt(SamplingStartValue * lastFrame),
            0,
            lastFrame
        );

        SamplingEndFrameValue = Mathf.Clamp(
            Mathf.RoundToInt(SamplingEndValue * lastFrame),
            0,
            lastFrame
        );

        // Asegura que siempre exista al menos un frame de diferencia.
        if (SamplingEndFrameValue <= SamplingStartFrameValue)
        {
            SamplingEndFrameValue = Mathf.Min(lastFrame, SamplingStartFrameValue + 1);
        }
    }

    private void ApplySamplingSettingsToActiveOscillators()
    {
        foreach (var osc in activeOscillators.Values)
        {
            ApplySamplingSettingsToOscillator(osc);
        }
    }

    private void ApplySamplingSettingsToOscillator(Osc osc)
    {
        if (osc == null)
            return;

        osc.samplingClip = SamplingClip;
        osc.samplingBaseFrequency = SamplingBaseFrequencyValue;
        osc.samplingStartNormalized = SamplingStartValue;
        osc.samplingEndNormalized = SamplingEndValue;
        osc.samplingStartFrame = SamplingStartFrameValue;
        osc.samplingEndFrame = SamplingEndFrameValue;
        osc.MarkExternalBackendDirty();
    }

    // ------------------------------------------------------------
    // ADSR DESDE AUDIOCLIP
    // ------------------------------------------------------------

    public void ADSRClipToggleChange()
    {
        UseADSRClipValue = UseADSRClipToggle != null ? UseADSRClipToggle.isOn : UseADSRClipValue;
        ApplyADSRClipSettingsToActiveOscillators();
    }

    public void AttackLogCurveChange()
    {
        AttackLogCurveValue = AttackLogCurveToggle != null ? AttackLogCurveToggle.isOn : AttackLogCurveValue;
        ApplyAdsrCurveModesToActiveOscillators();
    }

    public void DecayLogCurveChange()
    {
        DecayLogCurveValue = DecayLogCurveToggle != null ? DecayLogCurveToggle.isOn : DecayLogCurveValue;
        ApplyAdsrCurveModesToActiveOscillators();
    }

    public void SustainLogCurveChange()
    {
        SustainLogCurveValue = SustainLogCurveToggle != null ? SustainLogCurveToggle.isOn : SustainLogCurveValue;
        ApplyAdsrCurveModesToActiveOscillators();
    }

    public void ReleaseLogCurveChange()
    {
        ReleaseLogCurveValue = ReleaseLogCurveToggle != null ? ReleaseLogCurveToggle.isOn : ReleaseLogCurveValue;
        ApplyAdsrCurveModesToActiveOscillators();
    }

    private void ApplyADSRClipSettingsToActiveOscillators()
    {
        foreach (var osc in activeOscillators.Values)
        {
            ApplyADSRClipSettingsToOscillator(osc);
        }
    }

    private void ApplyADSRClipSettingsToOscillator(Osc osc)
    {
        if (osc == null)
            return;

        osc.adsrSourceClip = ADSRClip;
        osc.useAudioClipADSR = UseADSRClipValue;
        osc.BuildAudioClipADSRData();
        osc.MarkExternalBackendDirty();
    }

    private void ApplyAdsrCurveModesToActiveOscillators()
    {
        foreach (var osc in activeOscillators.Values)
        {
            ApplyAdsrCurveModesToOscillator(osc);
        }
    }

    private void ApplyAdsrCurveModesToOscillator(Osc osc)
    {
        if (osc == null)
            return;

        osc.attackUsesLogCurve = AttackLogCurveValue;
        osc.decayUsesLogCurve = DecayLogCurveValue;
        osc.sustainUsesLogCurve = SustainLogCurveValue;
        osc.releaseUsesLogCurve = ReleaseLogCurveValue;
        osc.UpdateADSR();
    }

    // ------------------------------------------------------------
    // FORMA DE ONDA
    // ------------------------------------------------------------

    public void WaveFormChange()
    {
        int waveformValue = WaveformSl != null ? (int)WaveformSl.value : WaveformValue;

        int maxWaveformIndex = System.Enum.GetValues(typeof(WaveFormType)).Length - 1;
        waveformValue = Mathf.Clamp(waveformValue, 0, maxWaveformIndex);

        WaveformValue = waveformValue;

        WaveFormType selectedType = (WaveFormType)waveformValue;

        foreach (var osc in activeOscillators.Values)
        {
            osc.WaveFormChange(selectedType);
        }

        if (WaveFormText != null)
        {
            switch (selectedType)
            {
                case WaveFormType.Sine:
                    WaveFormText.SetText("Seno");
                    break;

                case WaveFormType.Square:
                    WaveFormText.SetText("Square");
                    break;

                case WaveFormType.Sawtooth:
                    WaveFormText.SetText("Sawtooth");
                    break;

                case WaveFormType.Triangle:
                    WaveFormText.SetText("Triangle");
                    break;

                case WaveFormType.SA:
                    WaveFormText.SetText("SA");
                    break;

                case WaveFormType.FM:
                    WaveFormText.SetText("FM");
                    break;

                case WaveFormType.Sampling1:
                    WaveFormText.SetText("Sampling 1");
                    break;

                case WaveFormType.WhiteNoise:
                    WaveFormText.SetText("Noise");
                    break;

                case WaveFormType.Custom1:
                    WaveFormText.SetText("Custom1");
                    break;

                case WaveFormType.Custom2:
                    WaveFormText.SetText("Custom2");
                    break;

                case WaveFormType.Custom3:
                    WaveFormText.SetText("Custom3");
                    break;

                case WaveFormType.Gemini:
                    WaveFormText.SetText("Gemini");
                    break;

                default:
                    WaveFormText.SetText(selectedType.ToString());
                    break;
            }
        }
    }

    // ------------------------------------------------------------
    // SÍNTESIS ADITIVA
    // ------------------------------------------------------------

    public void ArmonicosChange()
    {
        int value = ArmonicosSl != null ? (int)ArmonicosSl.value : ArmonicosValue;
        ArmonicosValue = value;

        foreach (var osc in activeOscillators.Values)
        {
            osc.ArmonicosChange(value);
        }

        if (ArmonicosText != null)
            ArmonicosText.SetText(value.ToString());
    }

    public void AmplitudesChange()
    {
        for (int i = 0; i < AmplitudesLv.Length; i++)
        {
            float value = AmplitudesLv[i];

            if (amplitudes != null && i < amplitudes.Length && amplitudes[i] != null)
            {
                value = amplitudes[i].value;
                AmplitudesLv[i] = value;
            }

            foreach (var osc in activeOscillators.Values)
            {
                osc.AmplitudesChange(i, value);
            }

            if (HarmonicLevelTexts != null && i < HarmonicLevelTexts.Length && HarmonicLevelTexts[i] != null)
                HarmonicLevelTexts[i].text = value.ToString("F2");
        }
    }

    // ------------------------------------------------------------
    // WAVETABLE
    // ------------------------------------------------------------

    public void ToggleWavetable()
    {
        bool value = WavetableToggle != null ? WavetableToggle.isOn : WavetableValue;
        WavetableValue = value;

        foreach (var osc in activeOscillators.Values)
        {
            osc.ToggleWavetable(value);
        }
    }

    // ------------------------------------------------------------
    // ADSR PROCEDURAL
    // ------------------------------------------------------------

    public void UpdateAttack()
    {
        int value = AttackSlider != null ? (int)AttackSlider.value : AttackValue;
        AttackValue = value;

        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateAttack(value);
        }

        if (AttackText != null)
            AttackText.text = value.ToString("F2");
    }

    public void UpdateDecay()
    {
        int value = DecaySlider != null ? (int)DecaySlider.value : DecayValue;
        DecayValue = value;

        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateDecay(value);
        }

        if (DecayText != null)
            DecayText.text = value.ToString("F2");
    }

    public void UpdateSustain()
    {
        int value = SustainSlider != null ? (int)SustainSlider.value : SustainValue;
        SustainValue = value;

        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateSustain(value);
        }

        if (SustainText != null)
            SustainText.text = value.ToString();
    }

    public void UpdateSustainLevel()
    {
        float value = SustainLevelSlider != null ? SustainLevelSlider.value : SustainLevelValue;
        SustainLevelValue = value;

        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateSustainLevel(value);
        }

        if (SustainLevelText != null)
            SustainLevelText.text = value.ToString("F2");
    }

    public void UpdateRelease()
    {
        int value = ReleaseSlider != null ? (int)ReleaseSlider.value : ReleaseValue;
        ReleaseValue = value;

        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateRelease(value);
        }

        if (ReleaseText != null)
            ReleaseText.text = value.ToString("F2");
    }

    // ------------------------------------------------------------
    // NOTAS
    // ------------------------------------------------------------

    public void NoteOn(string note)
    {
        if (!activeOscillators.ContainsKey(note))
        {
            GameObject oscInstance = Instantiate(OSCprefab, transform);
            oscInstance.name = note;

            Osc oscScript = oscInstance.GetComponent<Osc>();

            if (oscScript != null)
            {
                activeOscillators[note] = oscScript;

                UpdateSamplingSettingsFromUI();
                ApplySettingsToOsc(oscScript);
                ApplySamplingSettingsToOscillator(oscScript);
                ApplyADSRClipSettingsToOscillator(oscScript);
                ApplyAdsrCurveModesToOscillator(oscScript);

                oscScript.KeyboardDown(note);
            }
        }
    }

    public void NoteOff(string note)
    {
        if (activeOscillators.ContainsKey(note))
        {
            activeOscillators[note].KeyboardUp();

            Destroy(activeOscillators[note].gameObject, 0.1f);

            activeOscillators.Remove(note);
        }
    }

    // ------------------------------------------------------------
    // APLICAR CONFIGURACIÓN A OSC NUEVO
    // ------------------------------------------------------------

    private void ApplySettingsToOsc(Osc osc)
    {
        UpdateVolume();
        OctaveChange();
        WaveFormChange();
        ArmonicosChange();
        AmplitudesChange();
        ToggleWavetable();

        UpdateAttack();
        UpdateDecay();
        UpdateSustainLevel();
        UpdateSustain();
        UpdateRelease();

        DetunedChange();
        TremFChange();
        VibFChange();
        VibratoDepthChange();

        FMModFrequencyChange();
        FMModIndexChange();

        UpdateSamplingSettingsFromUI();
        ApplySamplingSettingsToOscillator(osc);
        ApplyADSRClipSettingsToOscillator(osc);
        ApplyAdsrCurveModesToOscillator(osc);
    }
}
