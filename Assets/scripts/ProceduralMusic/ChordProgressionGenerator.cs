using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ChordProgressionGenerator
{
    public SongData GenerateSongSkeleton(ExpressiveMusicProfile profile, int barsA, int barsB, int randomSeed)
    {
        SongData song = new SongData
        {
            profile = profile,
            randomSeed = randomSeed,
            bpm = profile.bpm,
            meter = profile.meter,
            rootNote = profile.rootNote,
            mode = profile.mode,
            beatsPerBar = MusicalMeterUtility.GetBeatsPerBar(profile.meter)
        };

        SectionData sectionA = BuildSection(profile, "A", Mathf.Max(1, barsA), 0f, profile.progressionDegrees, false);
        List<ChordDegree> progressionB = BuildPartBProgression(profile);
        SectionData sectionB = BuildSection(profile, "B", Mathf.Max(1, barsB), sectionA.bars * sectionA.beatsPerBar, progressionB, true);

        song.sections.Add(sectionA);
        song.sections.Add(sectionB);
        song.RebuildFlatEventLists();

        Debug.Log(BuildProgressionLog(song));
        return song;
    }

    private SectionData BuildSection(
        ExpressiveMusicProfile profile,
        string sectionName,
        int bars,
        float startBeat,
        List<ChordDegree> progression,
        bool isVariation)
    {
        SectionData section = new SectionData
        {
            sectionName = sectionName,
            bars = bars,
            startBeat = startBeat,
            beatsPerBar = MusicalMeterUtility.GetBeatsPerBar(profile.meter)
        };

        if (progression == null || progression.Count == 0)
            progression = profile.progressionDegrees;

        for (int bar = 0; bar < bars; bar++)
        {
            ChordDegree degree = progression[bar % progression.Count];

            if (isVariation && profile.variationAmount > 0.5f && bar == bars - 1)
                degree = profile.mode == MusicalMode.Minor ? ChordDegree.I : ChordDegree.V;

            float chordStart = startBeat + bar * section.beatsPerBar;
            ChordEvent chordEvent = ScaleBuilder.BuildChordEvent(profile, degree, sectionName, bar, chordStart, section.beatsPerBar, 3);
            section.chordEvents.Add(chordEvent);
        }

        return section;
    }

    private List<ChordDegree> BuildPartBProgression(ExpressiveMusicProfile profile)
    {
        List<ChordDegree> source = profile.progressionDegrees;
        List<ChordDegree> result = new List<ChordDegree>();

        if (source == null || source.Count == 0)
        {
            result.Add(ChordDegree.I);
            result.Add(ChordDegree.IV);
            result.Add(ChordDegree.V);
            result.Add(ChordDegree.I);
            return result;
        }

        if (profile.variationAmount < 0.3f)
        {
            result.AddRange(source);
            return result;
        }

        int offset = profile.variationAmount > 0.55f ? 2 : 1;
        for (int i = 0; i < source.Count; i++)
            result.Add(source[(i + offset) % source.Count]);

        if (profile.attribute == ExpressiveMusicAttribute.Mysterious ||
            profile.attribute == ExpressiveMusicAttribute.Dark ||
            profile.attribute == ExpressiveMusicAttribute.Threatening)
        {
            result[result.Count - 1] = ChordDegree.VII;
        }
        else if (profile.attribute == ExpressiveMusicAttribute.Heroic ||
                 profile.attribute == ExpressiveMusicAttribute.Epic)
        {
            result[result.Count - 1] = ChordDegree.V;
        }
        else
        {
            result[result.Count - 1] = ChordDegree.I;
        }

        return result;
    }

    private string BuildProgressionLog(SongData song)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[ProceduralMusic] Generated chord progression");

        for (int s = 0; s < song.sections.Count; s++)
        {
            SectionData section = song.sections[s];
            sb.Append("Section ");
            sb.Append(section.sectionName);
            sb.Append(": ");

            for (int i = 0; i < section.chordEvents.Count; i++)
            {
                if (i > 0)
                    sb.Append(" | ");

                ChordEvent chord = section.chordEvents[i];
                sb.Append(chord.chordName);
                sb.Append(" (");
                sb.Append(chord.degree);
                sb.Append(")");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
