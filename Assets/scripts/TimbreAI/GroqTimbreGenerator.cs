using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Proyecto: Síntesis de Audio Procedural con Inteligencia Artificial.
/// Desarrollado por: Miguel Ángel Olivares Tenorio.
/// Descripción: Solicita descriptores tímbricos a GroqCloud, construye presets locales
/// y entrega el resultado al sistema de polifonía sin ejecutar síntesis de audio en la API.
/// </summary>
public class GroqTimbreGenerator : MonoBehaviour
{
    public const int WavetableSize = 2048;
    public const string EditorPrefsApiKeyKey = "SintesisProceduralIA.Groq.ApiKey";
    private const string GroqEndpoint = "https://api.groq.com/openai/v1/chat/completions";

    [Header("Target synth")]
    public Polifonia targetPolifonia;
    public Osc targetOsc;

    [Header("GroqCloud")]
    [SerializeField, HideInInspector, FormerlySerializedAs("groqApiKey")]
    private string legacySerializedGroqApiKey = string.Empty;
    [NonSerialized]
    private string sessionGroqApiKey = string.Empty;
    public string modelName = "openai/gpt-oss-20b";
    [Range(1, 120)]
    public int requestTimeoutSeconds = 30;
    [Tooltip("GPT-OSS can spend some completion tokens on reasoning before visible JSON appears. Keep enough room for the full structured response.")]
    [Range(128, 4096)]
    public int maxCompletionTokens = 900;
    public bool applyFallbackWhenGroqCloudFails = false;

    [Header("Prompt UI")]
    [Tooltip("Campo opcional para introducir una API key durante ejecución. No se guarda en disco.")]
    public TMP_InputField runtimeApiKeyInput;
    public Button applyRuntimeApiKeyButton;
    public Button clearRuntimeApiKeyButton;
    public TMP_InputField promptInput;
    public Button generatePromptButton;

    [TextArea(2, 5)]
    public string fallbackPrompt = "Warm analog bass with fast attack, strong fundamental, few high harmonics and slow release.";

    [Header("Audio UI")]
    public AudioClip referenceAudioClip;
    public Button analyzeAudioButton;
    public bool useGroqToComplementAudioAnalysis = false;

    [Header("Status")]
    public TextMeshProUGUI statusText;
    public bool logDetails = true;

    [Serializable]
    public class HarmonicDescriptor
    {
        public int index = 1;
        public float amplitude = 1f;
        public float phaseRadians = 0f;
    }

    [Serializable]
    public class TimbreDescriptor
    {
        public float brightness = 0.35f;
        public float harmonicity = 0.8f;
        public float noisiness = 0.05f;
        public float warmth = 0.5f;
        public float fundamentalStrength = 1f;
        public HarmonicDescriptor[] harmonics = Array.Empty<HarmonicDescriptor>();
    }

    [Serializable]
    public class ADSRDescriptor
    {
        public float attackMs = 10f;
        public float decayMs = 180f;
        public float sustainMs = 600f;
        public float sustainLevel = 0.65f;
        public float releaseMs = 300f;
    }

    [Serializable]
    public class GeneratedTimbre
    {
        public TimbrePreset preset = new TimbrePreset();
        public TimbreDescriptor timbre = new TimbreDescriptor();
        public ADSRDescriptor adsr = new ADSRDescriptor();
        public float[] wavetable = Array.Empty<float>();
        public string source = "Unknown";
        public bool usedGroq;
        public bool usedLocalDsp;
        public string message = string.Empty;

        public bool IsUsable()
        {
            return preset != null && wavetable != null && wavetable.Length == WavetableSize && adsr != null;
        }
    }

    [Serializable]
    private class GroqStructuredTimbre
    {
        public string timbreName = "Groq Timbre";
        public TimbreDescriptor timbre = new TimbreDescriptor();
        public ADSRDescriptor adsr = new ADSRDescriptor();
        public HarmonicDescriptor[] harmonics = Array.Empty<HarmonicDescriptor>();
        public float[] attackHarmonics = Array.Empty<float>();
        public float[] sustainHarmonics = Array.Empty<float>();
        public float[] releaseHarmonics = Array.Empty<float>();
    }

#pragma warning disable 0649
    [Serializable]
    private class GroqChatCompletionResponse
    {
        public GroqChoice[] choices;
        public GroqError error;
    }

    [Serializable]
    private class GroqChoice
    {
        public int index;
        public GroqMessage message;
        public string finish_reason;
    }

    [Serializable]
    private class GroqMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class GroqError
    {
        public int code;
        public string message;
        public string status;
    }
#pragma warning restore 0649

    private Coroutine runningRequest;
    private GeneratedTimbre lastValidTimbre;

    public bool HasSessionApiKey => !string.IsNullOrWhiteSpace(sessionGroqApiKey);

    private void Awake()
    {
        MigrateLegacySerializedApiKeyIfNeeded();
        LoadEditorApiKeyIntoSession();

        if (targetPolifonia == null)
            targetPolifonia = FindAnyObjectByType<Polifonia>();

        if (targetOsc == null)
            targetOsc = FindAnyObjectByType<Osc>();
    }

    private void OnEnable()
    {
        if (applyRuntimeApiKeyButton != null)
            applyRuntimeApiKeyButton.onClick.AddListener(ApplyRuntimeApiKeyFromUI);

        if (clearRuntimeApiKeyButton != null)
            clearRuntimeApiKeyButton.onClick.AddListener(ClearSessionApiKey);

        if (generatePromptButton != null)
            generatePromptButton.onClick.AddListener(GenerateFromPromptUI);

        if (analyzeAudioButton != null)
            analyzeAudioButton.onClick.AddListener(GenerateFromAudioUI);
    }

    private void OnDisable()
    {
        if (applyRuntimeApiKeyButton != null)
            applyRuntimeApiKeyButton.onClick.RemoveListener(ApplyRuntimeApiKeyFromUI);

        if (clearRuntimeApiKeyButton != null)
            clearRuntimeApiKeyButton.onClick.RemoveListener(ClearSessionApiKey);

        if (generatePromptButton != null)
            generatePromptButton.onClick.RemoveListener(GenerateFromPromptUI);

        if (analyzeAudioButton != null)
            analyzeAudioButton.onClick.RemoveListener(GenerateFromAudioUI);
    }

    /// <summary>
    /// Guarda una API key solo en memoria para la sesión actual.
    /// En WebGL este valor existe en el cliente mientras se usa y no debe considerarse secreto de producción.
    /// </summary>
    /// <param name="apiKey">Clave introducida por el usuario en el Inspector o en la UI de ejecución.</param>
    public void SetSessionApiKey(string apiKey)
    {
        sessionGroqApiKey = string.IsNullOrWhiteSpace(apiKey) ? string.Empty : apiKey.Trim();
        SetStatus(HasSessionApiKey ? "Groq API Key configurada para esta sesión." : "Groq API Key no configurada.");
    }

    /// <summary>
    /// Elimina la API key de la memoria de ejecución y limpia el campo visual opcional.
    /// No borra EditorPrefs; esa acción corresponde al Inspector del Editor.
    /// </summary>
    public void ClearSessionApiKey()
    {
        sessionGroqApiKey = string.Empty;

        if (runtimeApiKeyInput != null)
            runtimeApiKeyInput.SetTextWithoutNotify(string.Empty);

        SetStatus("Groq API Key eliminada de la sesión.");
    }

    /// <summary>
    /// Lee la API key escrita en la UI de ejecución y la conserva únicamente en memoria.
    /// </summary>
    public void ApplyRuntimeApiKeyFromUI()
    {
        SetSessionApiKey(runtimeApiKeyInput != null ? runtimeApiKeyInput.text : string.Empty);
    }

    private void MigrateLegacySerializedApiKeyIfNeeded()
    {
#if UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(legacySerializedGroqApiKey))
            return;

        if (!UnityEditor.EditorPrefs.HasKey(EditorPrefsApiKeyKey))
            UnityEditor.EditorPrefs.SetString(EditorPrefsApiKeyKey, legacySerializedGroqApiKey.Trim());

        sessionGroqApiKey = legacySerializedGroqApiKey.Trim();
        legacySerializedGroqApiKey = string.Empty;
        UnityEditor.EditorUtility.SetDirty(this);
#else
        legacySerializedGroqApiKey = string.Empty;
#endif
    }

    private void LoadEditorApiKeyIntoSession()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(sessionGroqApiKey))
            return;

        sessionGroqApiKey = UnityEditor.EditorPrefs.GetString(EditorPrefsApiKeyKey, string.Empty);
#endif
    }

#if UNITY_EDITOR
    public static bool HasEditorStoredApiKey()
    {
        return !string.IsNullOrWhiteSpace(UnityEditor.EditorPrefs.GetString(EditorPrefsApiKeyKey, string.Empty));
    }

    public static void SaveEditorApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            UnityEditor.EditorPrefs.DeleteKey(EditorPrefsApiKeyKey);
        else
            UnityEditor.EditorPrefs.SetString(EditorPrefsApiKeyKey, apiKey.Trim());
    }

    public static void DeleteEditorApiKey()
    {
        UnityEditor.EditorPrefs.DeleteKey(EditorPrefsApiKeyKey);
    }
#endif

    [ContextMenu("Generate From Prompt")]
    public void GenerateFromPromptUI()
    {
        string prompt = promptInput != null ? promptInput.text : fallbackPrompt;
        GenerateFromPrompt(prompt);
    }

    [ContextMenu("Generate From AudioClip")]
    public void GenerateFromAudioUI()
    {
        GenerateFromAudio(referenceAudioClip);
    }

    public void GenerateFromPrompt(string prompt)
    {
        if (runningRequest != null)
            StopCoroutine(runningRequest);

        runningRequest = StartCoroutine(GenerateFromPromptCoroutine(prompt));
    }

    [ContextMenu("Generate Local Test Timbre")]
    public void GenerateLocalTestTimbre()
    {
        string prompt = promptInput != null && !string.IsNullOrWhiteSpace(promptInput.text) ? promptInput.text : fallbackPrompt;
        GeneratedTimbre generated = BuildFallbackTimbre(prompt, "Generado localmente para probar descriptor, wavetable y ADSR sin GroqCloud.");
        ApplyGeneratedTimbre(generated, null);
    }

    public IEnumerator GenerateFromPromptCoroutine(string prompt, Action<GeneratedTimbre> onGenerated = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            ReportFailure("Prompt vacío. No se generó un timbre nuevo.");
            yield break;
        }

        string apiKey = string.IsNullOrWhiteSpace(sessionGroqApiKey) ? string.Empty : sessionGroqApiKey.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ReportFailure("Configura la Groq API Key en el Inspector o en la UI de ejecución antes de llamar a GroqCloud.");

            if (applyFallbackWhenGroqCloudFails)
                ApplyGeneratedTimbre(BuildFallbackTimbre(prompt, "AI_GENERATION=FAILED. FALLBACK=USED. Falta groqApiKey."), onGenerated);

            yield break;
        }

        SetStatus("Solicitando descriptor tímbrico a GroqCloud...");

        using (UnityWebRequest request = BuildGroqRequest(prompt, apiKey))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = BuildHttpErrorMessage(request);
                ReportFailure(error);

                if (applyFallbackWhenGroqCloudFails)
                    ApplyGeneratedTimbre(BuildFallbackTimbre(prompt, "AI_GENERATION=FAILED. FALLBACK=USED. " + error), onGenerated);

                yield break;
            }

            if (!TryParseGroqResponse(request.downloadHandler.text, out TimbreDescriptor descriptor, out ADSRDescriptor adsr, out TimbrePreset preset, out string parseError))
            {
                ReportFailure(parseError);

                if (applyFallbackWhenGroqCloudFails)
                    ApplyGeneratedTimbre(BuildFallbackTimbre(prompt, "AI_GENERATION=FAILED. FALLBACK=USED. " + parseError), onGenerated);

                yield break;
            }

            GeneratedTimbre generated = BuildGeneratedTimbre(preset, descriptor, adsr, "Prompt", true, false, "AI_GENERATION=SUCCESS. Generado desde prompt con GroqCloud.");
            ApplyGeneratedTimbre(generated, onGenerated);
        }
    }

    public GeneratedTimbre GenerateFromAudio(AudioClip clip)
    {
        if (clip == null)
        {
            ReportFailure("AudioClip inválido. Asigna un AudioClip antes de analizar.");
            return null;
        }

        SetStatus("Analizando AudioClip localmente...");

        if (!TryAnalyzeAudioClip(clip, out GeneratedTimbre generated, out string error))
        {
            ReportFailure(error);
            return null;
        }

        ApplyGeneratedTimbre(generated, null);

        if (useGroqToComplementAudioAnalysis)
            StartCoroutine(ComplementAudioAnalysisWithGroq(generated));

        return generated;
    }

    public GeneratedTimbre BuildGeneratedTimbre(TimbreDescriptor descriptor, ADSRDescriptor adsr, string source, bool usedGroq, bool usedLocalDsp, string message)
    {
        descriptor = descriptor ?? new TimbreDescriptor();
        adsr = adsr ?? new ADSRDescriptor();

        ValidateDescriptor(descriptor);
        ValidateAdsr(adsr);
        TimbrePreset preset = BuildPresetFromDescriptor(descriptor, adsr, source);
        return BuildGeneratedTimbre(preset, descriptor, adsr, source, usedGroq, usedLocalDsp, message);
    }

    private GeneratedTimbre BuildGeneratedTimbre(TimbrePreset preset, TimbreDescriptor descriptor, ADSRDescriptor adsr, string source, bool usedGroq, bool usedLocalDsp, string message)
    {
        descriptor = descriptor ?? new TimbreDescriptor();
        adsr = adsr ?? new ADSRDescriptor();
        preset = preset ?? BuildPresetFromDescriptor(descriptor, adsr, source);
        preset.Validate();
        ValidateDescriptor(descriptor);
        ValidateAdsr(adsr);
        return new GeneratedTimbre
        {
            preset = preset,
            timbre = descriptor,
            adsr = adsr,
            wavetable = HarmonicUtils.BuildBandLimitedWavetable(preset.sustainHarmonics, TimbrePreset.MaxHarmonics, WavetableSize),
            source = source,
            usedGroq = usedGroq,
            usedLocalDsp = usedLocalDsp,
            message = message
        };
    }

    /// <summary>
    /// Convierte el descriptor antiguo de Groq en el preset multiframe compartido.
    /// Si Groq no entrega tres frames explícitos, se derivan variaciones simples para ataque y release.
    /// </summary>
    /// <param name="descriptor">Descriptor tímbrico validado.</param>
    /// <param name="adsr">ADSR validado en milisegundos heredados.</param>
    /// <param name="name">Nombre o fuente del preset.</param>
    /// <returns>Preset multiframe listo para Legacy, External DSP y WebGL.</returns>
    private static TimbrePreset BuildPresetFromDescriptor(TimbreDescriptor descriptor, ADSRDescriptor adsr, string name)
    {
        TimbrePreset preset = new TimbrePreset();
        preset.timbreName = string.IsNullOrWhiteSpace(name) ? "Groq Timbre" : name;
        preset.sustainHarmonics = HarmonicsToAmplitudes(descriptor != null ? descriptor.harmonics : null);
        preset.attackHarmonics = ShapeAttackHarmonics(preset.sustainHarmonics, descriptor != null ? descriptor.brightness : 0.35f);
        preset.releaseHarmonics = ShapeReleaseHarmonics(preset.sustainHarmonics, descriptor != null ? descriptor.warmth : 0.5f);
        preset.adsr = new AdsrSettings
        {
            attackSeconds = adsr.attackMs / 1000f,
            decaySeconds = adsr.decayMs / 1000f,
            sustainLevel = adsr.sustainLevel,
            releaseSeconds = adsr.releaseMs / 1000f,
            legacySustainSeconds = adsr.sustainMs / 1000f
        };
        preset.Validate();
        return preset;
    }

    private static float[] HarmonicsToAmplitudes(HarmonicDescriptor[] harmonics)
    {
        float[] amplitudes = new float[TimbrePreset.MaxHarmonics];

        if (harmonics == null || harmonics.Length == 0)
        {
            amplitudes[0] = 1f;
            return amplitudes;
        }

        for (int i = 0; i < harmonics.Length; i++)
        {
            if (harmonics[i] == null)
                continue;

            int index = Mathf.Clamp(harmonics[i].index, 1, TimbrePreset.MaxHarmonics) - 1;
            amplitudes[index] = Mathf.Max(amplitudes[index], Mathf.Clamp01(harmonics[i].amplitude));
        }

        return HarmonicUtils.ValidateHarmonicArray(amplitudes);
    }

    private static float[] ShapeAttackHarmonics(float[] sustain, float brightness)
    {
        float[] result = new float[TimbrePreset.MaxHarmonics];
        float bright = Mathf.Clamp01(brightness);

        for (int i = 0; i < result.Length; i++)
        {
            float lift = Mathf.Lerp(1.05f, 1.65f, bright) * Mathf.Lerp(1f, 1.35f, (float)i / Mathf.Max(1, result.Length - 1));
            result[i] = Mathf.Clamp01((sustain != null && i < sustain.Length ? sustain[i] : 0f) * lift);
        }

        result[0] = Mathf.Max(result[0], 1f);
        return HarmonicUtils.ValidateHarmonicArray(result);
    }

    private static float[] ShapeReleaseHarmonics(float[] sustain, float warmth)
    {
        float[] result = new float[TimbrePreset.MaxHarmonics];
        float darkening = Mathf.Lerp(1.2f, 2.2f, Mathf.Clamp01(warmth));

        for (int i = 0; i < result.Length; i++)
        {
            float harmonic = i + 1f;
            result[i] = Mathf.Clamp01((sustain != null && i < sustain.Length ? sustain[i] : 0f) / Mathf.Pow(harmonic, darkening * 0.18f));
        }

        result[0] = Mathf.Max(result[0], 0.8f);
        return HarmonicUtils.ValidateHarmonicArray(result);
    }

    public static float[] BuildWavetableFromDescriptor(TimbreDescriptor descriptor)
    {
        descriptor = descriptor ?? new TimbreDescriptor();
        ValidateDescriptor(descriptor);

        float[] wavetable = new float[WavetableSize];
        HarmonicDescriptor[] harmonics = descriptor.harmonics;

        for (int n = 0; n < WavetableSize; n++)
        {
            float phase01 = (float)n / WavetableSize;
            float value = 0f;

            for (int i = 0; i < harmonics.Length; i++)
            {
                HarmonicDescriptor harmonic = harmonics[i];
                int index = Mathf.Max(1, harmonic.index);
                float amplitude = Mathf.Clamp01(harmonic.amplitude);
                float phase = harmonic.phaseRadians;

                value += amplitude * Mathf.Sin((2f * Mathf.PI * index * phase01) + phase);
            }

            wavetable[n] = value;
        }

        NormalizeInPlace(wavetable);
        return wavetable;
    }

    private UnityWebRequest BuildGroqRequest(string prompt, string apiKey)
    {
        string body = BuildGroqRequestJson(prompt);
        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);

        UnityWebRequest request = new UnityWebRequest(GroqEndpoint, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bodyBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.Max(1, requestTimeoutSeconds);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        return request;
    }

    private string BuildGroqRequestJson(string prompt)
    {
        string systemPrompt =
            "You are helping a Unity procedural synthesizer create a wavetable and ADSR envelope. " +
            "Return only JSON that matches the provided schema. " +
            "Do not include Markdown. Do not add explanations before or after the JSON. " +
            "Do not generate audio. Do not generate individual samples. Do not return 2048 wavetable values. " +
            "Describe the sound using a short timbre name, three harmonic amplitude frames, timbre values, and ADSR values. " +
            "ADSR units must be milliseconds for attackMs, decayMs, sustainMs, releaseMs, and 0..1 for sustainLevel. " +
            "Each harmonic frame must contain 16 normalized amplitudes. Do not use phases for the multiframe output.";

        return "{"
            + "\"model\":\"" + JsonEscape(modelName) + "\","
            + "\"messages\":["
            + "{\"role\":\"system\",\"content\":\"" + JsonEscape(systemPrompt) + "\"},"
            + "{\"role\":\"user\",\"content\":\"" + JsonEscape(prompt) + "\"}"
            + "],"
            + "\"temperature\":0.35,"
            + "\"reasoning_effort\":\"low\","
            + "\"max_completion_tokens\":" + Mathf.Clamp(maxCompletionTokens, 128, 4096) + ","
            + "\"response_format\":{"
            + "\"type\":\"json_schema\","
            + "\"json_schema\":{"
            + "\"name\":\"procedural_timbre_descriptor\","
            + "\"schema\":" + BuildResponseSchemaJson()
            + "}"
            + "}"
            + "}";
    }

    private static string BuildResponseSchemaJson()
    {
        return "{"
            + "\"type\":\"object\","
            + "\"additionalProperties\":false,"
            + "\"properties\":{"
            + "\"timbreName\":{\"type\":\"string\",\"minLength\":1,\"maxLength\":48},"
            + "\"timbre\":{\"type\":\"object\",\"properties\":{"
            + "\"brightness\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1},"
            + "\"harmonicity\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1},"
            + "\"noisiness\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1},"
            + "\"warmth\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1},"
            + "\"fundamentalStrength\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1}"
            + "},\"required\":[\"brightness\",\"harmonicity\",\"noisiness\",\"warmth\",\"fundamentalStrength\"],\"additionalProperties\":false},"
            + "\"attackHarmonics\":{\"type\":\"array\",\"minItems\":16,\"maxItems\":16,\"items\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1}},"
            + "\"sustainHarmonics\":{\"type\":\"array\",\"minItems\":16,\"maxItems\":16,\"items\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1}},"
            + "\"releaseHarmonics\":{\"type\":\"array\",\"minItems\":16,\"maxItems\":16,\"items\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1}},"
            + "\"adsr\":{\"type\":\"object\",\"properties\":{"
            + "\"attackMs\":{\"type\":\"number\",\"minimum\":1,\"maximum\":400},"
            + "\"decayMs\":{\"type\":\"number\",\"minimum\":1,\"maximum\":1000},"
            + "\"sustainMs\":{\"type\":\"number\",\"minimum\":0,\"maximum\":5000},"
            + "\"sustainLevel\":{\"type\":\"number\",\"minimum\":0.001,\"maximum\":1},"
            + "\"releaseMs\":{\"type\":\"number\",\"minimum\":1,\"maximum\":1000}"
            + "},\"required\":[\"attackMs\",\"decayMs\",\"sustainMs\",\"sustainLevel\",\"releaseMs\"],\"additionalProperties\":false}"
            + "},"
            + "\"required\":[\"timbreName\",\"timbre\",\"attackHarmonics\",\"sustainHarmonics\",\"releaseHarmonics\",\"adsr\"]"
            + "}";
    }

    private bool TryParseGroqResponse(string responseJson, out TimbreDescriptor descriptor, out ADSRDescriptor adsr, out TimbrePreset preset, out string error)
    {
        descriptor = null;
        adsr = null;
        preset = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(responseJson))
        {
            error = "GroqCloud devolvió una respuesta vacía.";
            return false;
        }

        GroqChatCompletionResponse response;
        try
        {
            response = JsonUtility.FromJson<GroqChatCompletionResponse>(responseJson);
        }
        catch (Exception exception)
        {
            error = "No se pudo interpretar la respuesta HTTP de GroqCloud: " + exception.Message;
            return false;
        }

        if (response != null && response.error != null && !string.IsNullOrWhiteSpace(response.error.message))
        {
            error = "GroqCloud reportó error " + response.error.code + ": " + response.error.message;
            return false;
        }

        if (response == null || response.choices == null || response.choices.Length == 0)
        {
            error = "GroqCloud no devolvió choices en la respuesta.";
            return false;
        }

        GroqChoice choice = response.choices[0];
        string finishReason = choice != null ? choice.finish_reason : string.Empty;
        string structuredJson = choice != null && choice.message != null ? choice.message.content : string.Empty;

        if (string.Equals(finishReason, "length", StringComparison.OrdinalIgnoreCase))
        {
            error = "GroqCloud truncó la respuesta antes de completar el JSON. Aumenta maxCompletionTokens o vuelve a intentarlo.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(structuredJson))
        {
            error = "GroqCloud devolvió contenido vacío. Revisa maxCompletionTokens y el estado del modelo.";
            return false;
        }

        structuredJson = ExtractJsonObject(structuredJson);
        return TryParseStructuredTimbreJson(structuredJson, out descriptor, out adsr, out preset, out error);
    }

    private bool TryParseStructuredTimbreJson(string structuredJson, out TimbreDescriptor descriptor, out ADSRDescriptor adsr, out TimbrePreset preset, out string error)
    {
        descriptor = null;
        adsr = null;
        preset = null;
        error = string.Empty;

        if (!LooksLikeStructuredTimbreJson(structuredJson))
        {
            error = "GroqCloud no devolvió el contrato JSON esperado para timbre, armónicos y ADSR.";
            return false;
        }

        GroqStructuredTimbre structured;
        try
        {
            structured = JsonUtility.FromJson<GroqStructuredTimbre>(structuredJson);
        }
        catch (Exception exception)
        {
            error = "GroqCloud devolvió JSON inválido: " + exception.Message;
            return false;
        }

        if (structured == null)
        {
            error = "GroqCloud devolvió JSON vacío.";
            return false;
        }

        descriptor = structured.timbre ?? new TimbreDescriptor();
        if ((structured.sustainHarmonics == null || structured.sustainHarmonics.Length == 0) &&
            (structured.harmonics == null || structured.harmonics.Length == 0))
        {
            error = "GroqCloud devolvió un descriptor sin armónicos.";
            return false;
        }

        float[] sustainHarmonics = structured.sustainHarmonics != null && structured.sustainHarmonics.Length > 0
            ? HarmonicUtils.ValidateHarmonicArray(structured.sustainHarmonics)
            : HarmonicsToAmplitudes(structured.harmonics);

        descriptor.harmonics = AmplitudesToHarmonics(sustainHarmonics);
        adsr = structured.adsr ?? new ADSRDescriptor();

        ValidateDescriptor(descriptor);
        ValidateAdsr(adsr);
        preset = new TimbrePreset
        {
            timbreName = string.IsNullOrWhiteSpace(structured.timbreName) ? "Groq Timbre" : structured.timbreName.Trim(),
            attackHarmonics = structured.attackHarmonics != null && structured.attackHarmonics.Length > 0
                ? HarmonicUtils.ValidateHarmonicArray(structured.attackHarmonics)
                : ShapeAttackHarmonics(sustainHarmonics, descriptor.brightness),
            sustainHarmonics = sustainHarmonics,
            releaseHarmonics = structured.releaseHarmonics != null && structured.releaseHarmonics.Length > 0
                ? HarmonicUtils.ValidateHarmonicArray(structured.releaseHarmonics)
                : ShapeReleaseHarmonics(sustainHarmonics, descriptor.warmth),
            adsr = new AdsrSettings
            {
                attackSeconds = adsr.attackMs / 1000f,
                decaySeconds = adsr.decayMs / 1000f,
                sustainLevel = adsr.sustainLevel,
                releaseSeconds = adsr.releaseMs / 1000f,
                legacySustainSeconds = adsr.sustainMs / 1000f
            }
        };
        preset.Validate();
        return true;
    }

    private static bool LooksLikeStructuredTimbreJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        return json.Contains("\"timbre\"") &&
               (json.Contains("\"sustainHarmonics\"") || json.Contains("\"harmonics\"")) &&
               json.Contains("\"adsr\"");
    }

    private static HarmonicDescriptor[] AmplitudesToHarmonics(float[] amplitudes)
    {
        float[] safe = HarmonicUtils.ValidateHarmonicArray(amplitudes);
        HarmonicDescriptor[] harmonics = new HarmonicDescriptor[safe.Length];

        for (int i = 0; i < safe.Length; i++)
        {
            harmonics[i] = new HarmonicDescriptor
            {
                index = i + 1,
                amplitude = safe[i],
                phaseRadians = 0f
            };
        }

        return harmonics;
    }

    private static string ExtractJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');

        if (start >= 0 && end > start)
            return text.Substring(start, end - start + 1);

        return text;
    }

    private GeneratedTimbre BuildFallbackTimbre(string prompt, string message)
    {
        string lower = prompt.ToLowerInvariant();
        float brightness = lower.Contains("bright") || lower.Contains("metallic") ? 0.8f : 0.35f;
        float warmth = lower.Contains("warm") || lower.Contains("analog") ? 0.8f : 0.45f;
        float noisiness = lower.Contains("noise") || lower.Contains("distorted") ? 0.25f : 0.04f;
        float fundamental = lower.Contains("bass") || lower.Contains("fundamental") ? 1f : 0.75f;

        TimbreDescriptor descriptor = new TimbreDescriptor
        {
            brightness = brightness,
            harmonicity = 0.85f,
            noisiness = noisiness,
            warmth = warmth,
            fundamentalStrength = fundamental,
            harmonics = BuildDefaultHarmonics(brightness, warmth, fundamental)
        };

        ADSRDescriptor adsr = new ADSRDescriptor
        {
            attackMs = lower.Contains("slow attack") || lower.Contains("pad") ? 220f : 10f,
            decayMs = 180f,
            sustainMs = lower.Contains("pad") ? 1600f : 650f,
            sustainLevel = lower.Contains("pluck") ? 0.3f : 0.68f,
            releaseMs = lower.Contains("slow release") || lower.Contains("pad") ? 850f : 240f
        };

        return BuildGeneratedTimbre(descriptor, adsr, "PromptFallback", false, true, message);
    }

    private static HarmonicDescriptor[] BuildDefaultHarmonics(float brightness, float warmth, float fundamentalStrength)
    {
        int count = Mathf.RoundToInt(Mathf.Lerp(6f, 18f, Mathf.Clamp01(brightness)));
        HarmonicDescriptor[] harmonics = new HarmonicDescriptor[count];

        for (int i = 0; i < count; i++)
        {
            int harmonicIndex = i + 1;
            float slope = Mathf.Lerp(1.8f, 0.75f, Mathf.Clamp01(brightness));
            float amplitude = Mathf.Pow(1f / harmonicIndex, slope);

            if (harmonicIndex == 1)
                amplitude *= Mathf.Lerp(0.7f, 1.25f, Mathf.Clamp01(fundamentalStrength));

            if (warmth > 0.55f && harmonicIndex % 2 == 0)
                amplitude *= 0.65f;

            harmonics[i] = new HarmonicDescriptor
            {
                index = harmonicIndex,
                amplitude = Mathf.Clamp01(amplitude),
                phaseRadians = 0f
            };
        }

        return harmonics;
    }

    private bool TryAnalyzeAudioClip(AudioClip clip, out GeneratedTimbre generated, out string error)
    {
        generated = null;
        error = string.Empty;

        if (clip.samples <= 0 || clip.channels <= 0)
        {
            error = "El AudioClip no contiene muestras válidas.";
            return false;
        }

        float[] raw = new float[clip.samples * clip.channels];
        if (!clip.GetData(raw, 0))
        {
            error = "No se pudo leer el AudioClip. Revisa que el clip permita cargar sus datos.";
            return false;
        }

        float[] mono = ConvertToMonoAndNormalize(raw, clip.channels);
        if (mono.Length == 0)
        {
            error = "No se pudieron convertir muestras del AudioClip a mono.";
            return false;
        }

        float[] envelope = BuildSmoothedEnvelope(mono, clip.frequency);
        ADSRDescriptor adsr = EstimateAdsrFromEnvelope(envelope, clip.frequency);

        int period = EstimatePeriodByAutocorrelation(mono, envelope, clip.frequency);
        float[] wavetable = period > 0
            ? ExtractCycleAsWavetable(mono, envelope, period)
            : ResampleWindowAsWavetable(mono, FindStableStart(envelope));

        TimbreDescriptor descriptor = EstimateDescriptorFromAudio(wavetable, adsr);
        TimbrePreset preset = BuildPresetFromDescriptor(descriptor, adsr, "AudioClip");

        generated = new GeneratedTimbre
        {
            preset = preset,
            timbre = descriptor,
            adsr = adsr,
            wavetable = wavetable != null && wavetable.Length == WavetableSize
                ? wavetable
                : HarmonicUtils.BuildBandLimitedWavetable(preset.sustainHarmonics, TimbrePreset.MaxHarmonics, WavetableSize),
            source = "AudioClip",
            usedGroq = false,
            usedLocalDsp = true,
            message = "Generado desde AudioClip con DSP local."
        };

        return generated.IsUsable();
    }

    private static float[] ConvertToMonoAndNormalize(float[] raw, int channels)
    {
        int safeChannels = Mathf.Max(1, channels);
        int frames = raw.Length / safeChannels;
        float[] mono = new float[frames];
        float maxAbs = 0f;

        for (int frame = 0; frame < frames; frame++)
        {
            float sum = 0f;
            int baseIndex = frame * safeChannels;

            for (int channel = 0; channel < safeChannels; channel++)
                sum += raw[baseIndex + channel];

            float value = sum / safeChannels;
            mono[frame] = value;
            maxAbs = Mathf.Max(maxAbs, Mathf.Abs(value));
        }

        if (maxAbs > 0.000001f)
        {
            for (int i = 0; i < mono.Length; i++)
                mono[i] /= maxAbs;
        }

        return mono;
    }

    private static float[] BuildSmoothedEnvelope(float[] mono, int sampleRate)
    {
        float[] envelope = new float[mono.Length];
        int window = Mathf.Clamp(sampleRate / 200, 16, 512);
        float sum = 0f;

        for (int i = 0; i < mono.Length; i++)
        {
            sum += Mathf.Abs(mono[i]);

            if (i >= window)
                sum -= Mathf.Abs(mono[i - window]);

            int count = Mathf.Min(i + 1, window);
            envelope[i] = sum / Mathf.Max(1, count);
        }

        NormalizeInPlace(envelope, true);
        return envelope;
    }

    private static ADSRDescriptor EstimateAdsrFromEnvelope(float[] envelope, int sampleRate)
    {
        int peakIndex = 0;
        float peak = 0f;

        for (int i = 0; i < envelope.Length; i++)
        {
            if (envelope[i] > peak)
            {
                peak = envelope[i];
                peakIndex = i;
            }
        }

        if (peak <= 0.0001f)
            return new ADSRDescriptor();

        int onset = FindFirstAbove(envelope, peak * 0.08f, 0, envelope.Length - 1);
        int attackEnd = FindFirstAbove(envelope, peak * 0.9f, onset, Mathf.Max(onset, peakIndex));
        int end = FindLastAbove(envelope, peak * 0.08f);

        float sustainLevel = EstimateSustainLevel(envelope, peakIndex, end);
        int decayEnd = FindFirstBelow(envelope, sustainLevel + ((peak - sustainLevel) * 0.15f), peakIndex, end);
        int releaseStart = FindReleaseStart(envelope, sustainLevel, peakIndex, end);

        float attackMs = FramesToMs(Mathf.Max(1, attackEnd - onset), sampleRate);
        float decayMs = FramesToMs(Mathf.Max(1, decayEnd - attackEnd), sampleRate);
        float sustainMs = FramesToMs(Mathf.Max(0, releaseStart - decayEnd), sampleRate);
        float releaseMs = FramesToMs(Mathf.Max(1, end - releaseStart), sampleRate);

        ADSRDescriptor adsr = new ADSRDescriptor
        {
            attackMs = attackMs,
            decayMs = decayMs,
            sustainMs = sustainMs,
            sustainLevel = sustainLevel,
            releaseMs = releaseMs
        };

        ValidateAdsr(adsr);
        return adsr;
    }

    private static int EstimatePeriodByAutocorrelation(float[] mono, float[] envelope, int sampleRate)
    {
        int start = FindStableStart(envelope);
        int windowSize = Mathf.Min(4096, mono.Length - start);

        if (windowSize < 512)
            return -1;

        int minLag = Mathf.Max(1, sampleRate / 1200);
        int maxLag = Mathf.Min(sampleRate / 50, windowSize / 2);
        float bestScore = 0f;
        int bestLag = -1;

        for (int lag = minLag; lag <= maxLag; lag++)
        {
            float sum = 0f;
            float aEnergy = 0f;
            float bEnergy = 0f;
            int count = windowSize - lag;

            for (int i = 0; i < count; i++)
            {
                float a = mono[start + i];
                float b = mono[start + i + lag];
                sum += a * b;
                aEnergy += a * a;
                bEnergy += b * b;
            }

            float denom = Mathf.Sqrt(aEnergy * bEnergy);
            if (denom <= 0.000001f)
                continue;

            float score = sum / denom;
            if (score > bestScore)
            {
                bestScore = score;
                bestLag = lag;
            }
        }

        return bestScore >= 0.35f ? bestLag : -1;
    }

    private static float[] ExtractCycleAsWavetable(float[] mono, float[] envelope, int period)
    {
        int stableStart = FindStableStart(envelope);
        int start = FindNearestRisingZeroCrossing(mono, stableStart, Mathf.Min(mono.Length - 1, stableStart + period));
        int end = Mathf.Min(mono.Length - 1, start + Mathf.Max(1, period));

        if (end <= start + 1)
            return ResampleWindowAsWavetable(mono, stableStart);

        float[] wavetable = new float[WavetableSize];
        int length = end - start;

        for (int i = 0; i < WavetableSize; i++)
        {
            float readPosition = start + ((float)i / WavetableSize) * length;
            int i0 = Mathf.Clamp(Mathf.FloorToInt(readPosition), 0, mono.Length - 1);
            int i1 = Mathf.Clamp(i0 + 1, 0, mono.Length - 1);
            float frac = readPosition - i0;
            wavetable[i] = Mathf.Lerp(mono[i0], mono[i1], frac);
        }

        RemoveDcAndNormalize(wavetable);
        return wavetable;
    }

    private static float[] ResampleWindowAsWavetable(float[] mono, int start)
    {
        float[] wavetable = new float[WavetableSize];
        int safeStart = Mathf.Clamp(start, 0, Mathf.Max(0, mono.Length - 2));
        int available = Mathf.Max(1, mono.Length - safeStart - 1);
        int length = Mathf.Min(available, WavetableSize);

        for (int i = 0; i < WavetableSize; i++)
        {
            float readPosition = safeStart + ((float)i / WavetableSize) * length;
            int i0 = Mathf.Clamp(Mathf.FloorToInt(readPosition), 0, mono.Length - 1);
            int i1 = Mathf.Clamp(i0 + 1, 0, mono.Length - 1);
            float frac = readPosition - i0;
            wavetable[i] = Mathf.Lerp(mono[i0], mono[i1], frac);
        }

        RemoveDcAndNormalize(wavetable);
        return wavetable;
    }

    private static TimbreDescriptor EstimateDescriptorFromAudio(float[] wavetable, ADSRDescriptor adsr)
    {
        float roughness = 0f;
        for (int i = 1; i < wavetable.Length; i++)
            roughness += Mathf.Abs(wavetable[i] - wavetable[i - 1]);

        roughness /= Mathf.Max(1, wavetable.Length - 1);

        float brightness = Mathf.Clamp01(roughness * 2.2f);
        float warmth = Mathf.Clamp01(1f - (brightness * 0.7f));
        float attackFastness = 1f - Mathf.Clamp01(adsr.attackMs / 400f);

        return new TimbreDescriptor
        {
            brightness = brightness,
            harmonicity = 0.7f,
            noisiness = Mathf.Clamp01(roughness * 0.6f),
            warmth = warmth,
            fundamentalStrength = Mathf.Lerp(0.7f, 1f, attackFastness),
            harmonics = BuildDefaultHarmonics(brightness, warmth, 1f)
        };
    }

    private IEnumerator ComplementAudioAnalysisWithGroq(GeneratedTimbre generated)
    {
        if (generated == null)
            yield break;

        string prompt = "Analyze this locally extracted audio descriptor and return a compact timbre descriptor. "
            + "Do not ask for audio samples. Local DSP already produced the wavetable. "
            + "Use the ADSR and rough timbre values to refine harmonics only. "
            + "Local values: brightness=" + generated.timbre.brightness.ToString("0.###", CultureInfo.InvariantCulture)
            + ", noisiness=" + generated.timbre.noisiness.ToString("0.###", CultureInfo.InvariantCulture)
            + ", attackMs=" + generated.adsr.attackMs.ToString("0.###", CultureInfo.InvariantCulture)
            + ", releaseMs=" + generated.adsr.releaseMs.ToString("0.###", CultureInfo.InvariantCulture) + ".";

        yield return GenerateFromPromptCoroutine(prompt);
    }

    private void ApplyGeneratedTimbre(GeneratedTimbre generated, Action<GeneratedTimbre> onGenerated)
    {
        if (generated == null || !generated.IsUsable())
        {
            ReportFailure("El timbre generado no es utilizable.");
            return;
        }

        if (targetPolifonia == null)
            targetPolifonia = FindAnyObjectByType<Polifonia>();

        if (targetPolifonia != null)
        {
            targetPolifonia.ApplyGeneratedTimbre(generated);
        }
        else
        {
            if (targetOsc == null)
                targetOsc = FindAnyObjectByType<Osc>();

            if (targetOsc != null)
                targetOsc.ApplyGeneratedTimbre(generated);
        }

        lastValidTimbre = generated;
        onGenerated?.Invoke(generated);
        SetStatus(generated.message);

        if (logDetails)
            Debug.Log("[GroqTimbreGenerator] " + generated.message);
    }

    private void ReportFailure(string message)
    {
        SetStatus(message);
        Debug.LogWarning("[GroqTimbreGenerator] " + message);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.SetText(message);
    }

    private static string BuildHttpErrorMessage(UnityWebRequest request)
    {
        long code = request.responseCode;
        string prefix;

        if (code == 401)
            prefix = "GroqCloud rechazó la API key. Revisa que groqApiKey sea correcta.";
        else if (code == 403)
            prefix = "GroqCloud rechazó los permisos de la cuenta o del modelo.";
        else if (code == 429)
            prefix = "Se alcanzó el límite temporal de solicitudes de GroqCloud. Espera antes de volver a intentarlo.";
        else if (code == 400)
            prefix = "GroqCloud rechazó la solicitud. Revisa el modelo, el schema o los parámetros enviados.";
        else if (code == 404)
            prefix = "GroqCloud no encontró el modelo solicitado. Revisa modelName.";
        else if (code >= 500)
            prefix = "GroqCloud no está disponible temporalmente.";
        else if (request.result == UnityWebRequest.Result.ConnectionError)
            prefix = "No hay conexión con GroqCloud.";
        else if (request.result == UnityWebRequest.Result.ProtocolError)
            prefix = "GroqCloud devolvió un error HTTP.";
        else if (request.result == UnityWebRequest.Result.DataProcessingError)
            prefix = "No se pudo procesar la respuesta de GroqCloud.";
        else
            prefix = "La solicitud a GroqCloud falló.";

        string retryAfter = request.GetResponseHeader("Retry-After");
        if (!string.IsNullOrWhiteSpace(retryAfter))
            prefix += " Retry-After: " + retryAfter + " segundos.";

        return prefix + " HTTP " + code + ". " + request.error;
    }

    private static void ValidateDescriptor(TimbreDescriptor descriptor)
    {
        if (descriptor == null)
            return;

        descriptor.brightness = Mathf.Clamp01(descriptor.brightness);
        descriptor.harmonicity = Mathf.Clamp01(descriptor.harmonicity);
        descriptor.noisiness = Mathf.Clamp01(descriptor.noisiness);
        descriptor.warmth = Mathf.Clamp01(descriptor.warmth);
        descriptor.fundamentalStrength = Mathf.Clamp01(descriptor.fundamentalStrength);

        if (descriptor.harmonics == null || descriptor.harmonics.Length == 0)
            descriptor.harmonics = BuildDefaultHarmonics(descriptor.brightness, descriptor.warmth, descriptor.fundamentalStrength);

        int count = Mathf.Clamp(descriptor.harmonics.Length, 1, 64);
        if (count != descriptor.harmonics.Length)
            Array.Resize(ref descriptor.harmonics, count);

        for (int i = 0; i < descriptor.harmonics.Length; i++)
        {
            if (descriptor.harmonics[i] == null)
                descriptor.harmonics[i] = new HarmonicDescriptor { index = i + 1, amplitude = 0f, phaseRadians = 0f };

            descriptor.harmonics[i].index = Mathf.Clamp(descriptor.harmonics[i].index, 1, 128);
            descriptor.harmonics[i].amplitude = Mathf.Clamp01(descriptor.harmonics[i].amplitude);
            descriptor.harmonics[i].phaseRadians = Mathf.Clamp(descriptor.harmonics[i].phaseRadians, -Mathf.PI * 2f, Mathf.PI * 2f);
        }
    }

    private static void ValidateAdsr(ADSRDescriptor adsr)
    {
        if (adsr == null)
            return;

        adsr.attackMs = Mathf.Clamp(adsr.attackMs, 1f, 400f);
        adsr.decayMs = Mathf.Clamp(adsr.decayMs, 1f, 1000f);
        adsr.sustainMs = Mathf.Clamp(adsr.sustainMs, 0f, 5000f);
        adsr.sustainLevel = Mathf.Clamp(adsr.sustainLevel, 0.001f, 1f);
        adsr.releaseMs = Mathf.Clamp(adsr.releaseMs, 1f, 1000f);
    }

    private static void NormalizeInPlace(float[] data, bool positiveOnly = false)
    {
        if (data == null || data.Length == 0)
            return;

        float max = 0f;
        for (int i = 0; i < data.Length; i++)
            max = Mathf.Max(max, positiveOnly ? data[i] : Mathf.Abs(data[i]));

        if (max <= 0.000001f)
            return;

        for (int i = 0; i < data.Length; i++)
            data[i] = Mathf.Clamp(data[i] / max, positiveOnly ? 0f : -1f, 1f);
    }

    private static void RemoveDcAndNormalize(float[] data)
    {
        if (data == null || data.Length == 0)
            return;

        float mean = 0f;
        for (int i = 0; i < data.Length; i++)
            mean += data[i];

        mean /= data.Length;

        for (int i = 0; i < data.Length; i++)
            data[i] -= mean;

        NormalizeInPlace(data);
    }

    private static int FindStableStart(float[] envelope)
    {
        if (envelope == null || envelope.Length == 0)
            return 0;

        int peakIndex = 0;
        float peak = 0f;

        for (int i = 0; i < envelope.Length; i++)
        {
            if (envelope[i] > peak)
            {
                peak = envelope[i];
                peakIndex = i;
            }
        }

        return Mathf.Clamp(peakIndex + Mathf.Max(32, envelope.Length / 50), 0, Mathf.Max(0, envelope.Length - 2));
    }

    private static int FindNearestRisingZeroCrossing(float[] mono, int start, int end)
    {
        int safeStart = Mathf.Clamp(start, 1, Mathf.Max(1, mono.Length - 1));
        int safeEnd = Mathf.Clamp(end, safeStart, mono.Length - 1);

        for (int i = safeStart; i <= safeEnd; i++)
        {
            if (mono[i - 1] <= 0f && mono[i] > 0f)
                return i;
        }

        return safeStart;
    }

    private static int FindFirstAbove(float[] data, float threshold, int start, int end)
    {
        int safeStart = Mathf.Clamp(start, 0, Mathf.Max(0, data.Length - 1));
        int safeEnd = Mathf.Clamp(end, safeStart, Mathf.Max(0, data.Length - 1));

        for (int i = safeStart; i <= safeEnd; i++)
        {
            if (data[i] >= threshold)
                return i;
        }

        return safeStart;
    }

    private static int FindFirstBelow(float[] data, float threshold, int start, int end)
    {
        int safeStart = Mathf.Clamp(start, 0, Mathf.Max(0, data.Length - 1));
        int safeEnd = Mathf.Clamp(end, safeStart, Mathf.Max(0, data.Length - 1));

        for (int i = safeStart; i <= safeEnd; i++)
        {
            if (data[i] <= threshold)
                return i;
        }

        return safeEnd;
    }

    private static int FindLastAbove(float[] data, float threshold)
    {
        for (int i = data.Length - 1; i >= 0; i--)
        {
            if (data[i] >= threshold)
                return i;
        }

        return Mathf.Max(0, data.Length - 1);
    }

    private static float EstimateSustainLevel(float[] envelope, int peakIndex, int end)
    {
        int start = Mathf.Clamp(peakIndex + ((end - peakIndex) / 2), 0, Mathf.Max(0, envelope.Length - 1));
        int safeEnd = Mathf.Clamp(end, start, Mathf.Max(0, envelope.Length - 1));
        float sum = 0f;
        int count = 0;

        for (int i = start; i <= safeEnd; i++)
        {
            sum += envelope[i];
            count++;
        }

        if (count == 0)
            return 0.5f;

        return Mathf.Clamp(sum / count, 0.001f, 1f);
    }

    private static int FindReleaseStart(float[] envelope, float sustainLevel, int peakIndex, int end)
    {
        float threshold = Mathf.Max(0.02f, sustainLevel * 0.75f);

        for (int i = Mathf.Max(peakIndex, 0); i <= end; i++)
        {
            if (envelope[i] < threshold)
                return i;
        }

        return Mathf.Clamp(end - Mathf.Max(1, envelope.Length / 10), peakIndex, end);
    }

    private static float FramesToMs(int frames, int sampleRate)
    {
        return (frames / Mathf.Max(1f, sampleRate)) * 1000f;
    }

    private static string JsonEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}

