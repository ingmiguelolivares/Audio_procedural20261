using System.Collections.Generic;
using UnityEngine;

public class HarmonyGenerator
{
    public void Generate(SongData song)
    {
        if (song == null || song.profile == null)
            return;

        ExpressiveMusicProfile profile = song.profile;
        int voices = Mathf.Clamp(profile.harmonyVoiceCount, 3, 4);
        int[] previous = { 48, 55, 60, 64 };

        for (int s = 0; s < song.sections.Count; s++)
        {
            SectionData section = song.sections[s];

            for (int c = 0; c < section.chordEvents.Count; c++)
            {
                ChordEvent chord = section.chordEvents[c];
                List<int> voiced = VoiceChord(chord, voices, previous);

                for (int v = 0; v < voices; v++)
                {
                    TrackRole role = VoiceIndexToRole(v);
                    section.noteEvents.Add(NoteEvent.FromMidi(role, voiced[v], chord.startBeat, chord.durationBeats * 0.95f, 0.42f));
                    previous[v] = voiced[v];
                }
            }
        }

        song.RebuildFlatEventLists();
    }

    private List<int> VoiceChord(ChordEvent chord, int voices, int[] previous)
    {
        List<int> pitchClasses = new List<int>();
        for (int i = 0; i < chord.chordMidiNotes.Count; i++)
            pitchClasses.Add(ScaleBuilder.Mod12(chord.chordMidiNotes[i]));

        if (voices == 4)
            pitchClasses.Add(ScaleBuilder.Mod12(chord.chordMidiNotes[0]));

        List<int> result = new List<int>();

        for (int i = 0; i < voices; i++)
        {
            int target = previous[i];
            int midi = ScaleBuilder.GetMidiForPitchClassNear(pitchClasses[i], target);

            if (i > 0)
            {
                while (midi <= result[i - 1])
                    midi += 12;
            }

            result.Add(Mathf.Clamp(midi, 36, 84));
        }

        return result;
    }

    private TrackRole VoiceIndexToRole(int index)
    {
        switch (index)
        {
            case 0: return TrackRole.Harmony1;
            case 1: return TrackRole.Harmony2;
            case 2: return TrackRole.Harmony3;
            default: return TrackRole.Harmony4;
        }
    }
}
