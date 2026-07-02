using System.Collections.Generic;
using UnityEngine;

public class ExpressiveProfileMapper
{
    public ExpressiveMusicProfile Map(ExpressiveMusicAttribute attribute, System.Random rng)
    {
        ExpressiveMusicProfile profile = new ExpressiveMusicProfile();
        profile.attribute = attribute;

        switch (attribute)
        {
            case ExpressiveMusicAttribute.Happy:
                Configure(profile, MusicalMode.Major, MusicalMeter.FourFour, 122, 0.72f, 0.68f, 69, 0.72f, 0.35f,
                    "I-V-vi-IV", new[] { ChordDegree.I, ChordDegree.V, ChordDegree.VI, ChordDegree.IV },
                    AccompanimentType.Pulsed, DrumPatternStyle.Active, Osc.WaveFormType.Sawtooth);
                profile.rootNote = Pick(rng, MusicalRootNote.C, MusicalRootNote.D, MusicalRootNote.G, MusicalRootNote.A);
                break;

            case ExpressiveMusicAttribute.Sad:
                Configure(profile, MusicalMode.Minor, MusicalMeter.FourFour, 76, 0.35f, 0.28f, 57, 0.18f, 0.25f,
                    "i-VI-III-VII", new[] { ChordDegree.I, ChordDegree.VI, ChordDegree.III, ChordDegree.VII },
                    AccompanimentType.Sustained, DrumPatternStyle.Minimal, Osc.WaveFormType.Triangle);
                profile.rootNote = Pick(rng, MusicalRootNote.A, MusicalRootNote.D, MusicalRootNote.E);
                break;

            case ExpressiveMusicAttribute.Mysterious:
                Configure(profile, MusicalMode.Minor, MusicalMeter.SixEight, 82, 0.32f, 0.34f, 55, 0.28f, 0.48f,
                    "i-iv-VII-III", new[] { ChordDegree.I, ChordDegree.IV, ChordDegree.VII, ChordDegree.III },
                    AccompanimentType.Sparse, DrumPatternStyle.DarkSparse, Osc.WaveFormType.Sine);
                profile.rootNote = Pick(rng, MusicalRootNote.D, MusicalRootNote.F, MusicalRootNote.GSharp);
                break;

            case ExpressiveMusicAttribute.Heroic:
                Configure(profile, MusicalMode.Major, MusicalMeter.FourFour, 104, 0.55f, 0.58f, 65, 0.82f, 0.42f,
                    "I-IV-V-I", new[] { ChordDegree.I, ChordDegree.IV, ChordDegree.V, ChordDegree.I },
                    AccompanimentType.Driving, DrumPatternStyle.March, Osc.WaveFormType.Sawtooth);
                profile.rootNote = Pick(rng, MusicalRootNote.C, MusicalRootNote.D, MusicalRootNote.G);
                profile.harmonyVoiceCount = 4;
                break;

            case ExpressiveMusicAttribute.Tender:
                Configure(profile, MusicalMode.Major, MusicalMeter.ThreeFour, 84, 0.36f, 0.26f, 64, 0.12f, 0.28f,
                    "I-vi-IV-V", new[] { ChordDegree.I, ChordDegree.VI, ChordDegree.IV, ChordDegree.V },
                    AccompanimentType.Arpeggiated, DrumPatternStyle.Minimal, Osc.WaveFormType.Sine);
                profile.rootNote = Pick(rng, MusicalRootNote.C, MusicalRootNote.F, MusicalRootNote.G);
                break;

            case ExpressiveMusicAttribute.Dark:
                Configure(profile, MusicalMode.Minor, MusicalMeter.FourFour, 72, 0.28f, 0.32f, 50, 0.24f, 0.34f,
                    "i-VII-VI-V", new[] { ChordDegree.I, ChordDegree.VII, ChordDegree.VI, ChordDegree.V },
                    AccompanimentType.Sparse, DrumPatternStyle.DarkSparse, Osc.WaveFormType.Square);
                profile.rootNote = Pick(rng, MusicalRootNote.C, MusicalRootNote.D, MusicalRootNote.FSharp);
                break;

            case ExpressiveMusicAttribute.Energetic:
                Configure(profile, MusicalMode.Major, MusicalMeter.FourFour, 146, 0.9f, 0.92f, 68, 0.95f, 0.55f,
                    "I-V-vi-IV", new[] { ChordDegree.I, ChordDegree.V, ChordDegree.VI, ChordDegree.IV },
                    AccompanimentType.Driving, DrumPatternStyle.Active, Osc.WaveFormType.Sawtooth);
                profile.rootNote = Pick(rng, MusicalRootNote.C, MusicalRootNote.E, MusicalRootNote.G, MusicalRootNote.A);
                profile.harmonyVoiceCount = 4;
                break;

            case ExpressiveMusicAttribute.Calm:
                Configure(profile, MusicalMode.Major, MusicalMeter.ThreeFour, 66, 0.22f, 0.18f, 60, 0.05f, 0.22f,
                    "I-IV-I-V", new[] { ChordDegree.I, ChordDegree.IV, ChordDegree.I, ChordDegree.V },
                    AccompanimentType.Sustained, DrumPatternStyle.Minimal, Osc.WaveFormType.Sine);
                profile.rootNote = Pick(rng, MusicalRootNote.C, MusicalRootNote.F, MusicalRootNote.G);
                break;

            case ExpressiveMusicAttribute.Threatening:
                Configure(profile, MusicalMode.Minor, MusicalMeter.FourFour, 88, 0.28f, 0.42f, 48, 0.55f, 0.5f,
                    "i-VII-i-V", new[] { ChordDegree.I, ChordDegree.VII, ChordDegree.I, ChordDegree.V },
                    AccompanimentType.Sparse, DrumPatternStyle.DarkSparse, Osc.WaveFormType.Square);
                profile.rootNote = Pick(rng, MusicalRootNote.C, MusicalRootNote.D, MusicalRootNote.F);
                break;

            case ExpressiveMusicAttribute.Magical:
                Configure(profile, MusicalMode.Major, MusicalMeter.SixEight, 96, 0.48f, 0.5f, 72, 0.22f, 0.62f,
                    "I-III-IV-V", new[] { ChordDegree.I, ChordDegree.III, ChordDegree.IV, ChordDegree.V },
                    AccompanimentType.Arpeggiated, DrumPatternStyle.SixEight, Osc.WaveFormType.Triangle);
                profile.rootNote = Pick(rng, MusicalRootNote.C, MusicalRootNote.FSharp, MusicalRootNote.G);
                profile.harmonyVoiceCount = 4;
                break;

            case ExpressiveMusicAttribute.Childlike:
                Configure(profile, MusicalMode.Major, MusicalMeter.FourFour, 118, 0.64f, 0.56f, 74, 0.42f, 0.5f,
                    "I-IV-V-I", new[] { ChordDegree.I, ChordDegree.IV, ChordDegree.V, ChordDegree.I },
                    AccompanimentType.Pulsed, DrumPatternStyle.Basic, Osc.WaveFormType.Square);
                profile.rootNote = Pick(rng, MusicalRootNote.C, MusicalRootNote.G, MusicalRootNote.A);
                break;

            case ExpressiveMusicAttribute.Epic:
                Configure(profile, MusicalMode.Major, MusicalMeter.FourFour, 112, 0.58f, 0.7f, 67, 0.95f, 0.58f,
                    "I-V-IV-V", new[] { ChordDegree.I, ChordDegree.V, ChordDegree.IV, ChordDegree.V },
                    AccompanimentType.Driving, DrumPatternStyle.March, Osc.WaveFormType.Sawtooth);
                profile.rootNote = Pick(rng, MusicalRootNote.C, MusicalRootNote.D, MusicalRootNote.G);
                profile.harmonyVoiceCount = 4;
                break;
        }

        profile.bpm = Mathf.Clamp(profile.bpm + rng.Next(-5, 6), 48, 180);
        ConfigureADSR(profile);
        return profile;
    }

    private static void Configure(
        ExpressiveMusicProfile profile,
        MusicalMode mode,
        MusicalMeter meter,
        int bpm,
        float melodicDensity,
        float rhythmicDensity,
        int registerCenter,
        float percussionIntensity,
        float variationAmount,
        string progressionName,
        ChordDegree[] progression,
        AccompanimentType accompanimentType,
        DrumPatternStyle drumPatternStyle,
        Osc.WaveFormType preferredWaveform)
    {
        profile.mode = mode;
        profile.meter = meter;
        profile.bpm = bpm;
        profile.melodicDensity = Mathf.Clamp01(melodicDensity);
        profile.rhythmicDensity = Mathf.Clamp01(rhythmicDensity);
        profile.registerCenter = registerCenter;
        profile.percussionIntensity = Mathf.Clamp01(percussionIntensity);
        profile.variationAmount = Mathf.Clamp01(variationAmount);
        profile.progressionName = progressionName;
        profile.progressionDegrees = new List<ChordDegree>(progression);
        profile.accompanimentType = accompanimentType;
        profile.drumPatternStyle = drumPatternStyle;
        profile.preferredWaveform = preferredWaveform;
        profile.melodyWaveform = preferredWaveform;
        profile.harmonyWaveform = preferredWaveform == Osc.WaveFormType.Sine ? Osc.WaveFormType.Triangle : preferredWaveform;
        profile.bassWaveform = mode == MusicalMode.Minor ? Osc.WaveFormType.Square : Osc.WaveFormType.Sawtooth;
    }

    private static MusicalRootNote Pick(System.Random rng, params MusicalRootNote[] options)
    {
        if (options == null || options.Length == 0)
            return MusicalRootNote.C;

        return options[rng.Next(0, options.Length)];
    }

    private static void ConfigureADSR(ExpressiveMusicProfile profile)
    {
        EnsureADSR(profile);

        switch (profile.attribute)
        {
            case ExpressiveMusicAttribute.Happy:
                profile.melodyADSR.Set(8f, 65f, 220f, 0.55f, 70f, true);
                profile.bassADSR.Set(5f, 80f, 180f, 0.72f, 60f, true);
                profile.harmonyADSR.Set(18f, 130f, 420f, 0.46f, 120f, true);
                break;

            case ExpressiveMusicAttribute.Sad:
                profile.melodyADSR.Set(55f, 180f, 520f, 0.42f, 260f, true);
                profile.bassADSR.Set(28f, 170f, 520f, 0.62f, 190f, true);
                profile.harmonyADSR.Set(120f, 320f, 1100f, 0.44f, 420f, true);
                break;

            case ExpressiveMusicAttribute.Mysterious:
                profile.melodyADSR.Set(38f, 150f, 460f, 0.36f, 240f, true);
                profile.bassADSR.Set(18f, 140f, 420f, 0.6f, 180f, true);
                profile.harmonyADSR.Set(100f, 280f, 1000f, 0.38f, 380f, true);
                break;

            case ExpressiveMusicAttribute.Heroic:
                profile.melodyADSR.Set(14f, 95f, 280f, 0.64f, 105f, true);
                profile.bassADSR.Set(5f, 75f, 240f, 0.82f, 75f, true);
                profile.harmonyADSR.Set(28f, 150f, 560f, 0.58f, 170f, true);
                break;

            case ExpressiveMusicAttribute.Tender:
                profile.melodyADSR.Set(70f, 190f, 560f, 0.5f, 300f, true);
                profile.bassADSR.Set(35f, 170f, 500f, 0.56f, 220f, true);
                profile.harmonyADSR.Set(140f, 330f, 1200f, 0.48f, 450f, true);
                break;

            case ExpressiveMusicAttribute.Dark:
                profile.melodyADSR.Set(36f, 155f, 430f, 0.34f, 230f, true);
                profile.bassADSR.Set(10f, 130f, 440f, 0.68f, 170f, true);
                profile.harmonyADSR.Set(90f, 280f, 1000f, 0.36f, 380f, true);
                break;

            case ExpressiveMusicAttribute.Energetic:
                profile.melodyADSR.Set(5f, 45f, 150f, 0.5f, 45f, true);
                profile.bassADSR.Set(4f, 55f, 140f, 0.78f, 45f, true);
                profile.harmonyADSR.Set(8f, 95f, 260f, 0.45f, 80f, true);
                break;

            case ExpressiveMusicAttribute.Calm:
                profile.melodyADSR.Set(85f, 220f, 650f, 0.52f, 360f, true);
                profile.bassADSR.Set(45f, 190f, 620f, 0.55f, 260f, true);
                profile.harmonyADSR.Set(160f, 360f, 1500f, 0.5f, 520f, true);
                break;

            case ExpressiveMusicAttribute.Threatening:
                profile.melodyADSR.Set(24f, 140f, 380f, 0.32f, 190f, true);
                profile.bassADSR.Set(5f, 115f, 380f, 0.75f, 150f, true);
                profile.harmonyADSR.Set(70f, 250f, 900f, 0.34f, 340f, true);
                break;

            case ExpressiveMusicAttribute.Magical:
                profile.melodyADSR.Set(30f, 130f, 420f, 0.56f, 260f, true);
                profile.bassADSR.Set(20f, 145f, 420f, 0.58f, 180f, true);
                profile.harmonyADSR.Set(110f, 300f, 1200f, 0.52f, 460f, true);
                break;

            case ExpressiveMusicAttribute.Childlike:
                profile.melodyADSR.Set(5f, 55f, 170f, 0.5f, 55f, true);
                profile.bassADSR.Set(5f, 70f, 160f, 0.68f, 55f, true);
                profile.harmonyADSR.Set(12f, 110f, 300f, 0.44f, 90f, true);
                break;

            case ExpressiveMusicAttribute.Epic:
                profile.melodyADSR.Set(18f, 110f, 330f, 0.66f, 130f, true);
                profile.bassADSR.Set(5f, 85f, 260f, 0.85f, 90f, true);
                profile.harmonyADSR.Set(55f, 210f, 760f, 0.62f, 260f, true);
                break;
        }
    }

    private static void EnsureADSR(ExpressiveMusicProfile profile)
    {
        if (profile.melodyADSR == null)
            profile.melodyADSR = InstrumentADSRSettings.CreateMelodyDefault();
        if (profile.bassADSR == null)
            profile.bassADSR = InstrumentADSRSettings.CreateBassDefault();
        if (profile.harmonyADSR == null)
            profile.harmonyADSR = InstrumentADSRSettings.CreateHarmonyDefault();
    }
}
