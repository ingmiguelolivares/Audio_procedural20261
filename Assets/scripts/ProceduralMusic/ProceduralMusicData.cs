using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class NoteEvent
{
    public TrackRole trackRole;
    public string noteName;
    public int octave;
    public int midiNote;
    public float startBeat;
    public float durationBeats;
    public float velocity = 0.8f;

    public static NoteEvent FromMidi(TrackRole role, int midiNote, float startBeat, float durationBeats, float velocity)
    {
        string noteName;
        int octave;
        ScaleBuilder.GetNoteParts(midiNote, out noteName, out octave);

        return new NoteEvent
        {
            trackRole = role,
            noteName = noteName,
            octave = octave,
            midiNote = midiNote,
            startBeat = startBeat,
            durationBeats = durationBeats,
            velocity = Mathf.Clamp01(velocity)
        };
    }
}

[Serializable]
public class ChordEvent
{
    public string sectionName;
    public int barIndex;
    public float startBeat;
    public float durationBeats;
    public ChordDegree degree;
    public MusicalRootNote chordRoot;
    public ChordQuality quality;
    public string chordName;
    public List<int> chordMidiNotes = new List<int>();
    public List<string> chordNoteNames = new List<string>();
}

[Serializable]
public class DrumEvent
{
    public DrumType drumType;
    public TrackRole trackRole;
    public float startBeat;
    public float durationBeats = 0.12f;
    public float velocity = 0.8f;
}

[Serializable]
public class SectionData
{
    public string sectionName;
    public int bars;
    public float startBeat;
    public float beatsPerBar;
    public List<ChordEvent> chordEvents = new List<ChordEvent>();
    public List<NoteEvent> noteEvents = new List<NoteEvent>();
    public List<DrumEvent> drumEvents = new List<DrumEvent>();
}

[Serializable]
public class SongData
{
    public ExpressiveMusicProfile profile;
    public int randomSeed;
    public int bpm;
    public MusicalMeter meter;
    public MusicalRootNote rootNote;
    public MusicalMode mode;
    public float beatsPerBar = 4f;
    public float totalBeats;
    public List<SectionData> sections = new List<SectionData>();
    public List<ChordEvent> chordEvents = new List<ChordEvent>();
    public List<NoteEvent> noteEvents = new List<NoteEvent>();
    public List<DrumEvent> drumEvents = new List<DrumEvent>();

    public float TotalSeconds
    {
        get { return bpm <= 0 ? 0f : totalBeats * (60f / bpm); }
    }

    public void RebuildFlatEventLists()
    {
        chordEvents.Clear();
        noteEvents.Clear();
        drumEvents.Clear();
        totalBeats = 0f;

        for (int i = 0; i < sections.Count; i++)
        {
            SectionData section = sections[i];
            chordEvents.AddRange(section.chordEvents);
            noteEvents.AddRange(section.noteEvents);
            drumEvents.AddRange(section.drumEvents);

            float sectionEnd = section.startBeat + section.bars * section.beatsPerBar;
            if (sectionEnd > totalBeats)
                totalBeats = sectionEnd;
        }
    }

    public int CountNotesForRole(TrackRole role)
    {
        int count = 0;
        for (int i = 0; i < noteEvents.Count; i++)
        {
            if (noteEvents[i].trackRole == role)
                count++;
        }

        for (int i = 0; i < drumEvents.Count; i++)
        {
            if (drumEvents[i].trackRole == role)
                count++;
        }

        return count;
    }

    public string BuildSummary()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Generated procedural music summary");
        sb.AppendLine("Attribute: " + (profile != null ? profile.attribute.ToString() : "None"));
        sb.AppendLine("Seed: " + randomSeed);
        sb.AppendLine("Root/Mode: " + rootNote + " " + mode);
        sb.AppendLine("Meter: " + MusicalMeterUtility.GetDisplayName(meter));
        sb.AppendLine("BPM: " + bpm);
        sb.AppendLine("Total beats: " + totalBeats.ToString("F2"));
        sb.AppendLine("Total seconds: " + TotalSeconds.ToString("F2"));
        sb.AppendLine("Chords: " + chordEvents.Count);
        sb.AppendLine("Melody events: " + CountNotesForRole(TrackRole.Melody));
        sb.AppendLine("Bass events: " + CountNotesForRole(TrackRole.Bass));
        sb.AppendLine("Harmony events: " +
                      (CountNotesForRole(TrackRole.Harmony1) +
                       CountNotesForRole(TrackRole.Harmony2) +
                       CountNotesForRole(TrackRole.Harmony3) +
                       CountNotesForRole(TrackRole.Harmony4)));
        sb.AppendLine("Drum events: " + drumEvents.Count);
        return sb.ToString();
    }
}
