using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

// Esta clase define un oscilador (Osc) que genera distintas formas de onda.
// Se encarga de reproducir audio en Unity usando un AudioSource y permite configurar parámetros
// como la frecuencia, la octava, el número de armónicos, el nivel de volumen y una envolvente ADSR.
public class Osc : MonoBehaviour
{
    public enum LegacyWaveformType
    {
        Sine,
        Square,
        Sawtooth,
        Triangle,
        SA,
        WhiteNoise
    }

    public enum AudioEngineBackend
    {
        LegacyOnAudioFilterRead,
        ExternalProceduralDSP
    }

    // Frecuencia de muestreo (samples por segundo).
    float FM = 44100f;

    // Frecuencia base de la forma de onda que se va a generar.
    public float frecuencia = 0;
    [FormerlySerializedAs("FOscAmp")] public float tremLFOf = 0f;
    [FormerlySerializedAs("FOscF")] public float VibLFOf = 0f;
    public float vibratoDepth = 5f;

    [Header("FM synthesis")]
    [Range(0f, 1f)] public float fmMacroAmount = 0.35f;
    [Range(0.25f, 8f)] public float fmMinRatio = 1f;
    [Range(0.25f, 12f)] public float fmMaxRatio = 5f;
    [Range(0f, 16f)] public float fmMaxIndex = 8f;
    [Range(0f, 0.5f)] public float fmHighHarmonicBlend = 0.18f;
    public float fmModFrequency = 220f;
    public float fmModIndex = 1f;

    [Header("Sampling")]
    public AudioClip samplingClip;

    [Tooltip("Frecuencia de la nota original grabada en el AudioClip. Ejemplo: 440 Hz si el sample es un La4.")]
    public float samplingBaseFrequency = 440f;

    [Range(0f, 1f)]
    public float samplingStartNormalized = 0f;

    [Range(0f, 1f)]
    public float samplingEndNormalized = 1f;

    [Tooltip("Frame inicial calculado desde Polifonia.cs.")]
    public int samplingStartFrame = 0;

    [Tooltip("Frame final calculado desde Polifonia.cs.")]
    public int samplingEndFrame = 0;

    private float[] samplingData;
    private int samplingChannels = 1;
    private int samplingTotalFrames = 0;
    private AudioClip loadedSamplingClip;
    private float samplingReadPosition = 0f;

    // Octava sobre la que se va a calcular la frecuencia final de la nota.
    public int Octava = 4;

    // Número de armónicos que se usarán en la forma de onda SA.
    public int Narmonicos;

    // Nivel de amplitud o volumen.
    public float Level = 0.5f;

    // Componente de audio de Unity que reproducirá el sonido generado.
    public AudioSource OscAudio;
    public AudioEngineBackend audioEngineBackend = AudioEngineBackend.LegacyOnAudioFilterRead;
    public int streamingChannels = 2;
    public int streamingClipSeconds = 1;

    // Diccionario para almacenar valores calculados de la envolvente (ADSR).
    private Dictionary<int, float> adsrCache = new Dictionary<int, float>();

    // Bandera para indicar si se actualizó la envolvente ADSR.
    private bool adsrUpdated = true;

    [Header("ADSR desde AudioClip")]
    [Tooltip("AudioClip recibido desde Polifonia.cs para construir una envolvente positiva normalizada entre 0 y 1.")]
    public AudioClip adsrSourceClip;

    [Tooltip("Si está activo, se usa el arreglo calculado desde adsrSourceClip como envolvente en lugar del ADSR procedural.")]
    public bool useAudioClipADSR = false;

    private float[] audioClipADSRData;
    private AudioClip loadedADSRClip;
    private bool audioClipADSRReady = false;

    // Indica si se utilizará una wavetable en lugar de la generación directa.
    private bool useWavetable = false;
    private bool currentWavetableLoadedFromExternal = false;

    // Arreglo que contendrá los valores precalculados de la forma de onda cuando se use wavetable.
    private float[] wavetable;

    [Header("Loaders de wavetables externas")]
    public bool autoUseWavetableForSamplingAndCustom = true;
    public WavetableTxtLoader sineWavetableLoader;
    public WavetableTxtLoader squareWavetableLoader;
    public WavetableTxtLoader sawtoothWavetableLoader;
    public WavetableTxtLoader triangleWavetableLoader;
    public WavetableTxtLoader saWavetableLoader;
    public WavetableTxtLoader samplingWavetableLoader;
    public WavetableTxtLoader custom1WavetableLoader;
    public WavetableTxtLoader custom2WavetableLoader;
    public WavetableTxtLoader custom3WavetableLoader;

    // Tamaño de la tabla de ondas usada internamente por el oscilador.
    private const int wavetableSize = 2048;

    // Variables para manejar desafinación.
    [FormerlySerializedAs("DetuneCents")] public float detuneCents = 0f;
    public int detuneFrames = 0;

    // Enum que define los tipos de forma de onda soportados.
    public enum WaveFormType
    {
        Sine,
        Square,
        Sawtooth,
        Triangle,
        SA,
        FM,
        Sampling1,
        WhiteNoise,
        Custom1,
        Custom2,
        Custom3
    }

    // Indica qué forma de onda se está usando actualmente.
    public WaveFormType FormType = WaveFormType.Sine;

    // Arreglo que define los niveles de amplitud para cada armónico en la forma de onda SA.
    public float[] AmplitudesLv = new float[10];

    [Header("Compatibilidad escena legacy")]
    public float OutputLevel = 1f;
    public AudioSource Aud;
    public LegacyWaveformType waveformType = LegacyWaveformType.Sine;
    public float[] Amplitudes = new float[] { 1f, 0.7f, 0.8f, 0.6f, 0.5f, 0.4f, 0.5f, 0.6f, 0.7f, 0.4f };
    [FormerlySerializedAs("SLevel")] public float SLegacy = 0.7f;
    public float AF = 5f;
    public float DF = 10f;
    public float LowEnvAF = 1f;
    public float HighEnvDF = 1f;
    public float FreqAlt = 1.5f;
    public float W = 1f;
    public bool UseFilterModulation = false;
    public float FilterLevel = 0f;
    public bool UseDistortion = false;
    public float DistortionAmount = 0.35f;

    // Variables auxiliares para el cálculo en OnAudioFilterRead.
    float X = 0;
    int TimeIndex = 0;
    private AudioClip streamingClip;
    private ProceduralSynthVoice proceduralSynthVoice;
    private bool externalBackendDirty = true;
    private bool noteIsActive = false;
    private bool legacyValuesApplied = false;
    private bool lastMuteState = false;
    private float lastAudioSourceVolume = 1f;

    void Awake()
    {
        if (OscAudio == null)
            OscAudio = gameObject.GetComponent<AudioSource>();

        if (OscAudio != null)
            OscAudio.spatialBlend = 0;

        ApplyLegacySerializedValuesIfNeeded();
        BuildAudioClipADSRData();

        LoadAssignedWavetableLoaders();
        GenerateWavetable();
        InitializeExternalBackendIfNeeded();
    }

    void Update()
    {
        if (OscAudio != null)
        {
            if (lastMuteState != OscAudio.mute || !Mathf.Approximately(lastAudioSourceVolume, OscAudio.volume))
            {
                lastMuteState = OscAudio.mute;
                lastAudioSourceVolume = OscAudio.volume;
                MarkExternalBackendDirty();
            }
        }

        SyncExternalBackendIfNeeded();
    }

    void OnDestroy()
    {
        if (proceduralSynthVoice != null)
        {
            proceduralSynthVoice.Dispose();
            proceduralSynthVoice = null;
        }
    }

    // Actualiza el nivel de volumen.
    public void UpdateVolume(float value)
    {
        Level = value;
        MarkExternalBackendDirty();
    }

    // Cambia la octava.
    public void OctaveChange(int value)
    {
        Octava = value;
        MarkExternalBackendDirty();
    }

    // Cambia el tipo de forma de onda.
    public void WaveFormChange(WaveFormType tipo)
    {
        if (FormType == tipo && wavetable != null)
            return;

        FormType = tipo;
        GenerateWavetable();
        MarkExternalBackendDirty();
    }

    public void UpdateFMAmount(float value)
    {
        fmMacroAmount = Mathf.Clamp01(value);
        MarkExternalBackendDirty();
    }

    public AudioSource AudSource => OscAudio;
    public AudioSource AudCompat => OscAudio;

    // Cambia la cantidad de armónicos para SA.
    public void ArmonicosChange(int value)
    {
        Narmonicos = value;

        if (ShouldUseWavetableForCurrentWaveform())
            GenerateWavetable();

        MarkExternalBackendDirty();
    }

    // Cambia la amplitud de un armónico.
    public void AmplitudesChange(int i, float value)
    {
        if (i < 0 || i >= AmplitudesLv.Length)
            return;

        AmplitudesLv[i] = value;

        if (ShouldUseWavetableForCurrentWaveform())
            GenerateWavetable();

        MarkExternalBackendDirty();
    }

    // Genera una onda seno.
    float SineWave(float f, int t)
    {
        return Mathf.Sin(2 * Mathf.PI * f * t / FM);
    }

    // Genera un componente seno para SA.
    float SineWaveSA(float f, int t, float A, int n)
    {
        return Mathf.Sin(2 * Mathf.PI * n * f * t / FM) * A;
    }

    // Genera una onda FM controlada por un único parámetro macro.
    // El macro mueve internamente relación de modulación, índice y brillo.
    float FMWave(float carrierFrequency, int t)
    {
        if (carrierFrequency <= 0f)
            return 0f;

        float amount = Mathf.Clamp01(fmMacroAmount);
        float shapedAmount = Mathf.SmoothStep(0f, 1f, amount);
        float ratio = Mathf.Lerp(Mathf.Max(0.25f, fmMinRatio), Mathf.Max(0.25f, fmMaxRatio), shapedAmount);
        float indexLimit = Mathf.Max(0f, fmMaxIndex * Mathf.Max(0.05f, fmModIndex));
        float modulationIndex = Mathf.Lerp(0f, indexLimit, Mathf.Pow(amount, 0.8f));
        float modulatorFrequency = Mathf.Min(carrierFrequency * ratio, FM * 0.45f);
        float harmonicBlend = Mathf.Lerp(0f, Mathf.Clamp01(fmHighHarmonicBlend), shapedAmount);

        fmModFrequency = modulatorFrequency;

        float carrierPhase = 2f * Mathf.PI * carrierFrequency * t / FM;
        float modulatorPhase = 2f * Mathf.PI * modulatorFrequency * t / FM;

        float modulation = Mathf.Sin(modulatorPhase);
        float primary = Mathf.Sin(carrierPhase + modulationIndex * modulation);
        float brightLayer = Mathf.Sin((carrierPhase * 2f) + (modulationIndex * 0.5f * modulation));
        return Mathf.Lerp(primary, 0.82f * primary + 0.18f * brightLayer, harmonicBlend);
    }

    // Genera una onda cuadrada.
    float SquareWave(float f, int t)
    {
        return Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t / FM));
    }

    // Interpolación lineal entre dos puntos.
    float LinearInterpolation(float x, float x0, float x1, float y0, float y1)
    {
        float y = y0 + (y1 - y0) * (x - x0) / (x1 - x0);
        return y;
    }

    // Genera una onda triangular.
    float TriangleWave(float f, int Index)
    {
        if (f <= 0f) return 0f;

        float T = FM / f;
        float t = Index % T;

        float t1 = 0;
        float t2 = T / 4f;
        float t3 = 3 * T / 4f;
        float t4 = T;

        float x1 = 0f, x2 = 1f, x3 = -1f, x4 = 0f;

        if (t <= t2)
            return LinearInterpolation(t, t1, t2, x1, x2);
        else if (t <= t3)
            return LinearInterpolation(t, t2, t3, x2, x3);
        else
            return LinearInterpolation(t, t3, t4, x3, x4);
    }

    // Genera una onda diente de sierra.
    float SawtoothWave(float f, int Index)
    {
        if (f <= 0f) return 0f;

        float T = FM / f;
        float t = Index % T;

        float t1 = 0;
        float t2 = T;

        float x1 = 1f, x2 = -1f;

        return LinearInterpolation(t, t1, t2, x1, x2);
    }

    // Genera la forma de onda SA (sumatoria de armónicos).
    float SA(float f, int t, int Armonicos)
    {
        if (Armonicos <= 0) return 0f;

        float x = 0f;

        for (int n = 1; n <= Armonicos; n++)
        {
            if (n - 1 >= AmplitudesLv.Length)
                break;

            float A = AmplitudesLv[n - 1];
            x += SineWaveSA(f, t, A, n);
        }

        return x / Armonicos;
    }

    private static System.Random rng = new System.Random();

    float WhiteNoise(int i)
    {
        return (float)rng.NextDouble() * 2f - 1f;
    }

    // ------------------------------------------------------------
    // SAMPLING
    // ------------------------------------------------------------

    private void EnsureSamplingDataLoaded()
    {
        if (samplingClip == null)
        {
            samplingData = null;
            samplingChannels = 1;
            samplingTotalFrames = 0;
            loadedSamplingClip = null;
            return;
        }

        if (loadedSamplingClip == samplingClip && samplingData != null && samplingData.Length > 0)
            return;

        loadedSamplingClip = samplingClip;
        samplingChannels = Mathf.Max(1, samplingClip.channels);
        samplingTotalFrames = Mathf.Max(0, samplingClip.samples);

        samplingData = new float[samplingTotalFrames * samplingChannels];

        samplingClip.GetData(samplingData, 0);

        ValidateSamplingFrameRange();
        MarkExternalBackendDirty();
    }

    private void ValidateSamplingFrameRange()
    {
        if (samplingClip == null || samplingTotalFrames <= 0)
        {
            samplingStartFrame = 0;
            samplingEndFrame = 0;
            samplingReadPosition = 0f;
            return;
        }

        int lastFrame = Mathf.Max(0, samplingTotalFrames - 1);

        samplingStartNormalized = Mathf.Clamp01(samplingStartNormalized);
        samplingEndNormalized = Mathf.Clamp01(samplingEndNormalized);

        if (samplingEndNormalized <= samplingStartNormalized)
        {
            samplingEndNormalized = Mathf.Clamp01(samplingStartNormalized + 0.01f);
        }

        // Los sliders 0..1 representan directamente el porcentaje de duración del AudioClip.
        // start = 0.25 significa iniciar en el 25% del audio.
        // end = 0.75 significa terminar en el 75% del audio.
        samplingStartFrame = Mathf.Clamp(
            Mathf.RoundToInt(samplingStartNormalized * lastFrame),
            0,
            lastFrame
        );

        samplingEndFrame = Mathf.Clamp(
            Mathf.RoundToInt(samplingEndNormalized * lastFrame),
            0,
            lastFrame
        );

        if (samplingEndFrame <= samplingStartFrame)
        {
            samplingEndFrame = Mathf.Min(lastFrame, samplingStartFrame + 1);
        }
    }

    private float SamplingWave(float frequency, int timeIndex)
    {
        return SamplingWaveInternal(frequency, true);
    }

    private float SamplingWaveInternal(float frequency, bool advanceReadPosition)
    {
        if (frequency <= 0f)
            return 0f;

        EnsureSamplingDataLoaded();

        if (samplingClip == null || samplingData == null || samplingData.Length == 0 || samplingTotalFrames <= 0)
            return 0f;

        ValidateSamplingFrameRange();

        int playableLength = samplingEndFrame - samplingStartFrame;

        if (playableLength <= 0)
            return 0f;

        float safeBaseFrequency = Mathf.Max(1f, samplingBaseFrequency);

        // Si la frecuencia de la nota coincide con la frecuencia base,
        // se reproduce el fragmento seleccionado a velocidad normal.
        // Si la nota es más aguda, avanza más rápido; si es más grave, más lento.
        float playbackRatio = frequency / safeBaseFrequency;

        float samplePosition = samplingReadPosition;

        if (samplePosition >= samplingEndFrame)
            return 0f;

        int frame0 = Mathf.FloorToInt(samplePosition);
        int frame1 = Mathf.Min(frame0 + 1, samplingEndFrame);
        float frac = samplePosition - frame0;

        frame0 = Mathf.Clamp(frame0, samplingStartFrame, samplingEndFrame);
        frame1 = Mathf.Clamp(frame1, samplingStartFrame, samplingEndFrame);

        float value0 = GetSamplingFrameMono(frame0);
        float value1 = GetSamplingFrameMono(frame1);
        float output = Mathf.Lerp(value0, value1, frac);

        if (advanceReadPosition)
        {
            samplingReadPosition += playbackRatio;
        }

        return output;
    }

    private float GetSamplingFrameMono(int frame)
    {
        if (samplingData == null || samplingData.Length == 0)
            return 0f;

        frame = Mathf.Clamp(frame, 0, Mathf.Max(0, samplingTotalFrames - 1));

        int baseIndex = frame * samplingChannels;

        if (baseIndex < 0 || baseIndex >= samplingData.Length)
            return 0f;

        if (samplingChannels == 1)
            return samplingData[baseIndex];

        float sum = 0f;
        int validChannels = 0;

        for (int ch = 0; ch < samplingChannels; ch++)
        {
            int index = baseIndex + ch;

            if (index >= 0 && index < samplingData.Length)
            {
                sum += samplingData[index];
                validChannels++;
            }
        }

        if (validChannels <= 0)
            return 0f;

        return sum / validChannels;
    }

    private void LoadAssignedWavetableLoaders()
    {
        TryLoadLoader(sineWavetableLoader);
        TryLoadLoader(squareWavetableLoader);
        TryLoadLoader(sawtoothWavetableLoader);
        TryLoadLoader(triangleWavetableLoader);
        TryLoadLoader(saWavetableLoader);
        TryLoadLoader(samplingWavetableLoader);
        TryLoadLoader(custom1WavetableLoader);
        TryLoadLoader(custom2WavetableLoader);
        TryLoadLoader(custom3WavetableLoader);
    }

    private void TryLoadLoader(WavetableTxtLoader loader)
    {
        if (loader == null)
            return;

        loader.LoadWavetable();
    }

    private bool TryBuildWavetableFromLoader(WavetableTxtLoader loader)
    {
        if (loader == null)
            return false;

        if (loader.samples == null || loader.samples.Length == 0)
            loader.LoadWavetable();

        if (loader.samples == null || loader.samples.Length == 0)
            return false;

        wavetable = new float[wavetableSize];

        int sourceLength = loader.samples.Length;

        for (int i = 0; i < wavetableSize; i++)
        {
            float readPos = ((float)i / wavetableSize) * sourceLength;
            int index0 = Mathf.FloorToInt(readPos) % sourceLength;
            int index1 = (index0 + 1) % sourceLength;
            float frac = readPos - Mathf.Floor(readPos);

            wavetable[i] = Mathf.Lerp(loader.samples[index0], loader.samples[index1], frac);
        }

        return true;
    }

    private WavetableTxtLoader GetLoaderForCurrentWaveform()
    {
        switch (FormType)
        {
            case WaveFormType.Sine:
                return sineWavetableLoader;

            case WaveFormType.Square:
                return squareWavetableLoader;

            case WaveFormType.Sawtooth:
                return sawtoothWavetableLoader;

            case WaveFormType.Triangle:
                return triangleWavetableLoader;

            case WaveFormType.SA:
                return saWavetableLoader;

            case WaveFormType.Sampling1:
                return samplingWavetableLoader;

            case WaveFormType.Custom1:
                return custom1WavetableLoader;

            case WaveFormType.Custom2:
                return custom2WavetableLoader;

            case WaveFormType.Custom3:
                return custom3WavetableLoader;

            default:
                return null;
        }
    }

    private bool ShouldUseWavetableForCurrentWaveform()
    {
        return useWavetable || ShouldAutoUseWavetableForCurrentWaveform();
    }

    private bool ShouldAutoUseWavetableForCurrentWaveform()
    {
        if (!autoUseWavetableForSamplingAndCustom)
            return false;

        return FormType == WaveFormType.Sampling1 ||
               FormType == WaveFormType.Custom1 ||
               FormType == WaveFormType.Custom2 ||
               FormType == WaveFormType.Custom3;
    }

    private bool ShouldReadFromWavetableInAudio()
    {
        if (FormType == WaveFormType.FM)
            return false;

        if (FormType == WaveFormType.Sampling1)
            return currentWavetableLoadedFromExternal;

        if (FormType == WaveFormType.Custom1 ||
            FormType == WaveFormType.Custom2 ||
            FormType == WaveFormType.Custom3)
            return currentWavetableLoadedFromExternal;

        return useWavetable;
    }

    // Genera o regenera la tabla de ondas.
    public void GenerateWavetable()
    {
        currentWavetableLoadedFromExternal = false;

        if (ShouldUseWavetableForCurrentWaveform())
        {
            WavetableTxtLoader loader = GetLoaderForCurrentWaveform();

            if (TryBuildWavetableFromLoader(loader))
            {
                currentWavetableLoadedFromExternal = true;
                return;
            }
        }

        wavetable = new float[wavetableSize];

        for (int i = 0; i < wavetableSize; i++)
        {
            float f = FM / wavetableSize;

            switch (FormType)
            {
                case WaveFormType.Sine:
                    wavetable[i] = SineWave(f, i);
                    break;

                case WaveFormType.Square:
                    wavetable[i] = SquareWave(f, i);
                    break;

                case WaveFormType.Sawtooth:
                    wavetable[i] = SawtoothWave(f, i);
                    break;

                case WaveFormType.Triangle:
                    wavetable[i] = TriangleWave(f, i);
                    break;

                case WaveFormType.SA:
                    wavetable[i] = SA(f, i, Narmonicos);
                    break;

                case WaveFormType.FM:
                    wavetable[i] = FMWave(f, i);
                    break;

                case WaveFormType.Sampling1:
                    wavetable[i] = 0f;
                    break;

                case WaveFormType.WhiteNoise:
                    wavetable[i] = WhiteNoise(i);
                    break;

                case WaveFormType.Custom1:
                case WaveFormType.Custom2:
                case WaveFormType.Custom3:
                    wavetable[i] = 0f;
                    break;
            }
        }
    }

    // Obtiene un valor de la tabla de ondas según la frecuencia y el índice actual.
    private float GetWavetableSample(int index, float frequency)
    {
        if (wavetable == null || wavetable.Length == 0)
            return 0f;

        if (frequency <= 0f && FormType != WaveFormType.WhiteNoise)
            return 0f;

        int wavetableIndex = Mathf.RoundToInt((index * frequency / FM) * wavetableSize) % wavetableSize;

        if (wavetableIndex < 0)
            wavetableIndex += wavetableSize;

        return wavetable[wavetableIndex];
    }

    // Activa o desactiva el uso de la tabla de ondas y regenera la tabla.
    public void ToggleWavetable(bool value)
    {
        useWavetable = value;
        LoadAssignedWavetableLoaders();
        GenerateWavetable();
        MarkExternalBackendDirty();
    }

    // Parámetros ADSR.
    public float A = 5;
    public float D = 5;
    public float S = 5;
    public float SL = 0.7f;
    public float R = 5;

    public void UpdateADSR()
    {
        adsrCache.Clear();
        adsrUpdated = true;
        MarkExternalBackendDirty();
    }

    public void UpdateAttack(int value)
    {
        A = value;
        UpdateADSR();
    }

    public void UpdateDecay(int value)
    {
        D = value;
        UpdateADSR();
    }

    public void UpdateSustain(int value)
    {
        S = value;
        UpdateADSR();
    }

    public void UpdateSustainLevel(float value)
    {
        SL = value;
        SLegacy = value;
        UpdateADSR();
    }

    public void UpdateRelease(int value)
    {
        R = value;
        UpdateADSR();
    }

    // ------------------------------------------------------------
    // ADSR DESDE AUDIOCLIP
    // ------------------------------------------------------------

    public void BuildAudioClipADSRData()
    {
        if (adsrSourceClip == null)
        {
            audioClipADSRData = null;
            loadedADSRClip = null;
            audioClipADSRReady = false;
            return;
        }

        if (loadedADSRClip == adsrSourceClip && audioClipADSRData != null && audioClipADSRData.Length > 0)
        {
            audioClipADSRReady = true;
            return;
        }

        loadedADSRClip = adsrSourceClip;

        int channels = Mathf.Max(1, adsrSourceClip.channels);
        int totalFrames = Mathf.Max(0, adsrSourceClip.samples);

        if (totalFrames <= 0)
        {
            audioClipADSRData = null;
            audioClipADSRReady = false;
            return;
        }

        float[] rawData = new float[totalFrames * channels];
        adsrSourceClip.GetData(rawData, 0);

        audioClipADSRData = new float[totalFrames];
        float maxValue = 0f;

        for (int frame = 0; frame < totalFrames; frame++)
        {
            float monoValue = 0f;

            for (int ch = 0; ch < channels; ch++)
            {
                int index = frame * channels + ch;

                if (index >= 0 && index < rawData.Length)
                    monoValue += rawData[index];
            }

            monoValue /= channels;

            // Procedimiento solicitado:
            // 1. Elevar al cuadrado.
            // 2. Calcular raíz cuadrada.
            // Resultado: valor positivo equivalente al valor absoluto.
            float positiveValue = Mathf.Sqrt(monoValue * monoValue);

            audioClipADSRData[frame] = positiveValue;

            if (positiveValue > maxValue)
                maxValue = positiveValue;
        }

        // Normalización del arreglo entre 0 y 1.
        if (maxValue > 0f)
        {
            for (int i = 0; i < audioClipADSRData.Length; i++)
            {
                audioClipADSRData[i] = Mathf.Clamp01(audioClipADSRData[i] / maxValue);
            }
        }
        else
        {
            for (int i = 0; i < audioClipADSRData.Length; i++)
            {
                audioClipADSRData[i] = 0f;
            }
        }

        audioClipADSRReady = true;
        MarkExternalBackendDirty();
    }

    private float GetAudioClipADSR(int t)
    {
        if (!audioClipADSRReady || loadedADSRClip != adsrSourceClip)
            BuildAudioClipADSRData();

        if (!audioClipADSRReady || audioClipADSRData == null || audioClipADSRData.Length == 0)
            return 0f;

        if (t < 0)
            return 0f;

        if (t >= audioClipADSRData.Length)
            return 0f;

        return Mathf.Clamp01(audioClipADSRData[t]);
    }

    // Calcula el factor ADSR para un tiempo dado.
    float getADSR(int t)
    {
        if (useAudioClipADSR)
        {
            return GetAudioClipADSR(t);
        }

        if (adsrUpdated)
        {
            adsrCache.Clear();
            adsrUpdated = false;
        }

        if (adsrCache.TryGetValue(t, out float value))
        {
            return value;
        }
        else
        {
            int Attack = Mathf.RoundToInt((A / 1000f) * FM);
            int Decay = Attack + Mathf.RoundToInt((D / 1000f) * FM);
            int Sustain = Decay + Mathf.RoundToInt((S / 1000f) * FM);
            int Release = Sustain + Mathf.RoundToInt((R / 1000f) * FM);

            if (Attack <= 0)
                Attack = 1;

            if (t < Attack)
            {
                value = (float)t / Attack;
            }
            else if (t < Decay)
            {
                float decayDen = Mathf.Max(1, Decay - Attack);
                value = Mathf.Lerp(1f, SL, (float)(t - Attack) / decayDen);
            }
            else if (t < Sustain)
            {
                value = SL;
            }
            else if (t < Release)
            {
                float releaseDen = Mathf.Max(1, Release - Sustain);
                value = Mathf.Lerp(SL, 0.0000001f, (float)(t - Sustain) / releaseDen);
            }
            else
            {
                value = 0.0000001f;
            }

            adsrCache[t] = value;
            return value;
        }
    }

    // Se activa la reproducción de una nota.
    public void KeyboardDown(string Note)
    {
        if (Note == "C" || Note == "c") frecuencia = 16.3516f * Mathf.Pow(2, Octava);
        else if (Note == "C#" || Note == "c#") frecuencia = 17.3239f * Mathf.Pow(2, Octava);
        else if (Note == "D" || Note == "d") frecuencia = 18.3540f * Mathf.Pow(2, Octava);
        else if (Note == "D#" || Note == "d#") frecuencia = 19.4454f * Mathf.Pow(2, Octava);
        else if (Note == "E" || Note == "e") frecuencia = 20.6017f * Mathf.Pow(2, Octava);
        else if (Note == "F" || Note == "f") frecuencia = 21.8268f * Mathf.Pow(2, Octava);
        else if (Note == "F#" || Note == "f#") frecuencia = 23.1246f * Mathf.Pow(2, Octava);
        else if (Note == "G" || Note == "g") frecuencia = 24.4997f * Mathf.Pow(2, Octava);
        else if (Note == "G#" || Note == "g#") frecuencia = 25.9565f * Mathf.Pow(2, Octava);
        else if (Note == "A" || Note == "a") frecuencia = 27.5f * Mathf.Pow(2, Octava);
        else if (Note == "A#" || Note == "a#") frecuencia = 29.1353f * Mathf.Pow(2, Octava);
        else if (Note == "B" || Note == "b") frecuencia = 30.3677f * Mathf.Pow(2, Octava);
        else if (Note == "C2" || Note == "c2") frecuencia = 2 * 16.3516f * Mathf.Pow(2, Octava);

        TimeIndex = 0;
        noteIsActive = true;

        if (useAudioClipADSR)
            BuildAudioClipADSRData();

        if (FormType == WaveFormType.Sampling1)
        {
            EnsureSamplingDataLoaded();
            ValidateSamplingFrameRange();

            // Aquí inicia exactamente desde el punto indicado por SamplingStartSlider.
            samplingReadPosition = samplingStartFrame;
        }

        if (UseExternalProceduralDSP())
        {
            InitializeExternalBackendIfNeeded();
            SyncExternalBackendIfNeeded(true);

            if (proceduralSynthVoice != null)
                proceduralSynthVoice.NoteOn(frecuencia);

            if (OscAudio != null && !OscAudio.isPlaying)
                OscAudio.Play();

            return;
        }

        if (OscAudio != null)
            OscAudio.Play();
    }

    // Detiene la reproducción.
    public void KeyboardUp()
    {
        noteIsActive = false;

        if (UseExternalProceduralDSP() && proceduralSynthVoice != null)
            proceduralSynthVoice.NoteOff();

        if (OscAudio != null)
            OscAudio.Stop();

        TimeIndex = 0;
        samplingReadPosition = samplingStartFrame;
        frecuencia = 0;
    }

    // Unity llama esto para rellenar el buffer de audio.
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (UseExternalProceduralDSP())
        {
            return;
        }

        if (data == null || data.Length == 0)
            return;

        for (int i = 0; i < data.Length; i += channels)
        {
            int currentTime = TimeIndex;

            float E = getADSR(currentTime);

            float tremValue = 1f;

            if (tremLFOf > 0f)
                tremValue = 0.5f + 0.5f * SineWave(tremLFOf, currentTime);

            float vibratoOffset = 0f;

            if (VibLFOf > 0f)
                vibratoOffset = vibratoDepth * SineWave(VibLFOf, currentTime);

            float currentFrequency = frecuencia + vibratoOffset;

            if (currentFrequency < 0f)
                currentFrequency = 0f;

            float detuneFactor = Mathf.Pow(2f, detuneCents / 1200f);
            float detunedFrequency = currentFrequency * detuneFactor;

            // FM se calcula directo. Sampling1 usa wavetable si tiene loader; si no, conserva AudioClip sampling.
            bool shouldUseWavetable = ShouldReadFromWavetableInAudio();

            float X;
            float X1;

            if (FormType == WaveFormType.Sampling1 && !shouldUseWavetable)
            {
                // En sampling solo se debe avanzar una vez por muestra de salida.
                // Si se llamara dos veces a SamplingWave(), el clip avanzaría al doble de velocidad.
                X = SamplingWaveInternal(currentFrequency, true);
                X1 = X;
            }
            else
            {
                X = shouldUseWavetable
                    ? GetWavetableSample(currentTime, currentFrequency)
                    : GenerateWaveSample(FormType, currentFrequency, currentTime);

                X1 = shouldUseWavetable
                    ? GetWavetableSample(currentTime, detunedFrequency)
                    : GenerateWaveSample(FormType, detunedFrequency, currentTime);
            }

            float sampleValue = Level * 0.5f * (X + X1) * E * tremValue;

            data[i] = sampleValue;

            if (channels > 1)
            {
                for (int ch = 1; ch < channels; ch++)
                    data[i + ch] = sampleValue;
            }

            TimeIndex++;
        }
    }

    // Generación directa sin wavetable.
    private float GenerateWaveSample(WaveFormType type, float f, int t)
    {
        if (f <= 0f && type != WaveFormType.WhiteNoise)
            return 0f;

        switch (type)
        {
            case WaveFormType.Sine:
                return SineWave(f, t);

            case WaveFormType.Square:
                return SquareWave(f, t);

            case WaveFormType.Sawtooth:
                return SawtoothWave(f, t);

            case WaveFormType.Triangle:
                return TriangleWave(f, t);

            case WaveFormType.SA:
                return SA(f, t, Narmonicos);

            case WaveFormType.FM:
                return FMWave(f, t);

            case WaveFormType.Sampling1:
                return SamplingWave(f, t);

            case WaveFormType.WhiteNoise:
                return WhiteNoise(t);

            case WaveFormType.Custom1:
            case WaveFormType.Custom2:
            case WaveFormType.Custom3:
                return 0f;

            default:
                return 0f;
        }
    }

    private void ApplyLegacySerializedValuesIfNeeded()
    {
        if (legacyValuesApplied)
            return;

        bool hasLegacyValues = Aud != null ||
                               OutputLevel != 1f ||
                               detuneFrames != 0 ||
                               SLegacy != 0.7f ||
                               AF != 5f ||
                               DF != 10f ||
                               LowEnvAF != 1f ||
                               HighEnvDF != 1f ||
                               FreqAlt != 1.5f ||
                               W != 1f ||
                               UseFilterModulation ||
                               FilterLevel != 0f ||
                               UseDistortion ||
                               DistortionAmount != 0.35f;

        if (!hasLegacyValues)
        {
            Aud = OscAudio;
            legacyValuesApplied = true;
            return;
        }

        if (OscAudio == null && Aud != null)
            OscAudio = Aud;

        Level = OutputLevel;
        SL = SLegacy;

        if (Amplitudes != null && Amplitudes.Length > 0)
        {
            int count = Mathf.Min(Amplitudes.Length, AmplitudesLv.Length);
            for (int index = 0; index < count; index++)
                AmplitudesLv[index] = Amplitudes[index];
        }

        FormType = ConvertLegacyWaveformType(waveformType);
        audioEngineBackend = AudioEngineBackend.ExternalProceduralDSP;
        Aud = OscAudio;
        externalBackendDirty = true;
        legacyValuesApplied = true;
    }

    private WaveFormType ConvertLegacyWaveformType(LegacyWaveformType legacyType)
    {
        switch (legacyType)
        {
            case LegacyWaveformType.Sine:
                return WaveFormType.Sine;
            case LegacyWaveformType.Square:
                return WaveFormType.Square;
            case LegacyWaveformType.Sawtooth:
                return WaveFormType.Sawtooth;
            case LegacyWaveformType.Triangle:
                return WaveFormType.Triangle;
            case LegacyWaveformType.SA:
                return WaveFormType.SA;
            case LegacyWaveformType.WhiteNoise:
                return WaveFormType.WhiteNoise;
            default:
                return WaveFormType.Sine;
        }
    }

    public void MarkExternalBackendDirty()
    {
        externalBackendDirty = true;
    }

    private bool UseExternalProceduralDSP()
    {
        return audioEngineBackend == AudioEngineBackend.ExternalProceduralDSP;
    }

    private void InitializeExternalBackendIfNeeded()
    {
        if (!UseExternalProceduralDSP())
            return;

        if (OscAudio == null)
            OscAudio = gameObject.GetComponent<AudioSource>();

#if UNITY_WEBGL && !UNITY_EDITOR
        if (proceduralSynthVoice == null)
            proceduralSynthVoice = new ProceduralSynthVoice(Mathf.RoundToInt(FM), Mathf.Max(1, streamingChannels));

        return;
#endif

        if (streamingClip == null)
        {
            int sampleRate = Mathf.RoundToInt(FM);
            int channels = Mathf.Max(1, streamingChannels);
            int lengthSamples = Mathf.Max(sampleRate / 4, sampleRate * Mathf.Max(1, streamingClipSeconds));
            streamingClip = AudioClip.Create($"{name}_ExternalDSP", lengthSamples, channels, sampleRate, true, OnStreamingClipRead);
        }

        if (OscAudio != null && OscAudio.clip != streamingClip)
        {
            OscAudio.clip = streamingClip;
            OscAudio.loop = true;
            OscAudio.playOnAwake = false;
        }

        if (proceduralSynthVoice == null)
            proceduralSynthVoice = new ProceduralSynthVoice(Mathf.RoundToInt(FM), Mathf.Max(1, streamingChannels));
    }

    private void OnStreamingClipRead(float[] data)
    {
        if (data == null || data.Length == 0)
            return;

        if (!UseExternalProceduralDSP() || proceduralSynthVoice == null || !noteIsActive)
        {
            System.Array.Clear(data, 0, data.Length);
            return;
        }

        proceduralSynthVoice.Render(data);
    }

    private void SyncExternalBackendIfNeeded(bool force = false)
    {
        if (!UseExternalProceduralDSP())
            return;

        InitializeExternalBackendIfNeeded();

        if (!force && !externalBackendDirty)
            return;

        EnsureSamplingDataLoaded();

        if (useAudioClipADSR)
            BuildAudioClipADSRData();

        ProceduralSynthVoiceConfig config = new ProceduralSynthVoiceConfig
        {
            sampleRate = Mathf.RoundToInt(FM),
            channels = Mathf.Max(1, streamingChannels),
            waveform = (int)FormType,
            harmonicCount = Narmonicos,
            useWavetable = ShouldReadFromWavetableInAudio(),
            useAudioClipADSR = useAudioClipADSR,
            level = GetEffectiveOutputLevel(),
            detuneCents = detuneCents,
            tremLfoFrequency = tremLFOf,
            vibLfoFrequency = VibLFOf,
            vibratoDepth = vibratoDepth,
            attackMs = A,
            decayMs = D,
            sustainMs = S,
            sustainLevel = SL,
            releaseMs = R,
            fmMacroAmount = fmMacroAmount,
            fmMinRatio = fmMinRatio,
            fmMaxRatio = fmMaxRatio,
            fmMaxIndex = fmMaxIndex,
            fmHighHarmonicBlend = fmHighHarmonicBlend,
            fmModFrequency = fmModFrequency,
            fmModIndex = fmModIndex,
            samplingBaseFrequency = samplingBaseFrequency,
            samplingStartFrame = samplingStartFrame,
            samplingEndFrame = samplingEndFrame,
            samplingChannels = samplingChannels,
            samplingTotalFrames = samplingTotalFrames,
            amplitudes = (float[])AmplitudesLv.Clone(),
            wavetable = wavetable != null ? (float[])wavetable.Clone() : new float[0],
            samplingData = samplingData != null ? (float[])samplingData.Clone() : new float[0],
            audioClipAdsrData = audioClipADSRData != null ? (float[])audioClipADSRData.Clone() : new float[0]
        };

        if (proceduralSynthVoice != null)
            proceduralSynthVoice.UpdateConfig(config);

        externalBackendDirty = false;
    }

    private float GetEffectiveOutputLevel()
    {
        float audioSourceVolume = OscAudio != null ? OscAudio.volume : 1f;
        bool isMuted = OscAudio != null && OscAudio.mute;
        return isMuted ? 0f : Level * audioSourceVolume;
    }
}
