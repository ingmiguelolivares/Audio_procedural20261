using UnityEngine;

public class ProceduralMusicManager : MonoBehaviour
{
    [Header("Generation")]
    public MusicGenerationSettings settings = new MusicGenerationSettings();
    public SongData generatedSong;
    public MusicGenerationLogData lastLogData;

    [Header("Runtime references")]
    public OSCController oscController;
    public ProceduralSequencer sequencer;

    [Header("Optional test behavior")]
    public bool generateOnStart = false;
    public bool playOnStart = false;

    private readonly ExpressiveProfileMapper profileMapper = new ExpressiveProfileMapper();
    private readonly ChordProgressionGenerator chordProgressionGenerator = new ChordProgressionGenerator();
    private readonly MelodyGenerator melodyGenerator = new MelodyGenerator();
    private readonly BassGenerator bassGenerator = new BassGenerator();
    private readonly HarmonyGenerator harmonyGenerator = new HarmonyGenerator();
    private readonly DrumPatternGenerator drumPatternGenerator = new DrumPatternGenerator();

    private void Awake()
    {
        if (oscController == null)
            oscController = GetComponent<OSCController>();

        if (sequencer == null)
            sequencer = GetComponent<ProceduralSequencer>();
    }

    private void Start()
    {
        if (generateOnStart)
            GenerateFromAttribute(settings.selectedAttribute, settings.barsPartA, settings.barsPartB);

        if (playOnStart)
            PlayGeneratedSong();
    }

    public void GenerateFromAttribute(ExpressiveMusicAttribute attribute, int barsA, int barsB)
    {
        if (settings == null)
            settings = new MusicGenerationSettings();

        settings.selectedAttribute = attribute;
        settings.barsPartA = Mathf.Max(1, barsA);
        settings.barsPartB = Mathf.Max(1, barsB);
        settings.bpm = Mathf.Clamp(settings.bpm, 30, 240);

        int seed = ResolveSeed();
        System.Random rng = new System.Random(seed);

        ExpressiveMusicProfile profile = profileMapper.Map(attribute, rng);
        ApplyGenerationSettingsToProfile(profile);
        settings.generatedProfile = profile;

        generatedSong = chordProgressionGenerator.GenerateSongSkeleton(profile, settings.barsPartA, settings.barsPartB, seed);
        melodyGenerator.Generate(generatedSong, rng);
        bassGenerator.Generate(generatedSong, rng);
        harmonyGenerator.Generate(generatedSong);
        drumPatternGenerator.Generate(generatedSong);
        generatedSong.RebuildFlatEventLists();

        if (oscController != null)
            oscController.SetProfileTimbre(profile);

        lastLogData = MusicGenerationLogData.FromSong(generatedSong, settings);
        PrintGeneratedSummary();
    }

    public void PlayGeneratedSong()
    {
        if (generatedSong == null)
            GenerateFromAttribute(settings.selectedAttribute, settings.barsPartA, settings.barsPartB);

        if (sequencer == null)
        {
            Debug.LogWarning("[ProceduralMusicManager] No ProceduralSequencer assigned.");
            return;
        }

        sequencer.Play(generatedSong, oscController);
    }

    public void Stop()
    {
        if (sequencer != null)
            sequencer.Stop();
        else if (oscController != null)
            oscController.StopAll();
    }

    public void Regenerate()
    {
        GenerateFromCurrentSettings();
    }

    public void GenerateFromCurrentSettings()
    {
        if (settings == null)
            settings = new MusicGenerationSettings();

        GenerateFromAttribute(settings.selectedAttribute, settings.barsPartA, settings.barsPartB);
    }

    public void SetAttribute(ExpressiveMusicAttribute attribute)
    {
        if (settings == null)
            settings = new MusicGenerationSettings();

        settings.selectedAttribute = attribute;
        generatedSong = null;
    }

    public void SetTempo(int bpm)
    {
        if (settings == null)
            settings = new MusicGenerationSettings();

        settings.bpm = Mathf.Clamp(bpm, 30, 240);
        settings.overrideProfileTempoAndMeter = true;

        if (settings.generatedProfile != null)
            settings.generatedProfile.bpm = settings.bpm;

        if (generatedSong != null)
        {
            generatedSong.bpm = settings.bpm;
            if (generatedSong.profile != null)
                generatedSong.profile.bpm = settings.bpm;

            lastLogData = MusicGenerationLogData.FromSong(generatedSong, settings);
        }
    }

    public void SetMeter(MusicalMeter meter)
    {
        if (settings == null)
            settings = new MusicGenerationSettings();

        settings.meter = meter;
        settings.overrideProfileTempoAndMeter = true;
        generatedSong = null;
    }

    public void SetBarsPartA(int bars)
    {
        if (settings == null)
            settings = new MusicGenerationSettings();

        settings.barsPartA = Mathf.Max(1, bars);
        generatedSong = null;
    }

    public void SetBarsPartB(int bars)
    {
        if (settings == null)
            settings = new MusicGenerationSettings();

        settings.barsPartB = Mathf.Max(1, bars);
        generatedSong = null;
    }

    public void PrintGeneratedSummary()
    {
        if (generatedSong == null)
        {
            Debug.LogWarning("[ProceduralMusicManager] No generated song available.");
            return;
        }

        Debug.Log(generatedSong.BuildSummary());

        if (lastLogData != null)
            Debug.Log(lastLogData.ExportSummary());
    }

    public string ExportSummary()
    {
        if (lastLogData == null)
            return string.Empty;

        return lastLogData.ExportSummary();
    }

    private int ResolveSeed()
    {
        if (settings.useRandomSeed)
            settings.randomSeed = Random.Range(1, int.MaxValue);

        return settings.randomSeed;
    }

    private void ApplyGenerationSettingsToProfile(ExpressiveMusicProfile profile)
    {
        if (profile == null || settings == null)
            return;

        if (settings.overrideProfileTempoAndMeter)
        {
            profile.bpm = Mathf.Clamp(settings.bpm, 30, 240);
            profile.meter = settings.meter;
        }
        else
        {
            settings.bpm = profile.bpm;
            settings.meter = profile.meter;
        }
    }
}
