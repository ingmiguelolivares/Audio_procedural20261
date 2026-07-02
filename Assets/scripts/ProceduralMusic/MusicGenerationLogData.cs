using System;
using System.Text;

[Serializable]
public class MusicGenerationLogData
{
    public ExpressiveMusicAttribute selectedAttribute;
    public int seed;
    public string generatedAt;
    public string rootAndMode;
    public string meter;
    public int bpm;
    public string progression;
    public int melodyEvents;
    public int bassEvents;
    public int harmonyEvents;
    public int drumEvents;
    public float durationTotalSeconds;
    public int barsPartA;
    public int barsPartB;
    public float melodicDensity;
    public float rhythmicDensity;
    public float percussionIntensity;
    public float variationAmount;

    public static MusicGenerationLogData FromSong(SongData song, MusicGenerationSettings settings)
    {
        MusicGenerationLogData data = new MusicGenerationLogData();

        if (song == null || song.profile == null)
            return data;

        data.selectedAttribute = song.profile.attribute;
        data.seed = song.randomSeed;
        data.generatedAt = DateTime.Now.ToString("s");
        data.rootAndMode = song.rootNote + " " + song.mode;
        data.meter = MusicalMeterUtility.GetDisplayName(song.meter);
        data.bpm = song.bpm;
        data.progression = song.profile.progressionName;
        data.melodyEvents = song.CountNotesForRole(TrackRole.Melody);
        data.bassEvents = song.CountNotesForRole(TrackRole.Bass);
        data.harmonyEvents =
            song.CountNotesForRole(TrackRole.Harmony1) +
            song.CountNotesForRole(TrackRole.Harmony2) +
            song.CountNotesForRole(TrackRole.Harmony3) +
            song.CountNotesForRole(TrackRole.Harmony4);
        data.drumEvents = song.drumEvents.Count;
        data.durationTotalSeconds = song.TotalSeconds;
        data.barsPartA = settings != null ? settings.barsPartA : 0;
        data.barsPartB = settings != null ? settings.barsPartB : 0;
        data.melodicDensity = song.profile.melodicDensity;
        data.rhythmicDensity = song.profile.rhythmicDensity;
        data.percussionIntensity = song.profile.percussionIntensity;
        data.variationAmount = song.profile.variationAmount;
        return data;
    }

    public string ExportSummary()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Music generation log");
        sb.AppendLine("Generated at: " + generatedAt);
        sb.AppendLine("Attribute: " + selectedAttribute);
        sb.AppendLine("Seed: " + seed);
        sb.AppendLine("Root/mode: " + rootAndMode);
        sb.AppendLine("Meter: " + meter);
        sb.AppendLine("BPM: " + bpm);
        sb.AppendLine("Progression: " + progression);
        sb.AppendLine("Bars A/B: " + barsPartA + "/" + barsPartB);
        sb.AppendLine("Duration: " + durationTotalSeconds.ToString("F2") + " seconds");
        sb.AppendLine("Events melody/bass/harmony/drums: " + melodyEvents + "/" + bassEvents + "/" + harmonyEvents + "/" + drumEvents);
        sb.AppendLine("Densities melodic/rhythmic/percussion/variation: " +
                      melodicDensity.ToString("F2") + "/" +
                      rhythmicDensity.ToString("F2") + "/" +
                      percussionIntensity.ToString("F2") + "/" +
                      variationAmount.ToString("F2"));
        return sb.ToString();
    }
}
