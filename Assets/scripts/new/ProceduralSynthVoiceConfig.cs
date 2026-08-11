using UnityEngine;

[System.Serializable]
public class ProceduralSynthVoiceConfig
{
    public int sampleRate = 44100;
    public int channels = 2;
    public int waveform;
    public int harmonicCount;
    public bool useWavetable;
    public bool useAudioClipADSR;
    public float level;
    public float detuneCents;
    public float tremLfoFrequency;
    public float vibLfoFrequency;
    public float vibratoDepth;
    public float attackMs;
    public float decayMs;
    public float sustainMs;
    public float sustainLevel;
    public float releaseMs;
    public bool attackUsesLogCurve;
    public bool decayUsesLogCurve;
    public bool sustainUsesLogCurve;
    public bool releaseUsesLogCurve;
    public float fmMacroAmount;
    public float fmMinRatio;
    public float fmMaxRatio;
    public float fmMaxIndex;
    public float fmHighHarmonicBlend;
    public float fmModFrequency;
    public float fmModIndex;
    public float samplingBaseFrequency;
    public int samplingStartFrame;
    public int samplingEndFrame;
    public int samplingChannels;
    public int samplingTotalFrames;
    public float[] amplitudes = new float[0];
    public float[] wavetable = new float[0];
    public float[] samplingData = new float[0];
    public float[] audioClipAdsrData = new float[0];
}
