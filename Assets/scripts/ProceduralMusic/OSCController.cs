using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class OSCController : MonoBehaviour
{
    [Header("Osc role assignments")]
    public Osc MelodyOSC;
    public Osc BassOSC;
    public Osc HarmonyOSC1;
    public Osc HarmonyOSC2;
    public Osc HarmonyOSC3;
    public Osc HarmonyOSC4;
    public Osc KickOSC;
    public Osc SnareOSC;
    public Osc HiHatOSC;

    [Header("Layered drum Osc assignments")]
    [Tooltip("KickOSC is used as the sine B2 layer. These two OSCs add white noise layers.")]
    public Osc KickNoiseOSC1;
    public Osc KickNoiseOSC2;
    [Tooltip("SnareOSC is used as the first white noise layer. This Osc adds the second white noise layer.")]
    public Osc SnareNoiseOSC2;

    [Header("Playback")]
    public bool logMissingAssignments = true;
    public bool applyVelocityToAudioSourceVolume = true;
    public bool forceLfoValuesToZero = true;
    public bool applyMelodicSoundControlsContinuously = true;
    public bool autoFindUnityAudioFilters = true;
    public bool refreshUnityAudioFilterReferencesOnStart = true;

    [Header("Group mixer")]
    [Range(0f, 1f)] public float melodyVolume = 1f;
    [Range(0f, 1f)] public float harmonyVolume = 0.75f;
    [Range(0f, 1f)] public float bassVolume = 0.8f;
    [Range(0f, 1f)] public float kickVolume = 0.9f;
    [Range(0f, 1f)] public float snareVolume = 0.7f;
    [Range(0f, 1f)] public float hiHatVolume = 0.55f;

    [Header("Layered drum levels")]
    public bool forceLayeredDrumDefaults = true;
    [Range(0f, 1f)] public float kickSineLayerLevel = 1f;
    [Range(0f, 1f)] public float kickNoiseLayer1Level = 0.6f;
    [Range(0f, 1f)] public float kickNoiseLayer2Level = 0.45f;
    [Range(0f, 1f)] public float snareNoiseLayer1Level = 0.8f;
    [Range(0f, 1f)] public float snareNoiseLayer2Level = 0.65f;

    [Header("Drum layer balance")]
    [Tooltip("0 = only kick sine layer, 0.5 = current full blend, 1 = only kick noise layers.")]
    [Range(0f, 1f)] public float kickNoiseHarmonicBalance = 0.5f;
    [Tooltip("0 = low noise layer, 0.5 = current full blend, 1 = high noise layer.")]
    [Range(0f, 1f)] public float snareHighLowNoiseBalance = 0.5f;

    [Header("Kick sound")]
    public bool kickSineLayerEnabled = true;
    public bool kickNoiseLayer1Enabled = true;
    public bool kickNoiseLayer2Enabled = true;
    public string kickPitchNote = "B";
    [Range(0, 7)] public int kickPitchOctave = 2;
    public string kickNoiseTriggerNote = "B";
    [Range(0, 7)] public int kickNoiseTriggerOctave = 2;
    [Range(5f, 400f)] public float kickAttackMs = 5f;
    [Range(10f, 1000f)] public float kickDecayMs = 21f;
    [Range(100f, 5000f)] public float kickSustainMs = 205f;
    [Range(0.001f, 1f)] public float kickSustainLevel = 0.02f;

    [Header("Snare sound")]
    public bool snareNoiseLayer1Enabled = true;
    public bool snareNoiseLayer2Enabled = true;
    public string snareNoiseTriggerNote = "D";
    [Range(0, 7)] public int snareNoiseTriggerOctave = 2;
    [Range(5f, 400f)] public float snareAttackMs = 5f;
    [Range(10f, 1000f)] public float snareDecayMs = 21f;
    [Range(100f, 5000f)] public float snareSustainMs = 205f;
    [Range(0.001f, 1f)] public float snareSustainLevel = 0.02f;

    [Header("HiHat sound")]
    [Range(5f, 400f)] public float hiHatAttackMs = 5f;
    [Range(10f, 1000f)] public float hiHatDecayMs = 35f;
    [Range(100f, 5000f)] public float hiHatSustainMs = 120f;
    [Range(0.001f, 1f)] public float hiHatSustainLevel = 0.015f;
    [Range(100f, 1000f)] public float hiHatReleaseMs = 100f;

    [Header("Drum distortion controls")]
    [Range(0f, 5f)] public float kickDistortionAmount = 0f;
    [Range(0f, 5f)] public float snareDistortionAmount = 0f;
    [Range(0f, 5f)] public float hiHatDistortionAmount = 0f;

    [Header("Drum trigger safety")]
    public bool stopDrumLayersBeforePlay = true;
    public float drumRetriggerGuardSeconds = 0.12f;
    public bool logIgnoredDrumRetriggers = false;

    [Header("Melodic articulation controls")]
    public bool useMelodicArticulationControls = false;
    [Range(0f, 1f)] public float melodyPercussiveAmount = 0.55f;
    [Range(0f, 1f)] public float melodySustainAmount = 0.55f;
    [Range(0f, 1f)] public float bassPercussiveAmount = 0.75f;
    [Range(0f, 1f)] public float bassSustainAmount = 0.5f;
    [Range(0f, 1f)] public float harmonyPercussiveAmount = 0.25f;
    [Range(0f, 1f)] public float harmonySustainAmount = 0.75f;

    [Header("Melodic sound controls")]
    public Osc.WaveFormType melodyGroupWaveform = Osc.WaveFormType.Sine;
    public Osc.WaveFormType bassGroupWaveform = Osc.WaveFormType.Sine;
    public Osc.WaveFormType harmonyGroupWaveform = Osc.WaveFormType.Sine;
    [FormerlySerializedAs("melodyDetuneFrames")]
    [Range(-1200f, 1200f)] public float melodyDetuneCents = 0f;
    [FormerlySerializedAs("bassDetuneFrames")]
    [Range(-1200f, 1200f)] public float bassDetuneCents = 0f;
    [FormerlySerializedAs("harmonyDetuneFrames")]
    [Range(-1200f, 1200f)] public float harmonyDetuneCents = 0f;
    public bool melodyFilterModulation = false;
    public bool bassFilterModulation = false;
    public bool harmonyFilterModulation = false;
    [Range(0f, 1f)] public float melodyFilterLevel = 0f;
    [Range(0f, 1f)] public float bassFilterLevel = 0f;
    [Range(0f, 1f)] public float harmonyFilterLevel = 0f;
    [Range(0f, 1f)] public float melodyFMLevel = 0.35f;
    [Range(0f, 1f)] public float bassFMLevel = 0.35f;
    [Range(0f, 1f)] public float harmonyFMLevel = 0.35f;
    public bool melodyDistortion = false;
    public bool bassDistortion = false;
    public bool harmonyDistortion = false;
    [Range(0f, 4f)] public float melodyDistortionAmount = 0.35f;
    [Range(0f, 4f)] public float bassDistortionAmount = 0.35f;
    [Range(0f, 4f)] public float harmonyDistortionAmount = 0.35f;

    [Header("Melodic LFO controls")]
    public bool useMelodicLfoControls = false;
    [FormerlySerializedAs("melodyLfoFrequency")]
    [Range(0f, 12f)] public float melodyTremLfoFrequency = 0f;
    [Range(0f, 12f)] public float melodyVibLfoFrequency = 0f;
    [FormerlySerializedAs("harmonyLfoFrequency")]
    [Range(0f, 12f)] public float harmonyTremLfoFrequency = 0f;
    [Range(0f, 12f)] public float harmonyVibLfoFrequency = 0f;
    [Range(0f, 24f)] public float lfoFrequencySliderMax = 12f;

    [Header("Playback octave controls")]
    public bool useMelodyHarmonyOctaveOverrides = true;
    [Range(3, 8)] public int melodyPlaybackOctave = 5;
    [Range(3, 8)] public int harmonyPlaybackOctave = 4;

    [Header("Unity audio effect assignments")]
    public AudioChorusFilter MelodyModulationFilter;
    public AudioChorusFilter BassModulationFilter;
    public AudioChorusFilter HarmonyModulationFilter1;
    public AudioChorusFilter HarmonyModulationFilter2;
    public AudioChorusFilter HarmonyModulationFilter3;
    public AudioChorusFilter HarmonyModulationFilter4;
    public AudioDistortionFilter MelodyDistortionFilter;
    public AudioDistortionFilter BassDistortionFilter;
    public AudioDistortionFilter HarmonyDistortionFilter1;
    public AudioDistortionFilter HarmonyDistortionFilter2;
    public AudioDistortionFilter HarmonyDistortionFilter3;
    public AudioDistortionFilter HarmonyDistortionFilter4;
    public AudioDistortionFilter KickDistortionFilter;
    public AudioDistortionFilter KickNoiseDistortionFilter1;
    public AudioDistortionFilter KickNoiseDistortionFilter2;
    public AudioDistortionFilter SnareDistortionFilter1;
    public AudioDistortionFilter SnareDistortionFilter2;
    public AudioDistortionFilter HiHatDistortionFilter;
    public AudioLowPassFilter SnareLowNoiseFilter;
    public AudioHighPassFilter SnareHighNoiseFilter;

    [Header("Unity audio effect ranges")]
    [Range(0f, 1f)] public float chorusDryMixAtMax = 0.2f;
    [Range(0f, 1f)] public float chorusWetMixAtMax = 1f;
    [Range(0f, 20f)] public float chorusRateAtMax = 16f;
    [Range(0f, 4f)] public float chorusDepthAtMax = 2f;
    [Range(0.1f, 100f)] public float chorusDelayAtMax = 80f;
    [Range(0f, 4f)] public float distortionEffectMax = 4f;
    [Range(0f, 5f)] public float drumDistortionEffectMax = 5f;

    [Header("Snare noise band split")]
    public bool configureSnareNoiseBandFilters = true;
    [Range(200f, 12000f)] public float snareLowNoiseCutoff = 1800f;
    [Range(200f, 12000f)] public float snareHighNoiseCutoff = 2500f;

    private readonly Dictionary<TrackRole, Coroutine> stopRoutines = new Dictionary<TrackRole, Coroutine>();
    private readonly Dictionary<TrackRole, int> roleTokens = new Dictionary<TrackRole, int>();
    private readonly Dictionary<TrackRole, float> lastVelocityByRole = new Dictionary<TrackRole, float>();
    private ExpressiveMusicProfile currentProfile;

    private void Awake()
    {
        if (refreshUnityAudioFilterReferencesOnStart)
            RefreshUnityAudioEffectReferences();
    }

    private void Update()
    {
        if (applyMelodicSoundControlsContinuously)
            ApplyMelodicSoundControls();
    }

    public void PlayNote(TrackRole trackRole, string noteName, int octave, float duration, float velocity)
    {
        if (trackRole == TrackRole.Kick)
        {
            PlayKick(duration, velocity);
            return;
        }

        if (trackRole == TrackRole.Snare)
        {
            PlaySnare(duration, velocity);
            return;
        }

        if (trackRole == TrackRole.HiHat)
        {
            PlayHiHat(duration, velocity);
            return;
        }

        Osc osc = GetOSC(trackRole);
        if (osc == null)
        {
            if (logMissingAssignments)
                Debug.LogWarning("[OSCController] No Osc assigned for role " + trackRole + ". Event would play: " + noteName + octave);
            return;
        }

        string normalizedNote = NormalizeNoteName(noteName);
        int playbackOctave = GetPlaybackOctaveForRole(trackRole, octave);
        osc.Octava = Mathf.Clamp(playbackOctave, 0, 8);
        ApplyLfoDefaults(osc);

        ApplyInstrumentADSR(trackRole, osc, duration);
        ApplySoundControlsForTrackRole(trackRole, osc);

        lastVelocityByRole[trackRole] = Mathf.Clamp01(velocity);
        ApplyRoleVolume(trackRole);

        IncrementToken(trackRole);
        osc.KeyboardDown(normalizedNote);

        if (duration > 0f)
        {
            if (stopRoutines.ContainsKey(trackRole) && stopRoutines[trackRole] != null)
                StopCoroutine(stopRoutines[trackRole]);

            int token = roleTokens[trackRole];
            stopRoutines[trackRole] = StartCoroutine(StopAfter(trackRole, normalizedNote, octave, duration, token));
        }
    }

    public void PlayKick(float duration, float velocity)
    {
        if (!HasAnyKickOSC())
        {
            if (logMissingAssignments)
                Debug.LogWarning("[OSCController] No Osc assigned for Kick. Expected KickOSC plus optional KickNoiseOSC1/KickNoiseOSC2.");
            return;
        }

        ConfigureKickStack();
        ApplyDrumDistortionForGroup(OSCGroupRole.Kick);
        lastVelocityByRole[TrackRole.Kick] = Mathf.Clamp01(velocity);
        ApplyRoleVolume(TrackRole.Kick);

        if (kickSineLayerEnabled)
            PlayDrumLayer(KickOSC, kickPitchNote, kickPitchOctave);
        if (kickNoiseLayer1Enabled && KickNoiseOSC1 != KickOSC)
            PlayDrumLayer(KickNoiseOSC1, kickNoiseTriggerNote, kickNoiseTriggerOctave);
        if (kickNoiseLayer2Enabled && KickNoiseOSC2 != KickOSC && KickNoiseOSC2 != KickNoiseOSC1)
            PlayDrumLayer(KickNoiseOSC2, kickNoiseTriggerNote, kickNoiseTriggerOctave);
    }

    public void PlaySnare(float duration, float velocity)
    {
        if (!HasAnySnareOSC())
        {
            if (logMissingAssignments)
                Debug.LogWarning("[OSCController] No Osc assigned for Snare. Expected SnareOSC plus optional SnareNoiseOSC2.");
            return;
        }

        ConfigureSnareStack();
        ApplyDrumDistortionForGroup(OSCGroupRole.Snare);
        lastVelocityByRole[TrackRole.Snare] = Mathf.Clamp01(velocity);
        ApplyRoleVolume(TrackRole.Snare);

        if (snareNoiseLayer1Enabled)
            PlayDrumLayer(SnareOSC, snareNoiseTriggerNote, snareNoiseTriggerOctave);
        if (snareNoiseLayer2Enabled && SnareNoiseOSC2 != SnareOSC)
            PlayDrumLayer(SnareNoiseOSC2, snareNoiseTriggerNote, snareNoiseTriggerOctave);
    }

    public void PlayHiHat(float duration, float velocity)
    {
        if (HiHatOSC == null)
        {
            if (logMissingAssignments)
                Debug.LogWarning("[OSCController] No Osc assigned for HiHat.");
            return;
        }

        ConfigureHiHatOSC();
        ApplyDrumDistortionForGroup(OSCGroupRole.HiHat);
        lastVelocityByRole[TrackRole.HiHat] = Mathf.Clamp01(velocity);
        ApplyRoleVolume(TrackRole.HiHat);

        PlayDrumLayer(HiHatOSC, "B", 5);
    }

    public void StopNote(TrackRole trackRole, string noteName, int octave)
    {
        if (trackRole == TrackRole.Kick)
        {
            IncrementToken(trackRole);
            StopDrumLayerOscs(TrackRole.Kick);
            return;
        }

        if (trackRole == TrackRole.Snare)
        {
            IncrementToken(trackRole);
            StopDrumLayerOscs(TrackRole.Snare);
            return;
        }

        Osc osc = GetOSC(trackRole);
        if (osc == null)
            return;

        IncrementToken(trackRole);
        osc.KeyboardUp();
    }

    public void SetProfileTimbre(ExpressiveMusicProfile profile)
    {
        if (profile == null)
            return;

        currentProfile = profile;

        melodyGroupWaveform = profile.melodyWaveform;
        bassGroupWaveform = profile.bassWaveform;
        harmonyGroupWaveform = profile.harmonyWaveform;
        SetWaveform(HiHatOSC, profile.hiHatWaveform);

        ApplyLfoDefaultsToAllOSCs();
        ApplyMelodicSoundControls();
        ApplyInstrumentADSR(TrackRole.Melody, MelodyOSC, 0.5f);
        ApplyInstrumentADSR(TrackRole.Bass, BassOSC, 0.5f);
        ApplyInstrumentADSRToHarmony(1f);

        ConfigureKickStack();
        ConfigureSnareStack();
        ConfigureHiHatOSC();
        ApplyDrumDistortionControls();
        ApplyGroupVolumes();
    }

    public void StopAll()
    {
        foreach (KeyValuePair<TrackRole, Coroutine> kv in stopRoutines)
        {
            if (kv.Value != null)
                StopCoroutine(kv.Value);
        }

        stopRoutines.Clear();

        List<Osc> uniqueOscs = GetUniqueOSCs();
        for (int i = 0; i < uniqueOscs.Count; i++)
        {
            if (uniqueOscs[i] != null)
                uniqueOscs[i].KeyboardUp();
        }

        roleTokens.Clear();
    }

    public void SetGroupVolume(OSCGroupRole groupRole, float volume)
    {
        volume = Mathf.Clamp01(volume);

        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyVolume = volume;
                ApplyRoleVolume(TrackRole.Melody);
                break;

            case OSCGroupRole.Harmony:
                harmonyVolume = volume;
                ApplyRoleVolume(TrackRole.Harmony1);
                ApplyRoleVolume(TrackRole.Harmony2);
                ApplyRoleVolume(TrackRole.Harmony3);
                ApplyRoleVolume(TrackRole.Harmony4);
                break;

            case OSCGroupRole.Bass:
                bassVolume = volume;
                ApplyRoleVolume(TrackRole.Bass);
                break;

            case OSCGroupRole.Kick:
                kickVolume = volume;
                ApplyRoleVolume(TrackRole.Kick);
                break;

            case OSCGroupRole.Snare:
                snareVolume = volume;
                ApplyRoleVolume(TrackRole.Snare);
                break;

            case OSCGroupRole.HiHat:
                hiHatVolume = volume;
                ApplyRoleVolume(TrackRole.HiHat);
                break;
        }
    }

    public float GetGroupVolume(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyVolume;
            case OSCGroupRole.Harmony:
                return harmonyVolume;
            case OSCGroupRole.Bass:
                return bassVolume;
            case OSCGroupRole.Kick:
                return kickVolume;
            case OSCGroupRole.Snare:
                return snareVolume;
            case OSCGroupRole.HiHat:
                return hiHatVolume;
            default:
                return 1f;
        }
    }

    public void SetInstrumentPercussiveAmount(OSCGroupRole groupRole, float value)
    {
        value = Mathf.Clamp01(value);
        useMelodicArticulationControls = true;

        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyPercussiveAmount = value;
                ApplyInstrumentADSR(TrackRole.Melody, MelodyOSC, 0.5f);
                break;

            case OSCGroupRole.Bass:
                bassPercussiveAmount = value;
                ApplyInstrumentADSR(TrackRole.Bass, BassOSC, 0.5f);
                break;

            case OSCGroupRole.Harmony:
                harmonyPercussiveAmount = value;
                ApplyInstrumentADSRToHarmony(1f);
                break;
        }
    }

    public void SetInstrumentSustainAmount(OSCGroupRole groupRole, float value)
    {
        value = Mathf.Clamp01(value);
        useMelodicArticulationControls = true;

        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodySustainAmount = value;
                ApplyInstrumentADSR(TrackRole.Melody, MelodyOSC, 0.5f);
                break;

            case OSCGroupRole.Bass:
                bassSustainAmount = value;
                ApplyInstrumentADSR(TrackRole.Bass, BassOSC, 0.5f);
                break;

            case OSCGroupRole.Harmony:
                harmonySustainAmount = value;
                ApplyInstrumentADSRToHarmony(1f);
                break;
        }
    }

    public float GetInstrumentPercussiveAmount(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyPercussiveAmount;
            case OSCGroupRole.Bass:
                return bassPercussiveAmount;
            case OSCGroupRole.Harmony:
                return harmonyPercussiveAmount;
            default:
                return 0f;
        }
    }

    public float GetInstrumentSustainAmount(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodySustainAmount;
            case OSCGroupRole.Bass:
                return bassSustainAmount;
            case OSCGroupRole.Harmony:
                return harmonySustainAmount;
            default:
                return 0f;
        }
    }

    public void SetGroupWaveform(OSCGroupRole groupRole, Osc.WaveFormType waveform)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyGroupWaveform = waveform;
                break;

            case OSCGroupRole.Bass:
                bassGroupWaveform = waveform;
                break;

            case OSCGroupRole.Harmony:
                harmonyGroupWaveform = waveform;
                break;

            default:
                return;
        }

        ApplySoundControlsForGroup(groupRole);
    }

    public void SetGroupWaveform(OSCGroupRole groupRole, int waveformIndex)
    {
        Osc.WaveFormType[] values = (Osc.WaveFormType[])System.Enum.GetValues(typeof(Osc.WaveFormType));
        int index = Mathf.Clamp(waveformIndex, 0, values.Length - 1);
        SetGroupWaveform(groupRole, values[index]);
    }

    public Osc.WaveFormType GetGroupWaveform(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyGroupWaveform;
            case OSCGroupRole.Bass:
                return bassGroupWaveform;
            case OSCGroupRole.Harmony:
                return harmonyGroupWaveform;
            default:
                return Osc.WaveFormType.Sine;
        }
    }

    public void SetGroupDetuneCents(OSCGroupRole groupRole, float detuneCents)
    {
        detuneCents = Mathf.Clamp(detuneCents, -1200f, 1200f);

        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyDetuneCents = detuneCents;
                break;

            case OSCGroupRole.Bass:
                bassDetuneCents = detuneCents;
                break;

            case OSCGroupRole.Harmony:
                harmonyDetuneCents = detuneCents;
                break;

            default:
                return;
        }

        ApplySoundControlsForGroup(groupRole);
    }

    public float GetGroupDetuneCents(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyDetuneCents;
            case OSCGroupRole.Bass:
                return bassDetuneCents;
            case OSCGroupRole.Harmony:
                return harmonyDetuneCents;
            default:
                return 0f;
        }
    }

    public void SetGroupDetuneFrames(OSCGroupRole groupRole, float detuneFrames)
    {
        SetGroupDetuneCents(groupRole, detuneFrames);
    }

    public float GetGroupDetuneFrames(OSCGroupRole groupRole)
    {
        return GetGroupDetuneCents(groupRole);
    }

    public void SetGroupFilterModulation(OSCGroupRole groupRole, bool enabled)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyFilterModulation = enabled;
                break;

            case OSCGroupRole.Bass:
                bassFilterModulation = enabled;
                break;

            case OSCGroupRole.Harmony:
                harmonyFilterModulation = enabled;
                break;

            default:
                return;
        }

        ApplySoundControlsForGroup(groupRole);
    }

    public bool GetGroupFilterModulation(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyFilterModulation;
            case OSCGroupRole.Bass:
                return bassFilterModulation;
            case OSCGroupRole.Harmony:
                return harmonyFilterModulation;
            default:
                return false;
        }
    }

    public void SetGroupFilterLevel(OSCGroupRole groupRole, float level)
    {
        level = Mathf.Clamp01(level);

        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyFilterLevel = level;
                break;

            case OSCGroupRole.Bass:
                bassFilterLevel = level;
                break;

            case OSCGroupRole.Harmony:
                harmonyFilterLevel = level;
                break;

            default:
                return;
        }

        ApplySoundControlsForGroup(groupRole);
    }

    public float GetGroupFilterLevel(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyFilterLevel;
            case OSCGroupRole.Bass:
                return bassFilterLevel;
            case OSCGroupRole.Harmony:
                return harmonyFilterLevel;
            default:
                return 0f;
        }
    }

    public void SetGroupFMLevel(OSCGroupRole groupRole, float level)
    {
        level = Mathf.Clamp01(level);

        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyFMLevel = level;
                break;

            case OSCGroupRole.Bass:
                bassFMLevel = level;
                break;

            case OSCGroupRole.Harmony:
                harmonyFMLevel = level;
                break;

            default:
                return;
        }

        ApplySoundControlsForGroup(groupRole);
    }

    public float GetGroupFMLevel(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyFMLevel;
            case OSCGroupRole.Bass:
                return bassFMLevel;
            case OSCGroupRole.Harmony:
                return harmonyFMLevel;
            default:
                return 0f;
        }
    }

    public void SetGroupDistortion(OSCGroupRole groupRole, bool enabled)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyDistortion = enabled;
                break;

            case OSCGroupRole.Bass:
                bassDistortion = enabled;
                break;

            case OSCGroupRole.Harmony:
                harmonyDistortion = enabled;
                break;

            default:
                return;
        }

        ApplySoundControlsForGroup(groupRole);
    }

    public bool GetGroupDistortion(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyDistortion;
            case OSCGroupRole.Bass:
                return bassDistortion;
            case OSCGroupRole.Harmony:
                return harmonyDistortion;
            default:
                return false;
        }
    }

    public void SetGroupDistortionAmount(OSCGroupRole groupRole, float amount)
    {
        amount = Mathf.Clamp(amount, 0f, Mathf.Max(0f, distortionEffectMax));

        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyDistortionAmount = amount;
                break;

            case OSCGroupRole.Bass:
                bassDistortionAmount = amount;
                break;

            case OSCGroupRole.Harmony:
                harmonyDistortionAmount = amount;
                break;

            default:
                return;
        }

        ApplySoundControlsForGroup(groupRole);
    }

    public float GetGroupDistortionAmount(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyDistortionAmount;
            case OSCGroupRole.Bass:
                return bassDistortionAmount;
            case OSCGroupRole.Harmony:
                return harmonyDistortionAmount;
            default:
                return 0f;
        }
    }

    public void SetDrumDistortionAmount(OSCGroupRole groupRole, float amount)
    {
        amount = Mathf.Clamp(amount, 0f, Mathf.Max(0f, drumDistortionEffectMax));

        switch (groupRole)
        {
            case OSCGroupRole.Kick:
                kickDistortionAmount = amount;
                break;

            case OSCGroupRole.Snare:
                snareDistortionAmount = amount;
                break;

            case OSCGroupRole.HiHat:
                hiHatDistortionAmount = amount;
                break;

            default:
                return;
        }

        ApplyDrumDistortionForGroup(groupRole);
    }

    public float GetDrumDistortionAmount(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Kick:
                return kickDistortionAmount;
            case OSCGroupRole.Snare:
                return snareDistortionAmount;
            case OSCGroupRole.HiHat:
                return hiHatDistortionAmount;
            default:
                return 0f;
        }
    }

    public void SetKickNoiseHarmonicBalance(float value)
    {
        kickNoiseHarmonicBalance = Mathf.Clamp01(value);
        ApplyRoleVolume(TrackRole.Kick);
    }

    public float GetKickNoiseHarmonicBalance()
    {
        return kickNoiseHarmonicBalance;
    }

    public void SetSnareHighLowNoiseBalance(float value)
    {
        snareHighLowNoiseBalance = Mathf.Clamp01(value);
        ApplySnareNoiseBandFilters();
        ApplyRoleVolume(TrackRole.Snare);
    }

    public float GetSnareHighLowNoiseBalance()
    {
        return snareHighLowNoiseBalance;
    }

    public void SetGroupLfoFrequency(OSCGroupRole groupRole, float frequency)
    {
        SetGroupTremLfoFrequency(groupRole, frequency);
        SetGroupVibLfoFrequency(groupRole, frequency);
    }

    public float GetGroupLfoFrequency(OSCGroupRole groupRole)
    {
        return GetGroupTremLfoFrequency(groupRole);
    }

    public void SetGroupTremLfoFrequency(OSCGroupRole groupRole, float frequency)
    {
        frequency = ClampLfoFrequency(frequency);
        useMelodicLfoControls = true;

        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyTremLfoFrequency = frequency;
                ApplyLfoForGroup(OSCGroupRole.Melody);
                break;

            case OSCGroupRole.Harmony:
                harmonyTremLfoFrequency = frequency;
                ApplyLfoForGroup(OSCGroupRole.Harmony);
                break;
        }
    }

    public float GetGroupTremLfoFrequency(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyTremLfoFrequency;
            case OSCGroupRole.Harmony:
                return harmonyTremLfoFrequency;
            default:
                return 0f;
        }
    }

    public void SetGroupVibLfoFrequency(OSCGroupRole groupRole, float frequency)
    {
        frequency = ClampLfoFrequency(frequency);
        useMelodicLfoControls = true;

        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyVibLfoFrequency = frequency;
                ApplyLfoForGroup(OSCGroupRole.Melody);
                break;

            case OSCGroupRole.Harmony:
                harmonyVibLfoFrequency = frequency;
                ApplyLfoForGroup(OSCGroupRole.Harmony);
                break;
        }
    }

    public float GetGroupVibLfoFrequency(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyVibLfoFrequency;
            case OSCGroupRole.Harmony:
                return harmonyVibLfoFrequency;
            default:
                return 0f;
        }
    }

    public void SetGroupPlaybackOctave(OSCGroupRole groupRole, int octave)
    {
        octave = Mathf.Clamp(octave, 3, 8);
        useMelodyHarmonyOctaveOverrides = true;

        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                melodyPlaybackOctave = octave;
                ApplyPlaybackOctaveToOSC(MelodyOSC, melodyPlaybackOctave);
                break;

            case OSCGroupRole.Harmony:
                harmonyPlaybackOctave = octave;
                ApplyPlaybackOctaveToOSC(HarmonyOSC1, harmonyPlaybackOctave);
                ApplyPlaybackOctaveToOSC(HarmonyOSC2, harmonyPlaybackOctave);
                ApplyPlaybackOctaveToOSC(HarmonyOSC3, harmonyPlaybackOctave);
                ApplyPlaybackOctaveToOSC(HarmonyOSC4, harmonyPlaybackOctave);
                break;
        }
    }

    public int GetGroupPlaybackOctave(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                return melodyPlaybackOctave;
            case OSCGroupRole.Harmony:
                return harmonyPlaybackOctave;
            default:
                return 4;
        }
    }

    public void ApplyMelodicSoundControls()
    {
        ApplySoundControlsForGroup(OSCGroupRole.Melody);
        ApplySoundControlsForGroup(OSCGroupRole.Bass);
        ApplySoundControlsForGroup(OSCGroupRole.Harmony);
        ApplyMelodicLfoControls();
    }

    public void ApplyMelodicLfoControls()
    {
        if (!useMelodicLfoControls)
            return;

        ApplyLfoForGroup(OSCGroupRole.Melody);
        ApplyLfoForGroup(OSCGroupRole.Harmony);
    }

    [ContextMenu("Refresh Unity Audio Effect References")]
    public void RefreshUnityAudioEffectReferences()
    {
        MelodyModulationFilter = FindUnityAudioFilter(MelodyOSC, MelodyModulationFilter);
        BassModulationFilter = FindUnityAudioFilter(BassOSC, BassModulationFilter);
        HarmonyModulationFilter1 = FindUnityAudioFilter(HarmonyOSC1, HarmonyModulationFilter1);
        HarmonyModulationFilter2 = FindUnityAudioFilter(HarmonyOSC2, HarmonyModulationFilter2);
        HarmonyModulationFilter3 = FindUnityAudioFilter(HarmonyOSC3, HarmonyModulationFilter3);
        HarmonyModulationFilter4 = FindUnityAudioFilter(HarmonyOSC4, HarmonyModulationFilter4);

        MelodyDistortionFilter = FindUnityAudioFilter(MelodyOSC, MelodyDistortionFilter);
        BassDistortionFilter = FindUnityAudioFilter(BassOSC, BassDistortionFilter);
        HarmonyDistortionFilter1 = FindUnityAudioFilter(HarmonyOSC1, HarmonyDistortionFilter1);
        HarmonyDistortionFilter2 = FindUnityAudioFilter(HarmonyOSC2, HarmonyDistortionFilter2);
        HarmonyDistortionFilter3 = FindUnityAudioFilter(HarmonyOSC3, HarmonyDistortionFilter3);
        HarmonyDistortionFilter4 = FindUnityAudioFilter(HarmonyOSC4, HarmonyDistortionFilter4);

        KickDistortionFilter = FindUnityAudioFilter(KickOSC, KickDistortionFilter);
        KickNoiseDistortionFilter1 = FindUnityAudioFilter(KickNoiseOSC1, KickNoiseDistortionFilter1);
        KickNoiseDistortionFilter2 = FindUnityAudioFilter(KickNoiseOSC2, KickNoiseDistortionFilter2);
        SnareDistortionFilter1 = FindUnityAudioFilter(SnareOSC, SnareDistortionFilter1);
        SnareDistortionFilter2 = FindUnityAudioFilter(SnareNoiseOSC2, SnareDistortionFilter2);
        HiHatDistortionFilter = FindUnityAudioFilter(HiHatOSC, HiHatDistortionFilter);
        SnareLowNoiseFilter = FindUnityAudioFilter(SnareOSC, SnareLowNoiseFilter);
        SnareHighNoiseFilter = FindUnityAudioFilter(SnareNoiseOSC2, SnareHighNoiseFilter);
    }

    public void ApplyGroupVolumes()
    {
        ApplyRoleVolume(TrackRole.Melody);
        ApplyRoleVolume(TrackRole.Bass);
        ApplyRoleVolume(TrackRole.Harmony1);
        ApplyRoleVolume(TrackRole.Harmony2);
        ApplyRoleVolume(TrackRole.Harmony3);
        ApplyRoleVolume(TrackRole.Harmony4);
        ApplyRoleVolume(TrackRole.Kick);
        ApplyRoleVolume(TrackRole.Snare);
        ApplyRoleVolume(TrackRole.HiHat);
    }

    public void ApplyLfoDefaultsToAllOSCs()
    {
        List<Osc> uniqueOscs = GetUniqueOSCs();
        for (int i = 0; i < uniqueOscs.Count; i++)
            ApplyLfoDefaults(uniqueOscs[i]);
    }

    public Osc GetOSC(TrackRole role)
    {
        switch (role)
        {
            case TrackRole.Melody: return MelodyOSC;
            case TrackRole.Bass: return BassOSC;
            case TrackRole.Harmony1: return HarmonyOSC1;
            case TrackRole.Harmony2: return HarmonyOSC2;
            case TrackRole.Harmony3: return HarmonyOSC3;
            case TrackRole.Harmony4: return HarmonyOSC4;
            case TrackRole.Kick: return KickOSC;
            case TrackRole.Snare: return SnareOSC;
            case TrackRole.HiHat: return HiHatOSC;
            default: return null;
        }
    }

    private void ApplyRoleVolume(TrackRole role)
    {
        float velocity = 1f;
        if (applyVelocityToAudioSourceVolume && lastVelocityByRole.ContainsKey(role))
            velocity = lastVelocityByRole[role];

        if (role == TrackRole.Kick)
        {
            float harmonicFactor = GetCenteredBalanceLeftFactor(kickNoiseHarmonicBalance);
            float noiseFactor = GetCenteredBalanceRightFactor(kickNoiseHarmonicBalance);

            ApplyOscVolume(KickOSC, velocity, kickVolume, kickSineLayerLevel * harmonicFactor);
            ApplyOscVolume(KickNoiseOSC1, velocity, kickVolume, kickNoiseLayer1Level * noiseFactor);
            ApplyOscVolume(KickNoiseOSC2, velocity, kickVolume, kickNoiseLayer2Level * noiseFactor);
            return;
        }

        if (role == TrackRole.Snare)
        {
            float lowNoiseFactor = GetCenteredBalanceLeftFactor(snareHighLowNoiseBalance);
            float highNoiseFactor = GetCenteredBalanceRightFactor(snareHighLowNoiseBalance);

            ApplyOscVolume(SnareOSC, velocity, snareVolume, snareNoiseLayer1Level * lowNoiseFactor);
            ApplyOscVolume(SnareNoiseOSC2, velocity, snareVolume, snareNoiseLayer2Level * highNoiseFactor);
            return;
        }

        Osc osc = GetOSC(role);
        ApplyOscVolume(osc, velocity, GetGroupVolumeForTrackRole(role), 1f);
    }

    public void ApplyDrumDistortionControls()
    {
        ApplyDrumDistortionForGroup(OSCGroupRole.Kick);
        ApplyDrumDistortionForGroup(OSCGroupRole.Snare);
        ApplyDrumDistortionForGroup(OSCGroupRole.HiHat);
    }

    private void ApplyDrumDistortionForGroup(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Kick:
                ApplyUnityDistortionFilter(KickOSC, KickDistortionFilter, kickDistortionAmount > 0.001f, kickDistortionAmount, drumDistortionEffectMax);
                ApplyUnityDistortionFilter(KickNoiseOSC1, KickNoiseDistortionFilter1, kickDistortionAmount > 0.001f, kickDistortionAmount, drumDistortionEffectMax);
                ApplyUnityDistortionFilter(KickNoiseOSC2, KickNoiseDistortionFilter2, kickDistortionAmount > 0.001f, kickDistortionAmount, drumDistortionEffectMax);
                break;

            case OSCGroupRole.Snare:
                ApplyUnityDistortionFilter(SnareOSC, SnareDistortionFilter1, snareDistortionAmount > 0.001f, snareDistortionAmount, drumDistortionEffectMax);
                ApplyUnityDistortionFilter(SnareNoiseOSC2, SnareDistortionFilter2, snareDistortionAmount > 0.001f, snareDistortionAmount, drumDistortionEffectMax);
                break;

            case OSCGroupRole.HiHat:
                ApplyUnityDistortionFilter(HiHatOSC, HiHatDistortionFilter, hiHatDistortionAmount > 0.001f, hiHatDistortionAmount, drumDistortionEffectMax);
                break;
        }
    }

    private static float GetCenteredBalanceLeftFactor(float balance)
    {
        balance = Mathf.Clamp01(balance);
        return balance <= 0.5f ? 1f : 1f - ((balance - 0.5f) * 2f);
    }

    private static float GetCenteredBalanceRightFactor(float balance)
    {
        balance = Mathf.Clamp01(balance);
        return balance >= 0.5f ? 1f : balance * 2f;
    }

    private void ApplySoundControlsForGroup(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                ApplySoundControlsToOSC(MelodyOSC, MelodyModulationFilter, MelodyDistortionFilter, melodyGroupWaveform, melodyDetuneCents, melodyFMLevel, melodyFilterModulation, melodyFilterLevel, melodyDistortion, melodyDistortionAmount);
                break;

            case OSCGroupRole.Bass:
                ApplySoundControlsToOSC(BassOSC, BassModulationFilter, BassDistortionFilter, bassGroupWaveform, bassDetuneCents, bassFMLevel, bassFilterModulation, bassFilterLevel, bassDistortion, bassDistortionAmount);
                break;

            case OSCGroupRole.Harmony:
                ApplySoundControlsToOSC(HarmonyOSC1, HarmonyModulationFilter1, HarmonyDistortionFilter1, harmonyGroupWaveform, harmonyDetuneCents, harmonyFMLevel, harmonyFilterModulation, harmonyFilterLevel, harmonyDistortion, harmonyDistortionAmount);
                ApplySoundControlsToOSC(HarmonyOSC2, HarmonyModulationFilter2, HarmonyDistortionFilter2, harmonyGroupWaveform, harmonyDetuneCents, harmonyFMLevel, harmonyFilterModulation, harmonyFilterLevel, harmonyDistortion, harmonyDistortionAmount);
                ApplySoundControlsToOSC(HarmonyOSC3, HarmonyModulationFilter3, HarmonyDistortionFilter3, harmonyGroupWaveform, harmonyDetuneCents, harmonyFMLevel, harmonyFilterModulation, harmonyFilterLevel, harmonyDistortion, harmonyDistortionAmount);
                ApplySoundControlsToOSC(HarmonyOSC4, HarmonyModulationFilter4, HarmonyDistortionFilter4, harmonyGroupWaveform, harmonyDetuneCents, harmonyFMLevel, harmonyFilterModulation, harmonyFilterLevel, harmonyDistortion, harmonyDistortionAmount);
                break;
        }
    }

    private void ApplySoundControlsForTrackRole(TrackRole role, Osc osc)
    {
        switch (role)
        {
            case TrackRole.Melody:
                ApplySoundControlsToOSC(osc, MelodyModulationFilter, MelodyDistortionFilter, melodyGroupWaveform, melodyDetuneCents, melodyFMLevel, melodyFilterModulation, melodyFilterLevel, melodyDistortion, melodyDistortionAmount);
                break;

            case TrackRole.Bass:
                ApplySoundControlsToOSC(osc, BassModulationFilter, BassDistortionFilter, bassGroupWaveform, bassDetuneCents, bassFMLevel, bassFilterModulation, bassFilterLevel, bassDistortion, bassDistortionAmount);
                break;

            case TrackRole.Harmony1:
                ApplySoundControlsToOSC(osc, HarmonyModulationFilter1, HarmonyDistortionFilter1, harmonyGroupWaveform, harmonyDetuneCents, harmonyFMLevel, harmonyFilterModulation, harmonyFilterLevel, harmonyDistortion, harmonyDistortionAmount);
                break;

            case TrackRole.Harmony2:
                ApplySoundControlsToOSC(osc, HarmonyModulationFilter2, HarmonyDistortionFilter2, harmonyGroupWaveform, harmonyDetuneCents, harmonyFMLevel, harmonyFilterModulation, harmonyFilterLevel, harmonyDistortion, harmonyDistortionAmount);
                break;

            case TrackRole.Harmony3:
                ApplySoundControlsToOSC(osc, HarmonyModulationFilter3, HarmonyDistortionFilter3, harmonyGroupWaveform, harmonyDetuneCents, harmonyFMLevel, harmonyFilterModulation, harmonyFilterLevel, harmonyDistortion, harmonyDistortionAmount);
                break;

            case TrackRole.Harmony4:
                ApplySoundControlsToOSC(osc, HarmonyModulationFilter4, HarmonyDistortionFilter4, harmonyGroupWaveform, harmonyDetuneCents, harmonyFMLevel, harmonyFilterModulation, harmonyFilterLevel, harmonyDistortion, harmonyDistortionAmount);
                break;
        }
    }

    private float GetGroupVolumeForTrackRole(TrackRole role)
    {
        switch (role)
        {
            case TrackRole.Melody:
                return melodyVolume;
            case TrackRole.Bass:
                return bassVolume;
            case TrackRole.Harmony1:
            case TrackRole.Harmony2:
            case TrackRole.Harmony3:
            case TrackRole.Harmony4:
                return harmonyVolume;
            case TrackRole.Kick:
                return kickVolume;
            case TrackRole.Snare:
                return snareVolume;
            case TrackRole.HiHat:
                return hiHatVolume;
            default:
                return 1f;
        }
    }

    private int GetPlaybackOctaveForRole(TrackRole role, int requestedOctave)
    {
        if (!useMelodyHarmonyOctaveOverrides)
            return requestedOctave;

        switch (role)
        {
            case TrackRole.Melody:
                return melodyPlaybackOctave;

            case TrackRole.Harmony1:
            case TrackRole.Harmony2:
            case TrackRole.Harmony3:
            case TrackRole.Harmony4:
                return harmonyPlaybackOctave;

            default:
                return requestedOctave;
        }
    }

    private static void ApplyPlaybackOctaveToOSC(Osc osc, int octave)
    {
        if (osc != null)
            osc.Octava = Mathf.Clamp(octave, 3, 8);
    }

    private IEnumerator StopAfter(TrackRole role, string noteName, int octave, float duration, int token)
    {
        yield return new WaitForSeconds(duration);

        if (roleTokens.ContainsKey(role) && roleTokens[role] == token)
            StopNote(role, noteName, octave);
    }

    private void IncrementToken(TrackRole role)
    {
        if (!roleTokens.ContainsKey(role))
            roleTokens[role] = 0;

        roleTokens[role]++;
    }

    private void StopDrumLayerOscs(TrackRole role)
    {
        List<Osc> stoppedOscs = new List<Osc>();

        if (role == TrackRole.Kick)
        {
            StopOscUnique(stoppedOscs, KickOSC);
            StopOscUnique(stoppedOscs, KickNoiseOSC1);
            StopOscUnique(stoppedOscs, KickNoiseOSC2);
            return;
        }

        if (role == TrackRole.Snare)
        {
            StopOscUnique(stoppedOscs, SnareOSC);
            StopOscUnique(stoppedOscs, SnareNoiseOSC2);
        }
    }

    private void PlayOsc(Osc osc, string noteName, int octave)
    {
        if (osc == null)
            return;

        osc.Octava = Mathf.Clamp(octave, 0, 8);
        ApplyLfoDefaults(osc);
        osc.KeyboardDown(NormalizeNoteName(noteName));
    }

    private void PlayDrumLayer(Osc osc, string noteName, int octave)
    {
        if (osc == null)
            return;

        osc.Octava = Mathf.Clamp(octave, 0, 7);
        ApplyLfoDefaults(osc);

        if (stopDrumLayersBeforePlay)
            osc.KeyboardUp();

        osc.KeyboardDown(NormalizeNoteName(noteName));
    }

    private void StopOscUnique(List<Osc> stoppedOscs, Osc osc)
    {
        if (osc == null || stoppedOscs.Contains(osc))
            return;

        stoppedOscs.Add(osc);
        osc.KeyboardUp();
    }

    private void ConfigureKickStack()
    {
        ConfigureLayeredDrumOSC(KickOSC, Osc.WaveFormType.Sine, kickPitchOctave, kickAttackMs, kickDecayMs, kickSustainMs, kickSustainLevel);
        ConfigureLayeredDrumOSC(KickNoiseOSC1, Osc.WaveFormType.WhiteNoise, kickNoiseTriggerOctave, kickAttackMs, kickDecayMs, kickSustainMs, kickSustainLevel);
        ConfigureLayeredDrumOSC(KickNoiseOSC2, Osc.WaveFormType.WhiteNoise, kickNoiseTriggerOctave, kickAttackMs, kickDecayMs, kickSustainMs, kickSustainLevel);
    }

    private void ConfigureSnareStack()
    {
        ConfigureLayeredDrumOSC(SnareOSC, Osc.WaveFormType.WhiteNoise, snareNoiseTriggerOctave, snareAttackMs, snareDecayMs, snareSustainMs, snareSustainLevel);
        ConfigureLayeredDrumOSC(SnareNoiseOSC2, Osc.WaveFormType.WhiteNoise, snareNoiseTriggerOctave, snareAttackMs, snareDecayMs, snareSustainMs, snareSustainLevel);
        ApplySnareNoiseBandFilters();
    }

    private void ApplySnareNoiseBandFilters()
    {
        if (!configureSnareNoiseBandFilters)
            return;

        if (SnareLowNoiseFilter != null)
        {
            SnareLowNoiseFilter.enabled = true;
            SnareLowNoiseFilter.cutoffFrequency = Mathf.Clamp(snareLowNoiseCutoff, 200f, 12000f);
        }

        if (SnareHighNoiseFilter != null)
        {
            SnareHighNoiseFilter.enabled = true;
            SnareHighNoiseFilter.cutoffFrequency = Mathf.Clamp(snareHighNoiseCutoff, 200f, 12000f);
        }
    }

    private void ConfigureHiHatOSC()
    {
        if (HiHatOSC == null)
            return;

        HiHatOSC.Octava = 5;
        HiHatOSC.WaveFormChange(Osc.WaveFormType.WhiteNoise);
        HiHatOSC.A = Mathf.Clamp(hiHatAttackMs, 5f, 400f);
        HiHatOSC.D = Mathf.Clamp(hiHatDecayMs, 10f, 1000f);
        HiHatOSC.S = Mathf.Clamp(hiHatSustainMs, 100f, 5000f);
        HiHatOSC.SL = Mathf.Clamp(hiHatSustainLevel, 0.001f, 1f);
        HiHatOSC.R = Mathf.Clamp(hiHatReleaseMs, 100f, 1000f);
        HiHatOSC.UpdateADSR();
    }

    private void ConfigureLayeredDrumOSC(Osc osc, Osc.WaveFormType waveform, int octave, float attackMs, float decayMs, float sustainMs, float sustainLevel)
    {
        if (osc == null)
            return;

        osc.Octava = Mathf.Clamp(octave, 0, 7);

        if (!forceLayeredDrumDefaults)
            return;

        osc.WaveFormChange(waveform);
        osc.A = Mathf.Clamp(attackMs, 5f, 400f);
        osc.D = Mathf.Clamp(decayMs, 10f, 1000f);
        osc.S = Mathf.Clamp(sustainMs, 100f, 5000f);
        osc.SL = Mathf.Clamp(sustainLevel, 0.001f, 1f);
        osc.UpdateADSR();
    }

    private void ApplyInstrumentADSR(TrackRole role, Osc osc, float durationSeconds)
    {
        if (osc == null || currentProfile == null)
            return;

        InstrumentADSRSettings settings = GetADSRForRole(role);
        if (settings == null)
            return;

        ApplyADSRToOSC(role, osc, settings, durationSeconds);
    }

    private void ApplyInstrumentADSRToHarmony(float durationSeconds)
    {
        ApplyInstrumentADSR(TrackRole.Harmony1, HarmonyOSC1, durationSeconds);
        ApplyInstrumentADSR(TrackRole.Harmony2, HarmonyOSC2, durationSeconds);
        ApplyInstrumentADSR(TrackRole.Harmony3, HarmonyOSC3, durationSeconds);
        ApplyInstrumentADSR(TrackRole.Harmony4, HarmonyOSC4, durationSeconds);
    }

    private InstrumentADSRSettings GetADSRForRole(TrackRole role)
    {
        if (currentProfile == null)
            return null;

        switch (role)
        {
            case TrackRole.Melody:
                return currentProfile.melodyADSR;
            case TrackRole.Bass:
                return currentProfile.bassADSR;
            case TrackRole.Harmony1:
            case TrackRole.Harmony2:
            case TrackRole.Harmony3:
            case TrackRole.Harmony4:
                return currentProfile.harmonyADSR;
            default:
                return null;
        }
    }

    private void ApplyADSRToOSC(TrackRole role, Osc osc, InstrumentADSRSettings settings, float durationSeconds)
    {
        if (osc == null || settings == null)
            return;

        float attack = Mathf.Clamp(settings.attackMs, 1f, 400f);
        float decay = Mathf.Clamp(settings.decayMs, 1f, 1000f);
        float sustain = Mathf.Clamp(settings.sustainMs, 0f, 5000f);
        float release = Mathf.Clamp(settings.releaseMs, 1f, 1000f);
        float sustainLevel = Mathf.Clamp(settings.sustainLevel, 0.001f, 1f);
        float sustainPortion = 1f;

        float percussiveAmount;
        float sustainAmount;
        if (TryGetArticulationForRole(role, out percussiveAmount, out sustainAmount))
        {
            float minAttack;
            float maxAttack;
            GetAttackRangeForRole(role, out minAttack, out maxAttack);
            attack = Mathf.Lerp(maxAttack, minAttack, percussiveAmount);
            sustainLevel = Mathf.Lerp(0.08f, 0.92f, sustainAmount);
            sustain = Mathf.Lerp(40f, Mathf.Max(80f, settings.sustainMs * 2f), sustainAmount);
            sustainPortion = Mathf.Lerp(0.18f, 1f, sustainAmount);
        }

        if (settings.fitSustainToNoteDuration && durationSeconds > 0f)
        {
            float durationMs = Mathf.Max(10f, durationSeconds * 1000f);
            float fixedEnvelopeMs = attack + decay + release;

            if (fixedEnvelopeMs > durationMs)
            {
                float scale = durationMs / fixedEnvelopeMs;
                attack = Mathf.Max(1f, attack * scale);
                decay = Mathf.Max(1f, decay * scale);
                release = Mathf.Max(1f, release * scale);
                sustain = 0f;
            }
            else
            {
                float availableSustainMs = Mathf.Max(0f, durationMs - fixedEnvelopeMs);
                sustain = availableSustainMs * sustainPortion;
                release += availableSustainMs * (1f - sustainPortion);
            }
        }

        osc.A = attack;
        osc.D = decay;
        osc.S = sustain;
        osc.SL = sustainLevel;
        osc.R = release;
        osc.UpdateADSR();
    }

    private bool TryGetArticulationForRole(TrackRole role, out float percussiveAmount, out float sustainAmount)
    {
        percussiveAmount = 0f;
        sustainAmount = 0f;

        if (!useMelodicArticulationControls)
            return false;

        switch (role)
        {
            case TrackRole.Melody:
                percussiveAmount = melodyPercussiveAmount;
                sustainAmount = melodySustainAmount;
                return true;

            case TrackRole.Bass:
                percussiveAmount = bassPercussiveAmount;
                sustainAmount = bassSustainAmount;
                return true;

            case TrackRole.Harmony1:
            case TrackRole.Harmony2:
            case TrackRole.Harmony3:
            case TrackRole.Harmony4:
                percussiveAmount = harmonyPercussiveAmount;
                sustainAmount = harmonySustainAmount;
                return true;

            default:
                return false;
        }
    }

    private static void GetAttackRangeForRole(TrackRole role, out float minAttack, out float maxAttack)
    {
        switch (role)
        {
            case TrackRole.Bass:
                minAttack = 3f;
                maxAttack = 130f;
                break;

            case TrackRole.Harmony1:
            case TrackRole.Harmony2:
            case TrackRole.Harmony3:
            case TrackRole.Harmony4:
                minAttack = 8f;
                maxAttack = 320f;
                break;

            default:
                minAttack = 4f;
                maxAttack = 220f;
                break;
        }
    }

    private bool HasAnyKickOSC()
    {
        return KickOSC != null || KickNoiseOSC1 != null || KickNoiseOSC2 != null;
    }

    private bool HasAnySnareOSC()
    {
        return SnareOSC != null || SnareNoiseOSC2 != null;
    }

    private static string NormalizeNoteName(string noteName)
    {
        if (string.IsNullOrEmpty(noteName))
            return "C";

        string trimmed = noteName.Trim();
        if (trimmed.Length == 0)
            return "C";

        string letter = trimmed.Substring(0, 1).ToUpperInvariant();
        bool sharp = trimmed.Length > 1 && trimmed[1] == '#';
        bool flat = trimmed.Length > 1 && (trimmed[1] == 'b' || trimmed[1] == 'B');

        if (flat)
            return FlatToSharp(letter);

        return sharp ? letter + "#" : letter;
    }

    private static string FlatToSharp(string letter)
    {
        switch (letter)
        {
            case "D": return "C#";
            case "E": return "D#";
            case "G": return "F#";
            case "A": return "G#";
            case "B": return "A#";
            default: return letter;
        }
    }

    private static void SetWaveform(Osc osc, Osc.WaveFormType waveform)
    {
        if (osc != null)
            osc.WaveFormChange(waveform);
    }

    private void ApplyLfoDefaults(Osc osc)
    {
        if (osc == null)
            return;

        if (useMelodicLfoControls)
        {
            if (osc == MelodyOSC)
            {
                ApplyLfoToOsc(osc, melodyTremLfoFrequency, melodyVibLfoFrequency);
                return;
            }

            if (osc == HarmonyOSC1 || osc == HarmonyOSC2 || osc == HarmonyOSC3 || osc == HarmonyOSC4)
            {
                ApplyLfoToOsc(osc, harmonyTremLfoFrequency, harmonyVibLfoFrequency);
                return;
            }
        }

        if (!forceLfoValuesToZero)
            return;

        osc.tremLFOf = 0f;
        osc.VibLFOf = 0f;
    }

    private void ApplyLfoForGroup(OSCGroupRole groupRole)
    {
        switch (groupRole)
        {
            case OSCGroupRole.Melody:
                ApplyLfoToOsc(MelodyOSC, melodyTremLfoFrequency, melodyVibLfoFrequency);
                break;

            case OSCGroupRole.Harmony:
                ApplyLfoToOsc(HarmonyOSC1, harmonyTremLfoFrequency, harmonyVibLfoFrequency);
                ApplyLfoToOsc(HarmonyOSC2, harmonyTremLfoFrequency, harmonyVibLfoFrequency);
                ApplyLfoToOsc(HarmonyOSC3, harmonyTremLfoFrequency, harmonyVibLfoFrequency);
                ApplyLfoToOsc(HarmonyOSC4, harmonyTremLfoFrequency, harmonyVibLfoFrequency);
                break;
        }
    }

    private float ClampLfoFrequency(float frequency)
    {
        return Mathf.Clamp(frequency, 0f, Mathf.Max(0f, lfoFrequencySliderMax));
    }

    private static void ApplyLfoToOsc(Osc osc, float tremFrequency, float vibFrequency)
    {
        if (osc == null)
            return;

        osc.tremLFOf = Mathf.Max(0f, tremFrequency);
        osc.VibLFOf = Mathf.Max(0f, vibFrequency);
    }

    private static void ConfigureDrumOSC(Osc osc, int octave)
    {
        if (osc == null)
            return;

        osc.Octava = octave;
    }

    private static void ApplyOscVolume(Osc osc, float velocity, float groupVolume, float layerLevel)
    {
        if (osc == null)
            return;

        osc.UpdateVolume(Mathf.Clamp01(velocity * groupVolume * layerLevel));

        if (osc.OscAudio != null)
            osc.OscAudio.volume = 1f;
    }

    private void ApplySoundControlsToOSC(Osc osc, AudioChorusFilter assignedChorus, AudioDistortionFilter assignedDistortion, Osc.WaveFormType waveform, float detuneCents, float fmLevel, bool useFilter, float filterLevel, bool useDistortion, float distortionAmount)
    {
        if (osc == null)
            return;

        osc.WaveFormChange(waveform);
        osc.detuneCents = Mathf.Clamp(detuneCents, -1200f, 1200f);
        osc.UpdateFMAmount(fmLevel);
        ApplyUnityModulationFilter(osc, assignedChorus, useFilter, filterLevel);
        ApplyUnityDistortionFilter(osc, assignedDistortion, useDistortion, distortionAmount);
    }

    private void ApplyUnityModulationFilter(Osc osc, AudioChorusFilter assignedFilter, bool enabled, float level)
    {
        AudioChorusFilter chorus = ResolveUnityAudioFilter(osc, assignedFilter);
        if (chorus == null)
            return;

        float amount = Mathf.Clamp01(level);
        float shapedAmount = Mathf.Pow(amount, 0.55f);
        float wetMixAtMax = Mathf.Clamp01(chorusWetMixAtMax);
        float dryMixAtMax = Mathf.Clamp01(chorusDryMixAtMax);

        chorus.enabled = enabled && amount > 0.001f;
        chorus.dryMix = Mathf.Lerp(1f, dryMixAtMax, shapedAmount);
        chorus.wetMix1 = Mathf.Lerp(0f, wetMixAtMax, shapedAmount);
        chorus.wetMix2 = Mathf.Lerp(0f, wetMixAtMax, shapedAmount);
        chorus.wetMix3 = Mathf.Lerp(0f, wetMixAtMax, shapedAmount);
        chorus.delay = Mathf.Lerp(6f, chorusDelayAtMax, shapedAmount);
        chorus.rate = Mathf.Lerp(0.5f, chorusRateAtMax, shapedAmount);
        chorus.depth = Mathf.Lerp(0.1f, chorusDepthAtMax, shapedAmount);
    }

    private void ApplyUnityDistortionFilter(Osc osc, AudioDistortionFilter assignedFilter, bool enabled, float amount, float maxAmount = -1f)
    {
        AudioDistortionFilter distortion = ResolveUnityAudioFilter(osc, assignedFilter);
        if (distortion == null)
            return;

        float limit = maxAmount > 0f ? maxAmount : distortionEffectMax;
        distortion.enabled = enabled && amount > 0.001f;
        distortion.distortionLevel = Mathf.Clamp(amount, 0f, Mathf.Max(0f, limit));
    }

    private T ResolveUnityAudioFilter<T>(Osc osc, T assignedFilter) where T : Component
    {
        if (assignedFilter != null)
            return assignedFilter;

        if (!autoFindUnityAudioFilters)
            return null;

        GameObject host = GetAudioFilterHost(osc);
        if (host == null)
            return null;

        return host.GetComponent<T>();
    }

    private static T FindUnityAudioFilter<T>(Osc osc, T currentFilter) where T : Component
    {
        if (currentFilter != null)
            return currentFilter;

        GameObject host = GetAudioFilterHost(osc);
        if (host == null)
            return null;

        return host.GetComponent<T>();
    }

    private static GameObject GetAudioFilterHost(Osc osc)
    {
        if (osc == null)
            return null;

        if (osc.OscAudio != null)
            return osc.OscAudio.gameObject;

        return osc.gameObject;
    }

    private List<Osc> GetUniqueOSCs()
    {
        List<Osc> result = new List<Osc>();
        AddUnique(result, MelodyOSC);
        AddUnique(result, BassOSC);
        AddUnique(result, HarmonyOSC1);
        AddUnique(result, HarmonyOSC2);
        AddUnique(result, HarmonyOSC3);
        AddUnique(result, HarmonyOSC4);
        AddUnique(result, KickOSC);
        AddUnique(result, KickNoiseOSC1);
        AddUnique(result, KickNoiseOSC2);
        AddUnique(result, SnareOSC);
        AddUnique(result, SnareNoiseOSC2);
        AddUnique(result, HiHatOSC);
        return result;
    }

    private static void AddUnique(List<Osc> list, Osc osc)
    {
        if (osc != null && !list.Contains(osc))
            list.Add(osc);
    }
}
