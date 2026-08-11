using System;
using UnityEngine;

public class ManagedProceduralSynthVoice
{
    private ProceduralSynthVoiceConfig config = new ProceduralSynthVoiceConfig();
    private int timeIndex;
    private float samplingReadPosition;
    private float frequency;
    private bool active;
    private bool releaseTriggered;
    private int releaseStartTimeIndex;
    private float releaseStartLevel;
    private readonly System.Random rng = new System.Random();

    public void UpdateConfig(ProceduralSynthVoiceConfig nextConfig)
    {
        config = nextConfig ?? new ProceduralSynthVoiceConfig();
    }

    public void NoteOn(float nextFrequency)
    {
        frequency = Mathf.Max(0f, nextFrequency);
        timeIndex = 0;
        samplingReadPosition = config.samplingStartFrame;
        active = true;
        releaseTriggered = false;
        releaseStartTimeIndex = 0;
        releaseStartLevel = 0f;
    }

    public void NoteOff()
    {
        if (!active || releaseTriggered)
            return;

        releaseStartTimeIndex = timeIndex;
        releaseStartLevel = GetScheduledAdsr(timeIndex, Mathf.Max(1, config.sampleRate));
        releaseTriggered = true;
    }

    public void Render(float[] data)
    {
        if (data == null || data.Length == 0)
            return;

        if (!active || frequency <= 0f)
        {
            Array.Clear(data, 0, data.Length);
            return;
        }

        int channels = Mathf.Max(1, config.channels);
        float sampleRate = Mathf.Max(1, config.sampleRate);

        for (int i = 0; i < data.Length; i += channels)
        {
            int currentTime = timeIndex;
            float envelope = GetAdsr(currentTime, sampleRate);
            float tremolo = config.tremLfoFrequency > 0f ? 0.5f + 0.5f * Sine(config.tremLfoFrequency, currentTime, sampleRate) : 1f;
            float vibrato = config.vibLfoFrequency > 0f ? config.vibratoDepth * Sine(config.vibLfoFrequency, currentTime, sampleRate) : 0f;

            float currentFrequency = Mathf.Max(0f, frequency + vibrato);
            float detuneFactor = Mathf.Pow(2f, config.detuneCents / 1200f);
            float detunedFrequency = currentFrequency * detuneFactor;

            float x0 = GenerateSample(currentFrequency, currentTime, sampleRate, true);
            float x1 = GenerateSample(detunedFrequency, currentTime, sampleRate, false);
            float sampleValue = config.level * 0.5f * (x0 + x1) * envelope * tremolo;

            data[i] = sampleValue;

            for (int ch = 1; ch < channels; ch++)
                data[i + ch] = sampleValue;

            timeIndex++;

            if (ShouldStop(currentTime, sampleRate))
            {
                active = false;
                frequency = 0f;
                samplingReadPosition = config.samplingStartFrame;

                int remaining = data.Length - (i + channels);
                if (remaining > 0)
                    Array.Clear(data, i + channels, remaining);

                break;
            }
        }
    }

    private float GenerateSample(float currentFrequency, int currentTime, float sampleRate, bool advanceSampling)
    {
        if (config.waveform != (int)Osc.WaveFormType.WhiteNoise && currentFrequency <= 0f)
            return 0f;

        if (config.useWavetable && config.wavetable != null && config.wavetable.Length > 0 && config.waveform != (int)Osc.WaveFormType.FM)
            return GetWavetableSample(currentTime, currentFrequency, sampleRate);

        switch ((Osc.WaveFormType)config.waveform)
        {
            case Osc.WaveFormType.Sine:
                return Sine(currentFrequency, currentTime, sampleRate);
            case Osc.WaveFormType.Square:
                return Mathf.Sign(Sine(currentFrequency, currentTime, sampleRate));
            case Osc.WaveFormType.Sawtooth:
                return Saw(currentFrequency, currentTime, sampleRate);
            case Osc.WaveFormType.Triangle:
                return Triangle(currentFrequency, currentTime, sampleRate);
            case Osc.WaveFormType.SA:
                return Additive(currentFrequency, currentTime, sampleRate);
            case Osc.WaveFormType.FM:
                return Fm(currentFrequency, currentTime, sampleRate);
            case Osc.WaveFormType.Sampling1:
                return Sampling(currentFrequency, advanceSampling);
            case Osc.WaveFormType.WhiteNoise:
                return (float)rng.NextDouble() * 2f - 1f;
            default:
                return 0f;
        }
    }

    private float Sine(float f, int t, float sr)
    {
        return Mathf.Sin(2f * Mathf.PI * f * t / sr);
    }

    private float Saw(float f, int t, float sr)
    {
        if (f <= 0f)
            return 0f;

        float period = sr / f;
        float pos = t % period;
        return Mathf.Lerp(1f, -1f, pos / Mathf.Max(1f, period));
    }

    private float Triangle(float f, int t, float sr)
    {
        if (f <= 0f)
            return 0f;

        float period = sr / f;
        float pos = t % period;
        float quarter = period * 0.25f;
        float threeQuarters = period * 0.75f;

        if (pos <= quarter)
            return Mathf.Lerp(0f, 1f, pos / Mathf.Max(1f, quarter));
        if (pos <= threeQuarters)
            return Mathf.Lerp(1f, -1f, (pos - quarter) / Mathf.Max(1f, threeQuarters - quarter));
        return Mathf.Lerp(-1f, 0f, (pos - threeQuarters) / Mathf.Max(1f, period - threeQuarters));
    }

    private float Additive(float f, int t, float sr)
    {
        if (config.harmonicCount <= 0 || config.amplitudes == null || config.amplitudes.Length == 0)
            return 0f;

        float sum = 0f;
        int limit = Mathf.Min(config.harmonicCount, config.amplitudes.Length);

        for (int harmonic = 1; harmonic <= limit; harmonic++)
            sum += Mathf.Sin(2f * Mathf.PI * harmonic * f * t / sr) * config.amplitudes[harmonic - 1];

        return sum / Mathf.Max(1, limit);
    }

    private float Fm(float carrierFrequency, int t, float sr)
    {
        float amount = Mathf.Clamp01(config.fmMacroAmount);
        float shapedAmount = Mathf.SmoothStep(0f, 1f, amount);
        float ratio = Mathf.Lerp(Mathf.Max(0.25f, config.fmMinRatio), Mathf.Max(0.25f, config.fmMaxRatio), shapedAmount);
        float indexLimit = Mathf.Max(0f, config.fmMaxIndex * Mathf.Max(0.05f, config.fmModIndex));
        float modulationIndex = Mathf.Lerp(0f, indexLimit, Mathf.Pow(amount, 0.8f));
        float modulatorFrequency = Mathf.Min(carrierFrequency * ratio, sr * 0.45f);
        float harmonicBlend = Mathf.Lerp(0f, Mathf.Clamp01(config.fmHighHarmonicBlend), shapedAmount);

        float carrierPhase = 2f * Mathf.PI * carrierFrequency * t / sr;
        float modulatorPhase = 2f * Mathf.PI * modulatorFrequency * t / sr;
        float modulation = Mathf.Sin(modulatorPhase);
        float primary = Mathf.Sin(carrierPhase + modulationIndex * modulation);
        float brightLayer = Mathf.Sin((carrierPhase * 2f) + (modulationIndex * 0.5f * modulation));
        return Mathf.Lerp(primary, 0.82f * primary + 0.18f * brightLayer, harmonicBlend);
    }

    private float GetWavetableSample(int t, float f, float sr)
    {
        if (config.wavetable == null || config.wavetable.Length == 0)
            return 0f;

        if (config.waveform != (int)Osc.WaveFormType.WhiteNoise && f <= 0f)
            return 0f;

        int length = config.wavetable.Length;
        int index = Mathf.RoundToInt((t * f / sr) * length) % length;

        if (index < 0)
            index += length;

        return config.wavetable[index];
    }

    private float Sampling(float currentFrequency, bool advanceReadPosition)
    {
        if (config.samplingData == null || config.samplingData.Length == 0 || config.samplingTotalFrames <= 0)
            return 0f;

        int startFrame = Mathf.Clamp(config.samplingStartFrame, 0, Mathf.Max(0, config.samplingTotalFrames - 1));
        int endFrame = Mathf.Clamp(config.samplingEndFrame, startFrame + 1, config.samplingTotalFrames - 1);
        if (endFrame <= startFrame)
            return 0f;

        float playbackRatio = currentFrequency / Mathf.Max(1f, config.samplingBaseFrequency);
        float samplePosition = samplingReadPosition;

        if (samplePosition >= endFrame)
            return 0f;

        int frame0 = Mathf.Clamp(Mathf.FloorToInt(samplePosition), startFrame, endFrame);
        int frame1 = Mathf.Clamp(frame0 + 1, startFrame, endFrame);
        float frac = samplePosition - frame0;

        float value0 = ReadSamplingFrame(frame0);
        float value1 = ReadSamplingFrame(frame1);
        float output = Mathf.Lerp(value0, value1, frac);

        if (advanceReadPosition)
            samplingReadPosition += playbackRatio;

        return output;
    }

    private float ReadSamplingFrame(int frame)
    {
        int channels = Mathf.Max(1, config.samplingChannels);
        int baseIndex = frame * channels;

        if (baseIndex < 0 || baseIndex >= config.samplingData.Length)
            return 0f;

        if (channels == 1)
            return config.samplingData[baseIndex];

        float sum = 0f;
        int count = 0;

        for (int channel = 0; channel < channels; channel++)
        {
            int index = baseIndex + channel;
            if (index >= 0 && index < config.samplingData.Length)
            {
                sum += config.samplingData[index];
                count++;
            }
        }

        return count > 0 ? sum / count : 0f;
    }

    private float GetAdsr(int t, float sr)
    {
        if (releaseTriggered && t >= releaseStartTimeIndex)
        {
            int releaseFrames = Mathf.Max(1, Mathf.RoundToInt((config.releaseMs / 1000f) * sr));
            float releaseProgress = (float)(t - releaseStartTimeIndex) / releaseFrames;
            return Mathf.Lerp(releaseStartLevel, 0.0000001f, EvaluateCurve01(releaseProgress, config.releaseUsesLogCurve));
        }

        return GetScheduledAdsr(t, sr);
    }

    private float GetScheduledAdsr(int t, float sr)
    {
        if (config.useAudioClipADSR && config.audioClipAdsrData != null && config.audioClipAdsrData.Length > 0)
        {
            if (t < 0 || t >= config.audioClipAdsrData.Length)
                return 0f;

            return Mathf.Clamp01(config.audioClipAdsrData[t]);
        }

        int attack = Mathf.Max(1, Mathf.RoundToInt((config.attackMs / 1000f) * sr));
        int decay = attack + Mathf.RoundToInt((config.decayMs / 1000f) * sr);
        int sustain = decay + Mathf.RoundToInt((config.sustainMs / 1000f) * sr);
        int release = sustain + Mathf.RoundToInt((config.releaseMs / 1000f) * sr);

        if (t < attack)
            return EvaluateCurve01((float)t / attack, config.attackUsesLogCurve);
        if (t < decay)
            return Mathf.Lerp(1f, config.sustainLevel, EvaluateCurve01((float)(t - attack) / Mathf.Max(1, decay - attack), config.decayUsesLogCurve));
        if (t < sustain)
            return config.sustainLevel;
        if (t < release)
            return Mathf.Lerp(config.sustainLevel, 0.0000001f, EvaluateCurve01((float)(t - sustain) / Mathf.Max(1, release - sustain), config.releaseUsesLogCurve));

        return 0.0000001f;
    }

    private bool ShouldStop(int t, float sr)
    {
        if (releaseTriggered)
        {
            int releaseFrames = Mathf.Max(1, Mathf.RoundToInt((config.releaseMs / 1000f) * sr));
            return (t - releaseStartTimeIndex) >= releaseFrames;
        }

        int attack = Mathf.Max(1, Mathf.RoundToInt((config.attackMs / 1000f) * sr));
        int decay = attack + Mathf.RoundToInt((config.decayMs / 1000f) * sr);
        int sustain = decay + Mathf.RoundToInt((config.sustainMs / 1000f) * sr);
        int release = sustain + Mathf.RoundToInt((config.releaseMs / 1000f) * sr);
        return t >= release;
    }

    private static float EvaluateCurve01(float progress, bool useLogCurve)
    {
        progress = Mathf.Clamp01(progress);

        if (!useLogCurve)
            return progress;

        return Mathf.Log10(1f + (9f * progress));
    }
}
