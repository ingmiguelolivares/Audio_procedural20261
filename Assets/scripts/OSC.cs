using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OSC : MonoBehaviour
{
    float FM = 44100;

    [Range(20, 20000)]
    public float frecuencia = 440;

    //para el LFO de amplitud
    [Range(0, 10)]
    public float FOscAmp = 5;


    //para el LFO de frecuencia vibrato
    [Range(0, 8)]
    public float FOscF = 1.5f;

    [Range(0.1f, 16)]
    public float FreqAlt = 1.5f;

    
    [Range(0.1f, 4)]
    public float W = 1f;

    [Range(0, 7)]
    public int Octava = 3;

    [Range(0, 7)]
    public int Narmonicos = 10;


    public AudioSource Aud;

    [Range(0f, 1f)]
    public float OutputLevel = 1f;

    [HideInInspector]
    [Range(-1200f, 1200f)]
    public float DetuneCents = 0f;

    [Range(0, 4096)]
    public int detuneFrames = 0;

    [HideInInspector]
    public bool UseFilterModulation = false;

    [HideInInspector]
    [Range(0f, 1f)]
    public float FilterLevel = 0f;

    [HideInInspector]
    public bool UseDistortion = false;

    [HideInInspector]
    [Range(0f, 1f)]
    public float DistortionAmount = 0.35f;

    public enum WaveformType
    {
        Sine,
        Square,
        Sawtooth,
        Triangle,
        SA,
        WhiteNoise
    }

    public WaveformType waveformType = WaveformType.Sine;

    public Slider OctaveSl, Waveformsl, NarmonicosSl, GenTypeSl;

    public TextMeshProUGUI OctaveValue, WaveformValue;

    // Start is called before the first frame update
    void Start()
    {
        //updateADSR();
        //Octava = (int)OctaveSl.value;
        //GenerateWaveTable();
        
    }

    int t = 0;
    int Em = 0;
    // Update is called once per frame
    void Update()
    {
        
        /*if (Input.GetKeyDown(KeyCode.A))
            {
            frecuencia = 16.3516f * Mathf.Pow(2, Octava);
            Aud.Play(); 
            }
        else if (Input.GetKeyUp(KeyCode.A)) Aud.Stop();



        if (Input.GetKeyDown(KeyCode.W))
        {
            frecuencia = 17.3239f * Mathf.Pow(2, Octava);
            Aud.Play();

        }
        else if (Input.GetKeyUp(KeyCode.W)) Aud.Stop();

        if (Input.GetKeyDown(KeyCode.S))
        {
            frecuencia = 18.3540f * Mathf.Pow(2, Octava);
            Aud.Play();

        }
        else if (Input.GetKeyUp(KeyCode.S)) Aud.Stop();

        if (Input.GetKeyDown(KeyCode.E))
        {
            frecuencia = 19.4454f * Mathf.Pow(2, Octava);
            Aud.Play();

        }
        else if (Input.GetKeyUp(KeyCode.E)) Aud.Stop();

        if (Input.GetKeyDown(KeyCode.D))
        {
            frecuencia = 20.6017f * Mathf.Pow(2, Octava);
            Aud.Play();

        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            frecuencia = 400;
            if (!Aud.isPlaying)
            {
                TimeIndex = 0;
                Aud.Play();
                waveformType = WaveformType.Sine;
            }
            else Aud.Stop();

        }
        else if (Input.GetKeyUp(KeyCode.D)) Aud.Stop();
        */
        
    }

    //funciones para controlar parámetros desde los sliders en el canvas
    public void OctaveSelector() {
        Octava = (int)OctaveSl.value;
        OctaveValue.SetText(Octava.ToString());
    }


    public void WaveFormSelector()
    {
        if (Waveformsl.value == 0) { waveformType = WaveformType.Sine; WaveformValue.SetText("Seno"); }
        else if (Waveformsl.value == 1) { waveformType = WaveformType.Square; WaveformValue.SetText("Square"); }
        else if (Waveformsl.value == 2) { waveformType = WaveformType.Triangle; WaveformValue.SetText("Triangle"); }
        else if (Waveformsl.value == 3) { waveformType = WaveformType.Sawtooth; WaveformValue.SetText("Sawtooth"); }
        else if (Waveformsl.value == 4) { waveformType = WaveformType.SA; WaveformValue.SetText("Sintesis Aditiva"); }
    }

    //funciones para definir las acciones de activación y desactivación de nota
    public void KeyboardDown(string Note) {
        if (Note == "C" || Note == "c") frecuencia = 16.3516f * Mathf.Pow(2, Octava);
        else if (Note == "C#" || Note == "c#") frecuencia = 17.3239f * Mathf.Pow(2, Octava);
        else if (Note == "D" || Note == "d") frecuencia = 18.3540f * Mathf.Pow(2, Octava);
        else if (Note == "D#" || Note == "d#") frecuencia = 19.4454f * Mathf.Pow(2, Octava);
        else if (Note == "E" || Note == "e") frecuencia = 20.6017f * Mathf.Pow(2, Octava);
        else if (Note == "F" || Note == "f") frecuencia = 21.8268f * Mathf.Pow(2, Octava);
        else if (Note == "F#" || Note == "f#") frecuencia = 23.1246f * Mathf.Pow(2, Octava);
        else if (Note == "G" || Note == "g") frecuencia = 24.4997f * Mathf.Pow(2, Octava);
        else if (Note == "G#" || Note == "g#") frecuencia = 25.9565f * Mathf.Pow(2, Octava);
        else if (Note == "A" || Note == "a") frecuencia = 27.5000f * Mathf.Pow(2, Octava);
        else if (Note == "A#" || Note == "a#") frecuencia = 29.1353f * Mathf.Pow(2, Octava);
        else if (Note == "B" || Note == "b") frecuencia = 30.8677f * Mathf.Pow(2, Octava);
        else if (Note == "C2" || Note == "c2") frecuencia = 2 * 16.3516f * Mathf.Pow(2, Octava);

        Aud.Play();
        AOscIndex = 0;
        FOscIndex = 0;
        FEnvIndex = 0;
        ADSRIndex = 0;
        releaseTriggered = false;
        releaseStartFrame = 0;
        releaseStartLevel = 0f;

    }

    public void KeyboardUp()
    {
        if (releaseTriggered)
            return;

        releaseStartFrame = ADSRIndex;
        releaseStartLevel = GetScheduledAdsrValue(ADSRIndex);
        releaseTriggered = true;

    }

    //funciones para generar las diferentes formas de onda

    float SineWave(float f, int t)
    {
        return Mathf.Sin(2 * Mathf.PI * f * t / FM);
    }

    float SineWaveSA(float f, int t, float A, int n)
    {
        return Mathf.Sin(2 * Mathf.PI * n*f * t / FM)*A;
    }

    float SquareWave(float f, int t)
    {
        return Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t / FM));
    }

    /*float TriangleWave( float frecuencia, int timeIndex)
    {
        //para obtener el número de muestras por periodo y la posición del mismo
        //al momento de generar
        var T = FM / frecuencia;
        var t = timeIndex % T;
        //para obtener los datos para recrear las rectas
        float m1 = 1 / (T / 4.0f);
        float m2 = -1 / (T / 4.0f);
        float m3 = 1 / (T / 4.0f);

        float b1 = 1 - (m1 * (T / 4.0f));
        float b2 = 1 - (m2 * (T / 4.0f));
        float b3 = 0 - (m3 * T);
        //para calcular los valores
        if (t<= (T / 4.0f)) return ((m1 * t) + b1);
        else if (t > (T / 4.0f) && t <= (T * (3 / 4f))) return ((m2 * t) + b2);
        else return ((m3 * t) + b3);
    }*/
    float TriangleWave(float frecuencia, int timeIndex)
    {
        // Calculamos el número de muestras por período
        var T = FM / frecuencia;
        var t = timeIndex % T; // Posición dentro del ciclo

        // Definir los puntos clave del período de la onda triangular
        float t1 = 0;
        float t2 = T / 4.0f;
        float t3 = 3 * T / 4.0f;
        float t4 = T;

        // Definir los valores de la onda en esos puntos
        float x1 = 0f, x2 = 1f, x3 = -1f, x4 = 0f;

        // Interpolar entre los segmentos
        if (t <= t2) 
            return LinearInterpolation(t, t1, t2, x1, x2);  // Subida
        else if (t <= t3) 
            return LinearInterpolation(t, t2, t3, x2, x3);  // Bajada
        else 
            return LinearInterpolation(t, t3, t4, x3, x4);  // Subida final
    }

    float LinearInterpolation(float x, float a1, float a2, float x1, float x2)
    {
        if (a1 == a2) return x1; // Evita división por cero
        return x1 + (x2 - x1) * (x - a1) / (a2 - a1);
    }

   /* float SawtoothWave(float frecuencia, int timeIndex)
    {
        //para obtener el número de muestras por periodo y la posición del mismo
        //al momento de generar
        var T = FM / frecuencia;
        var t = timeIndex % T;
        float m = 1 / (T / 2.0f);
        //para obtener los datos para recrear las rectas
        float b1 = 1 - (m * (T / 2.0f));
        float b2 = 0 - (m * T);
        //para calcular los valores
        if (t <= (T / 2.0f)) return ((m * t) + b1);
        else return ((m * t) + b2);
    }*/

    float SawtoothWave(float frecuencia, int timeIndex)
    {
        // Número de muestras por período
        var T = FM / frecuencia;
        var t = timeIndex % T; // Posición dentro del ciclo

        // Definir los puntos clave del período de la onda diente de sierra
        float t1 = 0;
        float t2 = T;
        
        // Valores de la onda en esos puntos
        float x1 = 1f, x2 = -1f;

        // Interpolar en todo el ciclo
        return LinearInterpolation(t, t1, t2, x1, x2);
    }

    private static System.Random rng = new System.Random();
    float WhiteNoise(int i) {
        return (float)rng.NextDouble() * 2f - 1f;
    }

    [Range(0,1f)]
    public float[] Amplitudes = { 1f, 0.7f, 0.8f, 0.6f, 0.5f, 0.4f, 0.5f, 0.6f, 0.7f, 0.4f };

    float SA(float f, int t, int Armonicos)
    {
        float x = 0f;
        for (int n = 1; n <= Armonicos; n++) {
            var A = Amplitudes[n - 1];
            x += SineWaveSA(f, t, A, n);
        }
        return x/Armonicos;
    }

    //para definir las funciones de onda y usarlas con wavetables
    float[] wavetable;
    int wavetableSize = 2048;
    public void GenerateWaveTable() {
        wavetable = new float[wavetableSize];
        float f = FM / wavetableSize;
        for (int i = 0; i < wavetableSize; i++)
        {
            switch (waveformType)
            {
                case WaveformType.Sine:
                    wavetable[i] += SineWave(f, i);
                    break;
                case WaveformType.Square:
                    wavetable[i] += SquareWave(f, i);
                    break;
                case WaveformType.Triangle:
                    wavetable[i] += TriangleWave(f, i);
                    break;
                case WaveformType.Sawtooth:
                    wavetable[i] += SawtoothWave(f, i);
                    break;
                case (WaveformType.SA):
                    wavetable[i] += SA(frecuencia, TimeIndex, Narmonicos);
                    break;

            }


        }
    }


    
    //funciones para definir la carga y actualización del ADSR
    [Range(5,400)]
    public float A = 5;

    [Range(10, 1000)]
    public float D = 10;

    [Range(100, 5000)]
    public float S = 100;

    [Range(0.001f, 1f)]
    public float SLevel = 0.7f;

    [Range(100, 1000)]
    public float R = 100;

    public bool AttackUsesLogCurve = false;
    public bool DecayUsesLogCurve = false;
    public bool SustainUsesLogCurve = false;
    public bool ReleaseUsesLogCurve = false;

    float[] env;

    /*public void updateADSR() {
        env = GetADSR();
    }*/


 /*   float[] GetADSR() {

        int ASamples = (int)(FM * (A / 1000));
        int DSamples = (int)(FM * (D / 1000));
        int SSamples = (int)(FM * (S / 1000));
        int RSamples = (int)(FM * (R / 1000));
        int TotalADSRSize = ASamples + DSamples + SSamples + RSamples;

        float[] envelope = new float[TotalADSRSize];

        for (int i = 0; i < TotalADSRSize; i++) {
            float value = 0f;

            if (i < ASamples) value = Mathf.Lerp(0f, 1f, (float)i / ASamples);
            else if (i < (ASamples+DSamples)) value = Mathf.Lerp(1f, SLevel, (float)i / DSamples);
            else if (i < (ASamples + DSamples+SSamples)) value = SLevel;
            else if (i < (ASamples + DSamples + SSamples + RSamples)) value = Mathf.Lerp(SLevel, 0.00001f, (float)i / RSamples);
            envelope[i] = value;
        }
        return envelope;
    }*/

    private bool releaseTriggered = false;
    private int releaseStartFrame = 0;
    private float releaseStartLevel = 0f;

    float GetADSRValue(int frameIndex)
    {
        if (releaseTriggered && frameIndex >= releaseStartFrame)
        {
            int releaseFrames = Mathf.Max(1, Mathf.RoundToInt((R / 1000f) * FM));
            float progress = (float)(frameIndex - releaseStartFrame) / releaseFrames;
            return Mathf.Lerp(releaseStartLevel, 0.00001f, EvaluateAdsrCurve01(progress, ReleaseUsesLogCurve));
        }

        return GetScheduledAdsrValue(frameIndex);
    }

    float GetScheduledAdsrValue(int frameIndex)
    {
        // Convertir tiempos de milisegundos a número de cuadros (frames)
        int attackFrames = Mathf.RoundToInt((A / 1000f) * FM);
        int decayFrames = attackFrames + Mathf.RoundToInt((D / 1000f) * FM);
        int sustainFrames = decayFrames + Mathf.RoundToInt((S / 1000f) * FM);
        int releaseFrames = sustainFrames + Mathf.RoundToInt((R / 1000f) * FM);

        // Evaluar en qué fase está el frame dado
        if (frameIndex < attackFrames)
        {
            // Fase de ataque (0 a 1)
            return EvaluateAdsrCurve01((float)frameIndex / Mathf.Max(1, attackFrames), AttackUsesLogCurve);
        }
        else if (frameIndex < decayFrames)
        {
            // Fase de decaimiento (1 a SLevel)
            return Mathf.Lerp(1f, SLevel, EvaluateAdsrCurve01((float)(frameIndex - attackFrames) / Mathf.Max(1, decayFrames - attackFrames), DecayUsesLogCurve));
        }
        else if (frameIndex < sustainFrames)
        {
            // Fase de sostenimiento (valor constante)
            return SLevel;
        }
        else if (frameIndex < releaseFrames)
        {
            // Fase de liberación (SLevel a 0.00001)
            return Mathf.Lerp(SLevel, 0.00001f, EvaluateAdsrCurve01((float)(frameIndex - sustainFrames) / Mathf.Max(1, releaseFrames - sustainFrames), ReleaseUsesLogCurve));
        }
        else
        {
            // Después del release, el valor es casi cero
            return 0.00001f;
        }
    }

    float EvaluateAdsrCurve01(float progress, bool useLogCurve)
    {
        progress = Mathf.Clamp01(progress);
        if (!useLogCurve) return progress;
        return Mathf.Log10(1f + (9f * progress));
    }


    [Range(5, 400)]
    public float AF = 5;

    [Range(10, 1000)]
    public float DF = 10;

    [Range(0.1f, 1)]
    public float LowEnvAF = 1;

    [Range(1, 3)]
    public float HighEnvDF = 1;

    float GetFADSR(int i)
    {

        int ASamples = (int)(FM * (AF / 1000));
        int DSamples = (int)(FM * (DF / 1000));
       
        
        float value = 0f;

        if (i < ASamples) value = Mathf.Lerp(LowEnvAF, HighEnvDF, (float)i / ASamples);
        else if (i < (ASamples + DSamples)) value = Mathf.Lerp(HighEnvDF, 1f, (float)i / DSamples);
        else value = 1f;
            
       
        return value;
    }


    //función para actualización de valores en tiempo real
    float X = 0;
    int TimeIndex = 0;
    public int ADSRIndex = 0;
    int AOscIndex = 0;
    int FOscIndex = 0;
    int WhiteIndex = 0;
    int FEnvIndex = 0;
    float Phase;
    float E, Y, Z, envA;
    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            

            /*if (ADSRIndex < env.Length) E = env[ADSRIndex];
            else E = 0.000001f;*/

            E = GetADSRValue(ADSRIndex) * OutputLevel;
            
            


            
            Y = (SineWave(FOscAmp, TimeIndex) / 2) + 0.5f;

          
            Z = SineWave(FOscF, TimeIndex);

            envA = GetFADSR(TimeIndex);
            float sample = GenerateCurrentSample(TimeIndex);

            if (detuneFrames > 0 && waveformType != WaveformType.WhiteNoise)
                sample = (sample + GenerateCurrentSample(TimeIndex + detuneFrames)) * 0.5f;

            sample *= E;

            //Z = 1;
            //W = 0;
            //Z = 1;
            //if ((int)GenTypeSl.value == 0)
            //{
                data[i] = sample;
                if (channels == 2) data[i + 1] = sample;

                TimeIndex++;
                ADSRIndex++;

                int naturalReleaseEnd = Mathf.Max(1, Mathf.RoundToInt((A / 1000f) * FM))
                    + Mathf.RoundToInt((D / 1000f) * FM)
                    + Mathf.RoundToInt((S / 1000f) * FM)
                    + Mathf.RoundToInt((R / 1000f) * FM);
                int releaseFrames = Mathf.Max(1, Mathf.RoundToInt((R / 1000f) * FM));
                bool finished = releaseTriggered
                    ? (ADSRIndex - releaseStartFrame) >= releaseFrames
                    : ADSRIndex >= naturalReleaseEnd;

                if (finished)
                {
                    if (i + channels < data.Length)
                        System.Array.Clear(data, i + channels, data.Length - (i + channels));

                    frecuencia = 0.1f;
                    break;
                }
                //FOscIndex++;
                //AOscIndex++;
                //FEnvIndex++;
            }
            
            
        
    }

    float GenerateCurrentSample(int timeIndex)
    {
        switch (waveformType)
        {
            case (WaveformType.Sine):
                X = SineWave(frecuencia + Z * envA, timeIndex);
                return X * Y;

            case (WaveformType.Square):
                X = SquareWave((frecuencia + Z) * W * envA, timeIndex);
                return X * Y;

            case (WaveformType.Triangle):
                X = TriangleWave((frecuencia + Z) * W * envA, timeIndex);
                return X * Y;

            case (WaveformType.Sawtooth):
                X = SawtoothWave((frecuencia + Z) * W * envA, timeIndex);
                return X * Y;

            case (WaveformType.SA):
                X = SA((frecuencia + Z) * W * envA, timeIndex, Narmonicos);
                return X * Y;

            case (WaveformType.WhiteNoise):
                X = WhiteNoise(WhiteIndex);
                return X;
        }

        return 0f;
    }

}
