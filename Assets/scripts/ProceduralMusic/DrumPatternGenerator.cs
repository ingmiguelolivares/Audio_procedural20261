using UnityEngine;

public class DrumPatternGenerator
{
    public void Generate(SongData song)
    {
        if (song == null || song.profile == null)
            return;

        ExpressiveMusicProfile profile = song.profile;

        if (profile.percussionIntensity <= 0.03f)
            return;

        for (int s = 0; s < song.sections.Count; s++)
        {
            SectionData section = song.sections[s];

            for (int bar = 0; bar < section.bars; bar++)
            {
                float barStart = section.startBeat + bar * section.beatsPerBar;
                AddPatternForBar(section, profile, barStart);
            }
        }

        song.RebuildFlatEventLists();
    }

    private void AddPatternForBar(SectionData section, ExpressiveMusicProfile profile, float barStart)
    {
        switch (profile.meter)
        {
            case MusicalMeter.ThreeFour:
                Add(section, DrumType.Kick, barStart, 0f, profile.percussionIntensity);
                if (profile.percussionIntensity > 0.2f)
                    Add(section, DrumType.Snare, barStart, 2f, profile.percussionIntensity * 0.65f);
                AddHiHats(section, profile, barStart, section.beatsPerBar, profile.rhythmicDensity > 0.55f ? 0.5f : 1f);
                break;

            case MusicalMeter.SixEight:
                Add(section, DrumType.Kick, barStart, 0f, profile.percussionIntensity);
                if (profile.percussionIntensity > 0.15f)
                    Add(section, DrumType.Snare, barStart, 3f, profile.percussionIntensity * 0.72f);
                AddHiHats(section, profile, barStart, section.beatsPerBar, profile.rhythmicDensity > 0.7f ? 0.5f : 1f);
                break;

            default:
                Add(section, DrumType.Kick, barStart, 0f, profile.percussionIntensity);
                if (profile.rhythmicDensity > 0.45f)
                    Add(section, DrumType.Kick, barStart, 2f, profile.percussionIntensity * 0.75f);
                Add(section, DrumType.Snare, barStart, 1f, profile.percussionIntensity * 0.72f);
                Add(section, DrumType.Snare, barStart, 3f, profile.percussionIntensity * 0.72f);
                AddHiHats(section, profile, barStart, section.beatsPerBar, profile.rhythmicDensity > 0.55f ? 0.5f : 1f);
                break;
        }
    }

    private void AddHiHats(SectionData section, ExpressiveMusicProfile profile, float barStart, float beatsPerBar, float step)
    {
        if (profile.drumPatternStyle == DrumPatternStyle.Minimal && profile.percussionIntensity < 0.2f)
            return;

        if (profile.drumPatternStyle == DrumPatternStyle.DarkSparse)
            step = Mathf.Max(step, 1.5f);

        for (float beat = 0f; beat < beatsPerBar - 0.01f; beat += step)
        {
            float accent = MusicalMeterUtility.IsStrongBeat(profile.meter, beat) ? 1f : 0.65f;
            Add(section, DrumType.HiHat, barStart, beat, profile.percussionIntensity * accent * 0.55f);
        }
    }

    private void Add(SectionData section, DrumType drumType, float barStart, float beatOffset, float velocity)
    {
        section.drumEvents.Add(new DrumEvent
        {
            drumType = drumType,
            trackRole = DrumToRole(drumType),
            startBeat = barStart + beatOffset,
            durationBeats = drumType == DrumType.HiHat ? 0.08f : 0.16f,
            velocity = Mathf.Clamp01(velocity)
        });
    }

    private TrackRole DrumToRole(DrumType drumType)
    {
        switch (drumType)
        {
            case DrumType.Snare:
                return TrackRole.Snare;
            case DrumType.HiHat:
                return TrackRole.HiHat;
            default:
                return TrackRole.Kick;
        }
    }
}
