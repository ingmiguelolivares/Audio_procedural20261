using System;
using System.Collections.Generic;

[Serializable]
public class ExpressiveMusicProfile
{
    public ExpressiveMusicAttribute attribute;
    public MusicalRootNote rootNote = MusicalRootNote.C;
    public MusicalMode mode = MusicalMode.Major;
    public MusicalMeter meter = MusicalMeter.FourFour;
    public int bpm = 100;
    public string progressionName = "I-IV-V-I";
    public List<ChordDegree> progressionDegrees = new List<ChordDegree>();

    [UnityEngine.Range(0f, 1f)] public float melodicDensity = 0.5f;
    [UnityEngine.Range(0f, 1f)] public float rhythmicDensity = 0.5f;
    public int registerCenter = 64;
    [UnityEngine.Range(0f, 1f)] public float percussionIntensity = 0.5f;
    [UnityEngine.Range(0f, 1f)] public float variationAmount = 0.35f;

    public AccompanimentType accompanimentType = AccompanimentType.Sustained;
    public DrumPatternStyle drumPatternStyle = DrumPatternStyle.Basic;
    public int harmonyVoiceCount = 3;

    public Osc.WaveFormType preferredWaveform = Osc.WaveFormType.Sine;
    public Osc.WaveFormType melodyWaveform = Osc.WaveFormType.Sine;
    public Osc.WaveFormType bassWaveform = Osc.WaveFormType.Square;
    public Osc.WaveFormType harmonyWaveform = Osc.WaveFormType.Sawtooth;
    public Osc.WaveFormType kickWaveform = Osc.WaveFormType.Sine;
    public Osc.WaveFormType snareWaveform = Osc.WaveFormType.WhiteNoise;
    public Osc.WaveFormType hiHatWaveform = Osc.WaveFormType.WhiteNoise;

    public InstrumentADSRSettings melodyADSR = InstrumentADSRSettings.CreateMelodyDefault();
    public InstrumentADSRSettings bassADSR = InstrumentADSRSettings.CreateBassDefault();
    public InstrumentADSRSettings harmonyADSR = InstrumentADSRSettings.CreateHarmonyDefault();

    public ExpressiveMusicProfile()
    {
        progressionDegrees.Add(ChordDegree.I);
        progressionDegrees.Add(ChordDegree.IV);
        progressionDegrees.Add(ChordDegree.V);
        progressionDegrees.Add(ChordDegree.I);
    }
}

[Serializable]
public class InstrumentADSRSettings
{
    [UnityEngine.Range(1f, 400f)] public float attackMs = 10f;
    [UnityEngine.Range(1f, 1000f)] public float decayMs = 90f;
    [UnityEngine.Range(0f, 5000f)] public float sustainMs = 250f;
    [UnityEngine.Range(0.001f, 1f)] public float sustainLevel = 0.55f;
    [UnityEngine.Range(1f, 1000f)] public float releaseMs = 90f;
    public bool fitSustainToNoteDuration = true;

    public void Set(float attack, float decay, float sustain, float level, float release, bool fitToDuration)
    {
        attackMs = attack;
        decayMs = decay;
        sustainMs = sustain;
        sustainLevel = level;
        releaseMs = release;
        fitSustainToNoteDuration = fitToDuration;
    }

    public static InstrumentADSRSettings CreateMelodyDefault()
    {
        InstrumentADSRSettings settings = new InstrumentADSRSettings();
        settings.Set(12f, 90f, 260f, 0.58f, 110f, true);
        return settings;
    }

    public static InstrumentADSRSettings CreateBassDefault()
    {
        InstrumentADSRSettings settings = new InstrumentADSRSettings();
        settings.Set(6f, 95f, 220f, 0.72f, 80f, true);
        return settings;
    }

    public static InstrumentADSRSettings CreateHarmonyDefault()
    {
        InstrumentADSRSettings settings = new InstrumentADSRSettings();
        settings.Set(45f, 180f, 600f, 0.5f, 220f, true);
        return settings;
    }
}
