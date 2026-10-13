using System;
using UnityEngine;

/// <summary>
/// Proyecto: Síntesis de Audio Procedural con Inteligencia Artificial.
/// Desarrollado por: Miguel Ángel Olivares Tenorio.
/// Descripción: Modelo compartido de timbre usado por Legacy, External DSP y WebGL.
/// </summary>
[Serializable]
public class TimbrePreset
{
    public const int MaxHarmonics = 16;
    public const int DefaultWavetableSize = 2048;

    public string timbreName = "Generated Timbre";
    public float[] attackHarmonics = new float[MaxHarmonics];
    public float[] sustainHarmonics = new float[MaxHarmonics];
    public float[] releaseHarmonics = new float[MaxHarmonics];
    public AdsrSettings adsr = new AdsrSettings();

    /// <summary>
    /// Crea un preset básico seguro para probar el sintetizador sin conexión a Groq.
    /// </summary>
    /// <returns>Preset local con fundamental fuerte y pocos armónicos altos.</returns>
    public static TimbrePreset CreateLocalDefault()
    {
        TimbrePreset preset = new TimbrePreset();
        preset.timbreName = "Preset local";

        for (int i = 0; i < MaxHarmonics; i++)
        {
            int harmonic = i + 1;
            float baseAmplitude = 1f / harmonic;
            preset.attackHarmonics[i] = Mathf.Clamp01(baseAmplitude * (harmonic <= 8 ? 1.15f : 0.35f));
            preset.sustainHarmonics[i] = Mathf.Clamp01(Mathf.Pow(1f / harmonic, 1.35f));
            preset.releaseHarmonics[i] = Mathf.Clamp01(Mathf.Pow(1f / harmonic, 1.8f) * 0.75f);
        }

        preset.adsr = new AdsrSettings
        {
            attackSeconds = 0.01f,
            decaySeconds = 0.18f,
            sustainLevel = 0.68f,
            releaseSeconds = 0.32f,
            legacySustainSeconds = 0.65f
        };

        preset.Validate();
        return preset;
    }

    /// <summary>
    /// Garantiza arreglos de 16 armónicos y rangos seguros para todos los motores.
    /// </summary>
    public void Validate()
    {
        attackHarmonics = HarmonicUtils.ValidateHarmonicArray(attackHarmonics);
        sustainHarmonics = HarmonicUtils.ValidateHarmonicArray(sustainHarmonics);
        releaseHarmonics = HarmonicUtils.ValidateHarmonicArray(releaseHarmonics);

        if (adsr == null)
            adsr = new AdsrSettings();

        adsr.Validate();

        if (string.IsNullOrWhiteSpace(timbreName))
            timbreName = "Generated Timbre";
    }
}

/// <summary>
/// Proyecto: Síntesis de Audio Procedural con Inteligencia Artificial.
/// Desarrollado por: Miguel Ángel Olivares Tenorio.
/// Descripción: Parámetros ADSR compartidos expresados en segundos y nivel normalizado.
/// </summary>
[Serializable]
public class AdsrSettings
{
    public float attackSeconds = 0.01f;
    public float decaySeconds = 0.18f;
    public float sustainLevel = 0.65f;
    public float releaseSeconds = 0.3f;
    public float legacySustainSeconds = 0.65f;

    /// <summary>
    /// Normaliza los tiempos y niveles para evitar valores inválidos en los motores de audio.
    /// </summary>
    public void Validate()
    {
        attackSeconds = Mathf.Clamp(Safe(attackSeconds, 0.01f), 0.001f, 4f);
        decaySeconds = Mathf.Clamp(Safe(decaySeconds, 0.18f), 0.001f, 6f);
        sustainLevel = Mathf.Clamp(Safe(sustainLevel, 0.65f), 0.001f, 1f);
        releaseSeconds = Mathf.Clamp(Safe(releaseSeconds, 0.3f), 0.001f, 8f);
        legacySustainSeconds = Mathf.Clamp(Safe(legacySustainSeconds, 0.65f), 0f, 20f);
    }

    /// <summary>
    /// Convierte segundos a milisegundos para los campos heredados ya serializados en el proyecto.
    /// </summary>
    /// <param name="seconds">Tiempo en segundos.</param>
    /// <returns>Tiempo equivalente en milisegundos.</returns>
    public static float SecondsToMs(float seconds)
    {
        return Mathf.Max(0f, seconds * 1000f);
    }

    private static float Safe(float value, float fallback)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
    }
}

/// <summary>
/// Proyecto: Síntesis de Audio Procedural con Inteligencia Artificial.
/// Desarrollado por: Miguel Ángel Olivares Tenorio.
/// Descripción: Utilidades DSP compartidas para armónicos, Nyquist y wavetables.
/// </summary>
public static class HarmonicUtils
{
    /// <summary>
    /// Valida un arreglo de amplitudes armónicas y lo ajusta a 16 elementos.
    /// </summary>
    /// <param name="source">Arreglo de amplitudes normalizadas.</param>
    /// <returns>Arreglo seguro de 16 amplitudes en rango 0 a 1.</returns>
    public static float[] ValidateHarmonicArray(float[] source)
    {
        float[] result = new float[TimbrePreset.MaxHarmonics];

        if (source == null || source.Length == 0)
        {
            result[0] = 1f;
            return result;
        }

        int count = Mathf.Min(source.Length, result.Length);
        for (int i = 0; i < count; i++)
        {
            float value = source[i];
            result[i] = float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp01(value);
        }

        if (result[0] <= 0.0001f)
            result[0] = 1f;

        return result;
    }

    /// <summary>
    /// Calcula cuántos armónicos puede usar una nota antes de superar Nyquist.
    /// </summary>
    /// <param name="fundamentalFrequency">Frecuencia fundamental de la nota.</param>
    /// <param name="sampleRate">Frecuencia de muestreo del motor.</param>
    /// <returns>Cantidad máxima de armónicos permitidos, limitada a 16.</returns>
    public static int GetAllowedHarmonicCount(float fundamentalFrequency, float sampleRate)
    {
        if (fundamentalFrequency <= 0f || sampleRate <= 0f)
            return 0;

        float nyquist = sampleRate * 0.5f;
        return Mathf.Clamp(Mathf.FloorToInt(nyquist / fundamentalFrequency), 0, TimbrePreset.MaxHarmonics);
    }

    /// <summary>
    /// Interpola entre dos frames de amplitudes armónicas.
    /// </summary>
    /// <param name="from">Frame inicial.</param>
    /// <param name="to">Frame final.</param>
    /// <param name="amount">Progreso entre 0 y 1.</param>
    /// <param name="destination">Arreglo destino reutilizable para evitar asignaciones en audio.</param>
    public static void LerpHarmonics(float[] from, float[] to, float amount, float[] destination)
    {
        if (destination == null || destination.Length < TimbrePreset.MaxHarmonics)
            return;

        float t = Mathf.Clamp01(amount);
        for (int i = 0; i < TimbrePreset.MaxHarmonics; i++)
        {
            float a = from != null && i < from.Length ? from[i] : 0f;
            float b = to != null && i < to.Length ? to[i] : 0f;
            destination[i] = Mathf.Lerp(a, b, t);
        }
    }

    /// <summary>
    /// Genera una muestra mediante síntesis aditiva con límite de Nyquist.
    /// </summary>
    /// <param name="harmonics">Amplitudes armónicas del frame actual.</param>
    /// <param name="frequency">Frecuencia fundamental.</param>
    /// <param name="timeIndex">Índice de muestra de la voz.</param>
    /// <param name="sampleRate">Frecuencia de muestreo.</param>
    /// <returns>Muestra normalizada entre -1 y 1.</returns>
    public static float AdditiveSample(float[] harmonics, float frequency, int timeIndex, float sampleRate)
    {
        int allowed = GetAllowedHarmonicCount(frequency, sampleRate);
        if (allowed <= 0 || harmonics == null || harmonics.Length == 0)
            return 0f;

        int limit = Mathf.Min(allowed, harmonics.Length, TimbrePreset.MaxHarmonics);
        float sum = 0f;
        float norm = 0f;

        for (int i = 0; i < limit; i++)
        {
            float amplitude = Mathf.Clamp01(harmonics[i]);
            if (amplitude <= 0f)
                continue;

            int harmonic = i + 1;
            sum += Mathf.Sin(2f * Mathf.PI * harmonic * frequency * timeIndex / sampleRate) * amplitude;
            norm += amplitude;
        }

        return norm > 0f ? Mathf.Clamp(sum / norm, -1f, 1f) : 0f;
    }

    /// <summary>
    /// Construye una wavetable limitada por cantidad máxima de armónicos.
    /// </summary>
    /// <param name="harmonics">Amplitudes del frame.</param>
    /// <param name="maxHarmonics">Cantidad máxima de armónicos incluidos.</param>
    /// <param name="tableSize">Tamaño de la tabla.</param>
    /// <returns>Tabla normalizada para lectura por fase.</returns>
    public static float[] BuildBandLimitedWavetable(float[] harmonics, int maxHarmonics, int tableSize = TimbrePreset.DefaultWavetableSize)
    {
        int safeSize = Mathf.Max(32, tableSize);
        float[] table = new float[safeSize];
        int limit = Mathf.Clamp(maxHarmonics, 1, TimbrePreset.MaxHarmonics);

        for (int n = 0; n < safeSize; n++)
        {
            float phase01 = (float)n / safeSize;
            float value = 0f;
            float norm = 0f;

            for (int i = 0; i < limit; i++)
            {
                float amplitude = harmonics != null && i < harmonics.Length ? Mathf.Clamp01(harmonics[i]) : 0f;
                if (amplitude <= 0f)
                    continue;

                int harmonic = i + 1;
                value += Mathf.Sin(2f * Mathf.PI * harmonic * phase01) * amplitude;
                norm += amplitude;
            }

            table[n] = norm > 0f ? value / norm : 0f;
        }

        return table;
    }
}
