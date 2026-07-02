using UnityEngine;

public class BassGenerator
{
    public void Generate(SongData song, System.Random rng)
    {
        if (song == null || song.profile == null)
            return;

        ExpressiveMusicProfile profile = song.profile;

        for (int s = 0; s < song.sections.Count; s++)
        {
            SectionData section = song.sections[s];
            int pulses = DeterminePulses(profile);

            for (int c = 0; c < section.chordEvents.Count; c++)
            {
                ChordEvent chord = section.chordEvents[c];
                int rootPc = ScaleBuilder.RootToPitchClass(chord.chordRoot);
                int fifthPc = ScaleBuilder.Mod12(rootPc + 7);
                int rootMidi = 36 + rootPc;
                int fifthMidi = ScaleBuilder.NextMidiAtOrAbove(rootMidi + 1, fifthPc);
                int octaveMidi = rootMidi + 12;
                float step = section.beatsPerBar / pulses;

                for (int p = 0; p < pulses; p++)
                {
                    int midi;
                    if (p == 0)
                    {
                        midi = rootMidi;
                    }
                    else if (p % 2 == 0)
                    {
                        midi = octaveMidi;
                    }
                    else
                    {
                        midi = profile.rhythmicDensity > 0.65f && rng.NextDouble() > 0.4 ? fifthMidi : rootMidi;
                    }

                    float start = chord.startBeat + p * step;
                    float duration = Mathf.Max(0.18f, step * 0.72f);
                    section.noteEvents.Add(NoteEvent.FromMidi(TrackRole.Bass, midi, start, duration, Mathf.Lerp(0.55f, 0.95f, profile.rhythmicDensity)));
                }
            }
        }

        song.RebuildFlatEventLists();
    }

    private int DeterminePulses(ExpressiveMusicProfile profile)
    {
        if (profile.attribute == ExpressiveMusicAttribute.Calm ||
            profile.attribute == ExpressiveMusicAttribute.Sad ||
            profile.attribute == ExpressiveMusicAttribute.Mysterious)
            return 1;

        if (profile.attribute == ExpressiveMusicAttribute.Energetic ||
            profile.attribute == ExpressiveMusicAttribute.Heroic ||
            profile.attribute == ExpressiveMusicAttribute.Epic)
            return profile.meter == MusicalMeter.SixEight ? 3 : 4;

        return profile.rhythmicDensity > 0.5f ? 2 : 1;
    }
}
