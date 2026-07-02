using System.Collections.Generic;
using UnityEngine;

public class MelodyGenerator
{
    public void Generate(SongData song, System.Random rng)
    {
        if (song == null || song.profile == null)
            return;

        ExpressiveMusicProfile profile = song.profile;
        int minMidi = Mathf.Clamp(profile.registerCenter - 12, 36, 96);
        int maxMidi = Mathf.Clamp(profile.registerCenter + 12, minMidi + 1, 108);
        List<int> scaleNotes = ScaleBuilder.BuildScaleMidiRange(profile.rootNote, profile.mode, minMidi, maxMidi);

        if (scaleNotes.Count == 0)
            return;

        int previousMidi = ScaleBuilder.GetMidiForPitchClassNear(ScaleBuilder.RootToPitchClass(profile.rootNote), profile.registerCenter);

        for (int s = 0; s < song.sections.Count; s++)
        {
            SectionData section = song.sections[s];
            int notesPerBar = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 6f, profile.melodicDensity)), 1, 6);

            if (profile.attribute == ExpressiveMusicAttribute.Calm || profile.attribute == ExpressiveMusicAttribute.Sad)
                notesPerBar = Mathf.Min(notesPerBar, 3);

            if (profile.attribute == ExpressiveMusicAttribute.Energetic || profile.attribute == ExpressiveMusicAttribute.Childlike)
                notesPerBar = Mathf.Max(notesPerBar, 4);

            for (int c = 0; c < section.chordEvents.Count; c++)
            {
                ChordEvent chord = section.chordEvents[c];
                float step = section.beatsPerBar / notesPerBar;

                for (int n = 0; n < notesPerBar; n++)
                {
                    float start = chord.startBeat + n * step;
                    float beatInBar = n * step;
                    bool strongBeat = MusicalMeterUtility.IsStrongBeat(profile.meter, beatInBar);
                    bool finalNote = s == song.sections.Count - 1 && c == section.chordEvents.Count - 1 && n == notesPerBar - 1;

                    int midi = finalNote
                        ? ScaleBuilder.GetMidiForPitchClassNear(ScaleBuilder.RootToPitchClass(profile.rootNote), profile.registerCenter)
                        : PickMelodyMidi(chord, scaleNotes, previousMidi, strongBeat, rng);

                    if (Mathf.Abs(midi - previousMidi) > 9)
                        midi = MoveToward(previousMidi, midi, 7);

                    float duration = Mathf.Max(0.15f, step * 0.82f);
                    float velocity = strongBeat ? 0.88f : 0.68f;
                    section.noteEvents.Add(NoteEvent.FromMidi(TrackRole.Melody, midi, start, duration, velocity));
                    previousMidi = midi;
                }
            }
        }

        song.RebuildFlatEventLists();
    }

    private int PickMelodyMidi(ChordEvent chord, List<int> scaleNotes, int previousMidi, bool strongBeat, System.Random rng)
    {
        List<int> candidates = new List<int>();

        if (strongBeat)
        {
            for (int i = 0; i < chord.chordMidiNotes.Count; i++)
            {
                int pitchClass = ScaleBuilder.Mod12(chord.chordMidiNotes[i]);
                candidates.Add(ScaleBuilder.GetMidiForPitchClassNear(pitchClass, previousMidi));
                candidates.Add(ScaleBuilder.GetMidiForPitchClassNear(pitchClass, previousMidi + 5));
                candidates.Add(ScaleBuilder.GetMidiForPitchClassNear(pitchClass, previousMidi - 5));
            }
        }
        else
        {
            for (int i = 0; i < scaleNotes.Count; i++)
            {
                if (Mathf.Abs(scaleNotes[i] - previousMidi) <= 5)
                    candidates.Add(scaleNotes[i]);
            }
        }

        if (candidates.Count == 0)
            candidates.Add(scaleNotes[rng.Next(0, scaleNotes.Count)]);

        candidates.Sort((a, b) => Mathf.Abs(a - previousMidi).CompareTo(Mathf.Abs(b - previousMidi)));
        int limit = Mathf.Min(4, candidates.Count);
        return candidates[rng.Next(0, limit)];
    }

    private int MoveToward(int from, int to, int maxStep)
    {
        if (to > from)
            return from + maxStep;
        return from - maxStep;
    }
}
