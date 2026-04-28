using UnityEngine;
using System.Globalization;
using System.IO;

/// <summary>
/// Lee un archivo de texto con muestras separadas por comas desde:
/// Assets/Wavetables/wavetable1.txt
///
/// También permite asignar el archivo como TextAsset por inspector,
/// lo cual suele ser más cómodo en builds.
/// </summary>
public class WavetableTxtLoader : MonoBehaviour
{
    [Header("Opción recomendada: asignar el txt como TextAsset desde el Inspector")]
    public TextAsset wavetableTextAsset;

    [Header("Ruta relativa dentro de Assets si no se usa TextAsset")]
    public string relativePathInsideAssets = "Wavetables/wavetable1.txt";

    [Header("Datos cargados")]
    public float[] samples;

    [Header("Opciones")]
    public bool loadOnStart = true;
    public bool logResult = true;

    private void Start()
    {
        if (loadOnStart)
        {
            LoadWavetable();
        }
    }

    [ContextMenu("Load Wavetable")]
    public void LoadWavetable()
    {
        string rawText = string.Empty;

        // Opción 1: leer desde TextAsset asignado en inspector
        if (wavetableTextAsset != null)
        {
            rawText = wavetableTextAsset.text;
        }
        else
        {
            // Opción 2: leer directamente desde Assets usando ruta absoluta
            string fullPath = Path.Combine(Application.dataPath, relativePathInsideAssets);

            if (!File.Exists(fullPath))
            {
                Debug.LogError("No se encontró el archivo wavetable en: " + fullPath);
                samples = null;
                return;
            }

            rawText = File.ReadAllText(fullPath);
        }

        ParseSamples(rawText);
    }

    private void ParseSamples(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            Debug.LogError("El archivo wavetable está vacío.");
            samples = null;
            return;
        }

        string cleaned = rawText.Replace("\n", "").Replace("\r", "").Trim();
        string[] parts = cleaned.Split(',');

        samples = new float[parts.Length];
        int validCount = 0;

        for (int i = 0; i < parts.Length; i++)
        {
            string token = parts[i].Trim();

            if (string.IsNullOrEmpty(token))
                continue;

            if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                samples[validCount] = Mathf.Clamp(value, -1f, 1f);
                validCount++;
            }
            else
            {
                Debug.LogWarning("No se pudo convertir el valor en el índice " + i + ": " + token);
            }
        }

        if (validCount != samples.Length)
        {
            float[] resized = new float[validCount];
            for (int i = 0; i < validCount; i++)
            {
                resized[i] = samples[i];
            }
            samples = resized;
        }

        if (logResult)
        {
            Debug.Log("Wavetable cargada correctamente. Número de muestras: " + samples.Length);
        }
    }

    public float GetSample(int index)
    {
        if (samples == null || samples.Length == 0)
            return 0f;

        index = Mathf.Clamp(index, 0, samples.Length - 1);
        return samples[index];
    }

    public float GetSampleNormalized(float phase01)
    {
        if (samples == null || samples.Length == 0)
            return 0f;

        phase01 = Mathf.Repeat(phase01, 1f);
        int index = Mathf.FloorToInt(phase01 * samples.Length) % samples.Length;
        return samples[index];
    }
}
