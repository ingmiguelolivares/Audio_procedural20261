using System.Collections.Generic;
using UnityEngine;

public static class ScaleBuilder
{
    private static readonly string[] NoteNames =
    {
        "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
    };

    private static readonly int[] MajorIntervals = { 0, 2, 4, 5, 7, 9, 11 };
    private static readonly int[] MinorIntervals = { 0, 2, 3, 5, 7, 8, 10 };

    public static int RootToPitchClass(MusicalRootNote root)
    {
        switch (root)
        {
            case MusicalRootNote.CSharp: return 1;
            case MusicalRootNote.D: return 2;
            case MusicalRootNote.DSharp: return 3;
            case MusicalRootNote.E: return 4;
            case MusicalRootNote.F: return 5;
            case MusicalRootNote.FSharp: return 6;
            case MusicalRootNote.G: return 7;
            case MusicalRootNote.GSharp: return 8;
            case MusicalRootNote.A: return 9;
            case MusicalRootNote.ASharp: return 10;
            case MusicalRootNote.B: return 11;
            default: return 0;
        }
    }

    public static MusicalRootNote PitchClassToRoot(int pitchClass)
    {
        pitchClass = Mod12(pitchClass);
        return (MusicalRootNote)pitchClass;
    }

    public static List<int> BuildScale(MusicalRootNote root, MusicalMode mode)
    {
        int rootPc = RootToPitchClass(root);
        int[] intervals = mode == MusicalMode.Minor ? MinorIntervals : MajorIntervals;
        List<int> scale = new List<int>();

        for (int i = 0; i < intervals.Length; i++)
            scale.Add(Mod12(rootPc + intervals[i]));

        return scale;
    }

    public static int DegreeToIndex(ChordDegree degree)
    {
        switch (degree)
        {
            case ChordDegree.II: return 1;
            case ChordDegree.III: return 2;
            case ChordDegree.IV: return 3;
            case ChordDegree.V: return 4;
            case ChordDegree.VI: return 5;
            case ChordDegree.VII: return 6;
            default: return 0;
        }
    }

    public static string GetPitchClassName(int pitchClass)
    {
        return NoteNames[Mod12(pitchClass)];
    }

    public static string GetNoteNameFromMidi(int midiNote)
    {
        string noteName;
        int octave;
        GetNoteParts(midiNote, out noteName, out octave);
        return noteName + octave;
    }

    public static void GetNoteParts(int midiNote, out string noteName, out int octave)
    {
        int pitchClass = Mod12(midiNote);
        noteName = NoteNames[pitchClass];
        octave = (midiNote / 12) - 1;
    }

    public static int GetMidiForPitchClassNear(int pitchClass, int targetMidi)
    {
        pitchClass = Mod12(pitchClass);
        int octave = Mathf.Clamp((targetMidi / 12) - 1, 0, 8);
        int midi = (octave + 1) * 12 + pitchClass;

        while (midi - targetMidi > 6)
            midi -= 12;

        while (targetMidi - midi > 6)
            midi += 12;

        return Mathf.Clamp(midi, 0, 127);
    }

    public static int NextMidiAtOrAbove(int referenceMidi, int pitchClass)
    {
        int midi = GetMidiForPitchClassNear(pitchClass, referenceMidi);
        while (midi < referenceMidi)
            midi += 12;

        return Mathf.Clamp(midi, 0, 127);
    }

    public static ChordEvent BuildChordEvent(
        ExpressiveMusicProfile profile,
        ChordDegree degree,
        string sectionName,
        int barIndex,
        float startBeat,
        float durationBeats,
        int chordOctave)
    {
        List<int> scale = BuildScale(profile.rootNote, profile.mode);
        int degreeIndex = DegreeToIndex(degree);
        int rootPc = scale[degreeIndex];
        int thirdPc = scale[(degreeIndex + 2) % 7];
        int fifthPc = scale[(degreeIndex + 4) % 7];

        bool useMajorDominant = profile.mode == MusicalMode.Minor && degree == ChordDegree.V;
        if (useMajorDominant)
            thirdPc = Mod12(rootPc + 4);

        int rootMidi = (chordOctave + 1) * 12 + rootPc;
        int thirdMidi = NextMidiAtOrAbove(rootMidi + 1, thirdPc);
        int fifthMidi = NextMidiAtOrAbove(thirdMidi + 1, fifthPc);

        ChordQuality quality = DetermineQuality(rootMidi, thirdMidi, fifthMidi);
        string chordName = BuildChordName(rootPc, quality);

        ChordEvent chordEvent = new ChordEvent
        {
            sectionName = sectionName,
            barIndex = barIndex,
            startBeat = startBeat,
            durationBeats = durationBeats,
            degree = degree,
            chordRoot = PitchClassToRoot(rootPc),
            quality = quality,
            chordName = chordName
        };

        chordEvent.chordMidiNotes.Add(rootMidi);
        chordEvent.chordMidiNotes.Add(thirdMidi);
        chordEvent.chordMidiNotes.Add(fifthMidi);

        for (int i = 0; i < chordEvent.chordMidiNotes.Count; i++)
            chordEvent.chordNoteNames.Add(GetNoteNameFromMidi(chordEvent.chordMidiNotes[i]));

        return chordEvent;
    }

    public static List<int> BuildScaleMidiRange(MusicalRootNote root, MusicalMode mode, int minMidi, int maxMidi)
    {
        List<int> pitchClasses = BuildScale(root, mode);
        List<int> result = new List<int>();

        for (int midi = Mathf.Max(0, minMidi); midi <= Mathf.Min(127, maxMidi); midi++)
        {
            if (pitchClasses.Contains(Mod12(midi)))
                result.Add(midi);
        }

        return result;
    }

    public static int Mod12(int value)
    {
        int result = value % 12;
        return result < 0 ? result + 12 : result;
    }

    private static ChordQuality DetermineQuality(int rootMidi, int thirdMidi, int fifthMidi)
    {
        int third = Mod12(thirdMidi - rootMidi);
        int fifth = Mod12(fifthMidi - rootMidi);

        if (third == 4 && fifth == 7)
            return ChordQuality.Major;
        if (third == 3 && fifth == 7)
            return ChordQuality.Minor;
        if (third == 3 && fifth == 6)
            return ChordQuality.Diminished;

        return ChordQuality.Suspended;
    }

    private static string BuildChordName(int rootPitchClass, ChordQuality quality)
    {
        string name = GetPitchClassName(rootPitchClass);

        switch (quality)
        {
            case ChordQuality.Minor:
                return name + "m";
            case ChordQuality.Diminished:
                return name + "dim";
            case ChordQuality.Suspended:
                return name + "sus";
            default:
                return name;
        }
    }
}
