#include <cmath>
#include <vector>
#include <unordered_map>
#include <algorithm>

namespace
{
    struct Voice
    {
        int sampleRate = 44100;
        int channels = 2;
        int waveform = 0;
        int harmonicCount = 0;
        bool useWavetable = false;
        bool useAudioClipADSR = false;
        bool active = false;
        float level = 0.5f;
        float frequency = 0.0f;
        float detuneCents = 0.0f;
        float tremLfoFrequency = 0.0f;
        float vibLfoFrequency = 0.0f;
        float vibratoDepth = 5.0f;
        float attackMs = 5.0f;
        float decayMs = 5.0f;
        float sustainMs = 5.0f;
        float sustainLevel = 0.7f;
        float releaseMs = 5.0f;
        float fmMacroAmount = 0.35f;
        float fmMinRatio = 1.0f;
        float fmMaxRatio = 5.0f;
        float fmMaxIndex = 8.0f;
        float fmHighHarmonicBlend = 0.18f;
        float fmModFrequency = 220.0f;
        float fmModIndex = 1.0f;
        float samplingBaseFrequency = 440.0f;
        int samplingChannels = 1;
        int samplingTotalFrames = 0;
        int samplingStartFrame = 0;
        int samplingEndFrame = 0;
        int timeIndex = 0;
        float samplingReadPosition = 0.0f;
        std::vector<float> amplitudes;
        std::vector<float> wavetable;
        std::vector<float> samplingData;
        std::vector<float> adsrData;
    };

    std::unordered_map<int, Voice> voices;
    int nextHandle = 1;

    float Sine(float f, int t, float sampleRate)
    {
        return std::sin(2.0f * 3.14159265359f * f * t / sampleRate);
    }

    float Saw(float f, int t, float sampleRate)
    {
        if (f <= 0.0f) return 0.0f;
        float period = sampleRate / f;
        float pos = std::fmod(static_cast<float>(t), period);
        return 1.0f + ((-2.0f) * (pos / std::max(1.0f, period)));
    }

    float Triangle(float f, int t, float sampleRate)
    {
        if (f <= 0.0f) return 0.0f;
        float period = sampleRate / f;
        float pos = std::fmod(static_cast<float>(t), period);
        float quarter = period * 0.25f;
        float threeQuarters = period * 0.75f;

        if (pos <= quarter) return pos / std::max(1.0f, quarter);
        if (pos <= threeQuarters) return 1.0f - 2.0f * ((pos - quarter) / std::max(1.0f, threeQuarters - quarter));
        return -1.0f + ((pos - threeQuarters) / std::max(1.0f, period - threeQuarters));
    }

    float Additive(const Voice& voice, float f, int t)
    {
        if (voice.harmonicCount <= 0 || voice.amplitudes.empty()) return 0.0f;
        int limit = std::min(voice.harmonicCount, static_cast<int>(voice.amplitudes.size()));
        float sum = 0.0f;
        for (int harmonic = 1; harmonic <= limit; ++harmonic)
            sum += std::sin(2.0f * 3.14159265359f * harmonic * f * t / voice.sampleRate) * voice.amplitudes[harmonic - 1];
        return sum / std::max(1, limit);
    }

    float Fm(const Voice& voice, float carrierFrequency, int t)
    {
        float amount = std::clamp(voice.fmMacroAmount, 0.0f, 1.0f);
        float ratio = voice.fmMinRatio + (voice.fmMaxRatio - voice.fmMinRatio) * amount;
        float modulationIndex = voice.fmMaxIndex * std::max(0.05f, voice.fmModIndex) * amount;
        float modulatorFrequency = std::min(carrierFrequency * ratio, voice.sampleRate * 0.45f);
        float harmonicBlend = voice.fmHighHarmonicBlend * amount;
        float carrierPhase = 2.0f * 3.14159265359f * carrierFrequency * t / voice.sampleRate;
        float modulatorPhase = 2.0f * 3.14159265359f * modulatorFrequency * t / voice.sampleRate;
        float modulation = std::sin(modulatorPhase);
        float primary = std::sin(carrierPhase + modulationIndex * modulation);
        float brightLayer = std::sin((carrierPhase * 2.0f) + (modulationIndex * 0.5f * modulation));
        return primary + (0.18f * harmonicBlend * (brightLayer - primary));
    }

    float Wavetable(const Voice& voice, int t, float frequency)
    {
        if (voice.wavetable.empty()) return 0.0f;
        int length = static_cast<int>(voice.wavetable.size());
        int index = static_cast<int>(std::round((t * frequency / voice.sampleRate) * length)) % length;
        if (index < 0) index += length;
        return voice.wavetable[index];
    }

    float SamplingFrame(const Voice& voice, int frame)
    {
        int channels = std::max(1, voice.samplingChannels);
        int baseIndex = frame * channels;
        if (baseIndex < 0 || baseIndex >= static_cast<int>(voice.samplingData.size())) return 0.0f;
        if (channels == 1) return voice.samplingData[baseIndex];
        float sum = 0.0f;
        for (int channel = 0; channel < channels; ++channel)
        {
            int index = baseIndex + channel;
            if (index >= 0 && index < static_cast<int>(voice.samplingData.size()))
                sum += voice.samplingData[index];
        }
        return sum / channels;
    }

    float Sampling(Voice& voice, float frequency, bool advance)
    {
        if (voice.samplingData.empty() || voice.samplingTotalFrames <= 0) return 0.0f;
        int startFrame = std::clamp(voice.samplingStartFrame, 0, std::max(0, voice.samplingTotalFrames - 1));
        int endFrame = std::clamp(voice.samplingEndFrame, startFrame + 1, std::max(0, voice.samplingTotalFrames - 1));
        if (endFrame <= startFrame) return 0.0f;
        float playbackRatio = frequency / std::max(1.0f, voice.samplingBaseFrequency);
        float samplePosition = voice.samplingReadPosition;
        if (samplePosition >= endFrame) return 0.0f;

        int frame0 = std::clamp(static_cast<int>(std::floor(samplePosition)), startFrame, endFrame);
        int frame1 = std::clamp(frame0 + 1, startFrame, endFrame);
        float frac = samplePosition - frame0;
        float output = SamplingFrame(voice, frame0) + ((SamplingFrame(voice, frame1) - SamplingFrame(voice, frame0)) * frac);

        if (advance) voice.samplingReadPosition += playbackRatio;
        return output;
    }

    float Adsr(const Voice& voice, int t)
    {
        if (voice.useAudioClipADSR && !voice.adsrData.empty())
        {
            if (t < 0 || t >= static_cast<int>(voice.adsrData.size())) return 0.0f;
            return std::clamp(voice.adsrData[t], 0.0f, 1.0f);
        }

        int attack = std::max(1, static_cast<int>((voice.attackMs / 1000.0f) * voice.sampleRate));
        int decay = attack + static_cast<int>((voice.decayMs / 1000.0f) * voice.sampleRate);
        int sustain = decay + static_cast<int>((voice.sustainMs / 1000.0f) * voice.sampleRate);
        int release = sustain + static_cast<int>((voice.releaseMs / 1000.0f) * voice.sampleRate);

        if (t < attack) return static_cast<float>(t) / attack;
        if (t < decay) return 1.0f + ((voice.sustainLevel - 1.0f) * (static_cast<float>(t - attack) / std::max(1, decay - attack)));
        if (t < sustain) return voice.sustainLevel;
        if (t < release) return voice.sustainLevel + ((0.0000001f - voice.sustainLevel) * (static_cast<float>(t - sustain) / std::max(1, release - sustain)));
        return 0.0000001f;
    }

    float Generate(Voice& voice, float frequency, int time, bool advanceSampling)
    {
        if (voice.waveform != 7 && frequency <= 0.0f) return 0.0f;
        if (voice.useWavetable && !voice.wavetable.empty() && voice.waveform != 5) return Wavetable(voice, time, frequency);

        switch (voice.waveform)
        {
            case 0: return Sine(frequency, time, voice.sampleRate);
            case 1: return Sine(frequency, time, voice.sampleRate) >= 0.0f ? 1.0f : -1.0f;
            case 2: return Saw(frequency, time, voice.sampleRate);
            case 3: return Triangle(frequency, time, voice.sampleRate);
            case 4: return Additive(voice, frequency, time);
            case 5: return Fm(voice, frequency, time);
            case 6: return Sampling(voice, frequency, advanceSampling);
            case 7: return static_cast<float>(std::rand()) / RAND_MAX * 2.0f - 1.0f;
            default: return 0.0f;
        }
    }
}

extern "C"
{
    int PS_CreateVoice(int sampleRate, int channels)
    {
        int handle = nextHandle++;
        Voice voice;
        voice.sampleRate = sampleRate;
        voice.channels = channels;
        voices[handle] = voice;
        return handle;
    }

    void PS_DestroyVoice(int handle)
    {
        voices.erase(handle);
    }

    void PS_SetCore(int handle, int waveform, float level, float detuneCents, float tremLfoFrequency, float vibLfoFrequency, float vibratoDepth, float attackMs, float decayMs, float sustainMs, float sustainLevel, float releaseMs)
    {
        auto& voice = voices[handle];
        voice.waveform = waveform;
        voice.level = level;
        voice.detuneCents = detuneCents;
        voice.tremLfoFrequency = tremLfoFrequency;
        voice.vibLfoFrequency = vibLfoFrequency;
        voice.vibratoDepth = vibratoDepth;
        voice.attackMs = attackMs;
        voice.decayMs = decayMs;
        voice.sustainMs = sustainMs;
        voice.sustainLevel = sustainLevel;
        voice.releaseMs = releaseMs;
    }

    void PS_SetFM(int handle, float fmMacroAmount, float fmMinRatio, float fmMaxRatio, float fmMaxIndex, float fmHighHarmonicBlend, float fmModFrequency, float fmModIndex)
    {
        auto& voice = voices[handle];
        voice.fmMacroAmount = fmMacroAmount;
        voice.fmMinRatio = fmMinRatio;
        voice.fmMaxRatio = fmMaxRatio;
        voice.fmMaxIndex = fmMaxIndex;
        voice.fmHighHarmonicBlend = fmHighHarmonicBlend;
        voice.fmModFrequency = fmModFrequency;
        voice.fmModIndex = fmModIndex;
    }

    void PS_SetFlags(int handle, int useWavetable, int useAudioClipADSR)
    {
        auto& voice = voices[handle];
        voice.useWavetable = useWavetable != 0;
        voice.useAudioClipADSR = useAudioClipADSR != 0;
    }

    void PS_SetHarmonics(int handle, float* amplitudes, int count)
    {
        auto& voice = voices[handle];
        voice.harmonicCount = count;
        voice.amplitudes.assign(amplitudes, amplitudes + std::max(0, count));
    }

    void PS_SetWavetable(int handle, float* wavetable, int count)
    {
        auto& voice = voices[handle];
        voice.wavetable.assign(wavetable, wavetable + std::max(0, count));
    }

    void PS_SetSampling(int handle, float* samplingData, int sampleCount, int channels, int totalFrames, float baseFrequency, int startFrame, int endFrame)
    {
        auto& voice = voices[handle];
        voice.samplingChannels = channels;
        voice.samplingTotalFrames = totalFrames;
        voice.samplingBaseFrequency = baseFrequency;
        voice.samplingStartFrame = startFrame;
        voice.samplingEndFrame = endFrame;
        voice.samplingData.assign(samplingData, samplingData + std::max(0, sampleCount));
    }

    void PS_SetADSRClip(int handle, float* adsrData, int count)
    {
        auto& voice = voices[handle];
        voice.adsrData.assign(adsrData, adsrData + std::max(0, count));
    }

    void PS_NoteOn(int handle, float frequency)
    {
        auto& voice = voices[handle];
        voice.frequency = frequency;
        voice.timeIndex = 0;
        voice.samplingReadPosition = static_cast<float>(voice.samplingStartFrame);
        voice.active = true;
    }

    void PS_NoteOff(int handle)
    {
        auto& voice = voices[handle];
        voice.active = false;
        voice.frequency = 0.0f;
        voice.timeIndex = 0;
        voice.samplingReadPosition = static_cast<float>(voice.samplingStartFrame);
    }

    void PS_Render(int handle, float* data, int sampleCount)
    {
        auto& voice = voices[handle];
        if (!voice.active || voice.frequency <= 0.0f)
        {
            std::fill(data, data + sampleCount, 0.0f);
            return;
        }

        int channels = std::max(1, voice.channels);

        for (int i = 0; i < sampleCount; i += channels)
        {
            int currentTime = voice.timeIndex;
            float envelope = Adsr(voice, currentTime);
            float tremolo = voice.tremLfoFrequency > 0.0f ? 0.5f + 0.5f * Sine(voice.tremLfoFrequency, currentTime, static_cast<float>(voice.sampleRate)) : 1.0f;
            float vibrato = voice.vibLfoFrequency > 0.0f ? voice.vibratoDepth * Sine(voice.vibLfoFrequency, currentTime, static_cast<float>(voice.sampleRate)) : 0.0f;
            float currentFrequency = std::max(0.0f, voice.frequency + vibrato);
            float detunedFrequency = currentFrequency * std::pow(2.0f, voice.detuneCents / 1200.0f);
            float x0 = Generate(voice, currentFrequency, currentTime, true);
            float x1 = Generate(voice, detunedFrequency, currentTime, false);
            float sampleValue = voice.level * 0.5f * (x0 + x1) * envelope * tremolo;

            data[i] = sampleValue;
            for (int channel = 1; channel < channels; ++channel)
                data[i + channel] = sampleValue;

            voice.timeIndex++;
        }
    }
}
