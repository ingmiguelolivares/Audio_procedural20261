using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class ProceduralSynthVoice : IDisposable
{
    private readonly int sampleRate;
    private readonly int channels;
    private readonly ManagedProceduralSynthVoice managedVoice = new ManagedProceduralSynthVoice();
    private int nativeHandle = -1;
    private bool triedNativeInit;
    private bool useNativeBackend;

    public ProceduralSynthVoice(int sampleRate, int channels)
    {
        this.sampleRate = Mathf.Max(1, sampleRate);
        this.channels = Mathf.Max(1, channels);
    }

    public void UpdateConfig(ProceduralSynthVoiceConfig config)
    {
        if (config == null)
            return;

        config.sampleRate = sampleRate;
        config.channels = channels;
        managedVoice.UpdateConfig(config);

        if (!EnsureNativeVoice())
        {
            return;
        }

        try
        {
            SetCore(nativeHandle, config);

            float[] amplitudes = config.amplitudes ?? Array.Empty<float>();
            SetHarmonics(nativeHandle, amplitudes, Mathf.Min(config.harmonicCount, amplitudes.Length));

            float[] wavetable = config.wavetable ?? Array.Empty<float>();
            SetWavetable(nativeHandle, wavetable, wavetable.Length);

            float[] samplingData = config.samplingData ?? Array.Empty<float>();
            SetSampling(nativeHandle, samplingData, samplingData.Length, config.samplingChannels, config.samplingTotalFrames, config.samplingBaseFrequency, config.samplingStartFrame, config.samplingEndFrame);

            float[] adsrData = config.audioClipAdsrData ?? Array.Empty<float>();
            SetADSRClip(nativeHandle, adsrData, adsrData.Length);
        }
        catch (Exception)
        {
            DisableNativeBackend();
        }
    }

    public void NoteOn(float frequency)
    {
        managedVoice.NoteOn(frequency);

        if (!EnsureNativeVoice())
        {
            return;
        }

        try
        {
            NoteOnNative(nativeHandle, frequency);
        }
        catch (Exception)
        {
            DisableNativeBackend();
        }
    }

    public void NoteOff()
    {
        managedVoice.NoteOff();

        if (!useNativeBackend || nativeHandle < 0)
            return;

        try
        {
            NoteOffNative(nativeHandle);
        }
        catch (Exception)
        {
            DisableNativeBackend();
        }
    }

    public void Render(float[] data)
    {
        if (data == null || data.Length == 0)
            return;

        if (!useNativeBackend || nativeHandle < 0)
        {
            managedVoice.Render(data);
            return;
        }

        try
        {
            RenderNative(nativeHandle, data, data.Length);
        }
        catch (Exception)
        {
            DisableNativeBackend();
            managedVoice.Render(data);
        }
    }

    public void Dispose()
    {
        if (!useNativeBackend || nativeHandle < 0)
            return;

        try
        {
            DestroyVoice(nativeHandle);
        }
        catch (Exception)
        {
        }

        nativeHandle = -1;
        useNativeBackend = false;
    }

    public static void TryUnlockWebAudio()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            UnlockAudioNative();
        }
        catch (Exception)
        {
        }
#endif
    }

    public static bool IsWebAudioUnlocked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            return IsAudioUnlockedNative() != 0;
        }
        catch (Exception)
        {
            return false;
        }
#else
        return true;
#endif
    }

    private bool EnsureNativeVoice()
    {
        if (useNativeBackend && nativeHandle >= 0)
            return true;

        if (triedNativeInit)
            return false;

        triedNativeInit = true;

        try
        {
            nativeHandle = CreateVoice(sampleRate, channels);
            useNativeBackend = nativeHandle >= 0;
        }
        catch (Exception)
        {
            DisableNativeBackend();
        }

        return useNativeBackend;
    }

    private void DisableNativeBackend()
    {
        nativeHandle = -1;
        useNativeBackend = false;
        triedNativeInit = true;
    }

    private static int CreateVoice(int sampleRate, int channels)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return NativeApi.PSW_CreateVoice(sampleRate, channels);
#else
        return NativeApi.PS_CreateVoice(sampleRate, channels);
#endif
    }

    private static void DestroyVoice(int handle)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        NativeApi.PSW_DestroyVoice(handle);
#else
        NativeApi.PS_DestroyVoice(handle);
#endif
    }

    private static void SetCore(int handle, ProceduralSynthVoiceConfig config)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        NativeApi.PSW_SetCore(handle, config.waveform, config.level, config.detuneCents, config.tremLfoFrequency, config.vibLfoFrequency, config.vibratoDepth, config.attackMs, config.decayMs, config.sustainMs, config.sustainLevel, config.releaseMs);
        NativeApi.PSW_SetFM(handle, config.fmMacroAmount, config.fmMinRatio, config.fmMaxRatio, config.fmMaxIndex, config.fmHighHarmonicBlend, config.fmModFrequency, config.fmModIndex);
        NativeApi.PSW_SetFlags(handle, config.useWavetable ? 1 : 0, config.useAudioClipADSR ? 1 : 0);
#else
        NativeApi.PS_SetCore(handle, config.waveform, config.level, config.detuneCents, config.tremLfoFrequency, config.vibLfoFrequency, config.vibratoDepth, config.attackMs, config.decayMs, config.sustainMs, config.sustainLevel, config.releaseMs);
        NativeApi.PS_SetFM(handle, config.fmMacroAmount, config.fmMinRatio, config.fmMaxRatio, config.fmMaxIndex, config.fmHighHarmonicBlend, config.fmModFrequency, config.fmModIndex);
        NativeApi.PS_SetFlags(handle, config.useWavetable ? 1 : 0, config.useAudioClipADSR ? 1 : 0);
#endif
    }

    private static void SetHarmonics(int handle, float[] amplitudes, int count)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        NativeApi.PSW_SetHarmonics(handle, amplitudes, count);
#else
        NativeApi.PS_SetHarmonics(handle, amplitudes, count);
#endif
    }

    private static void SetWavetable(int handle, float[] wavetable, int count)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        NativeApi.PSW_SetWavetable(handle, wavetable, count);
#else
        NativeApi.PS_SetWavetable(handle, wavetable, count);
#endif
    }

    private static void SetSampling(int handle, float[] samplingData, int sampleCount, int channels, int totalFrames, float baseFrequency, int startFrame, int endFrame)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        NativeApi.PSW_SetSampling(handle, samplingData, sampleCount, channels, totalFrames, baseFrequency, startFrame, endFrame);
#else
        NativeApi.PS_SetSampling(handle, samplingData, sampleCount, channels, totalFrames, baseFrequency, startFrame, endFrame);
#endif
    }

    private static void SetADSRClip(int handle, float[] adsrData, int count)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        NativeApi.PSW_SetADSRClip(handle, adsrData, count);
#else
        NativeApi.PS_SetADSRClip(handle, adsrData, count);
#endif
    }

    private static void NoteOnNative(int handle, float frequency)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        NativeApi.PSW_NoteOn(handle, frequency);
#else
        NativeApi.PS_NoteOn(handle, frequency);
#endif
    }

    private static void NoteOffNative(int handle)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        NativeApi.PSW_NoteOff(handle);
#else
        NativeApi.PS_NoteOff(handle);
#endif
    }

    private static void RenderNative(int handle, float[] data, int sampleCount)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        NativeApi.PSW_Render(handle, data, sampleCount);
#else
        NativeApi.PS_Render(handle, data, sampleCount);
#endif
    }

    private static void UnlockAudioNative()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        NativeApi.PSW_UnlockAudio();
#else
        NativeApi.PS_UnlockAudio();
#endif
    }

    private static int IsAudioUnlockedNative()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return NativeApi.PSW_IsAudioUnlocked();
#else
        return NativeApi.PS_IsAudioUnlocked();
#endif
    }

    private static class NativeApi
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private const string PluginName = "__Internal";
        [DllImport(PluginName)] public static extern int PSW_CreateVoice(int sampleRate, int channels);
        [DllImport(PluginName)] public static extern void PSW_DestroyVoice(int handle);
        [DllImport(PluginName)] public static extern void PSW_SetCore(int handle, int waveform, float level, float detuneCents, float tremLfoFrequency, float vibLfoFrequency, float vibratoDepth, float attackMs, float decayMs, float sustainMs, float sustainLevel, float releaseMs);
        [DllImport(PluginName)] public static extern void PSW_SetFM(int handle, float fmMacroAmount, float fmMinRatio, float fmMaxRatio, float fmMaxIndex, float fmHighHarmonicBlend, float fmModFrequency, float fmModIndex);
        [DllImport(PluginName)] public static extern void PSW_SetFlags(int handle, int useWavetable, int useAudioClipADSR);
        [DllImport(PluginName)] public static extern void PSW_SetHarmonics(int handle, float[] amplitudes, int count);
        [DllImport(PluginName)] public static extern void PSW_SetWavetable(int handle, float[] wavetable, int count);
        [DllImport(PluginName)] public static extern void PSW_SetSampling(int handle, float[] samplingData, int sampleCount, int channels, int totalFrames, float baseFrequency, int startFrame, int endFrame);
        [DllImport(PluginName)] public static extern void PSW_SetADSRClip(int handle, float[] adsrData, int count);
        [DllImport(PluginName)] public static extern void PSW_NoteOn(int handle, float frequency);
        [DllImport(PluginName)] public static extern void PSW_NoteOff(int handle);
        [DllImport(PluginName)] public static extern void PSW_Render(int handle, float[] data, int sampleCount);
        [DllImport(PluginName)] public static extern void PSW_UnlockAudio();
        [DllImport(PluginName)] public static extern int PSW_IsAudioUnlocked();
#else
        private const string PluginName = "ProceduralSynth";
        [DllImport(PluginName)] public static extern int PS_CreateVoice(int sampleRate, int channels);
        [DllImport(PluginName)] public static extern void PS_DestroyVoice(int handle);
        [DllImport(PluginName)] public static extern void PS_SetCore(int handle, int waveform, float level, float detuneCents, float tremLfoFrequency, float vibLfoFrequency, float vibratoDepth, float attackMs, float decayMs, float sustainMs, float sustainLevel, float releaseMs);
        [DllImport(PluginName)] public static extern void PS_SetFM(int handle, float fmMacroAmount, float fmMinRatio, float fmMaxRatio, float fmMaxIndex, float fmHighHarmonicBlend, float fmModFrequency, float fmModIndex);
        [DllImport(PluginName)] public static extern void PS_SetFlags(int handle, int useWavetable, int useAudioClipADSR);
        [DllImport(PluginName)] public static extern void PS_SetHarmonics(int handle, float[] amplitudes, int count);
        [DllImport(PluginName)] public static extern void PS_SetWavetable(int handle, float[] wavetable, int count);
        [DllImport(PluginName)] public static extern void PS_SetSampling(int handle, float[] samplingData, int sampleCount, int channels, int totalFrames, float baseFrequency, int startFrame, int endFrame);
        [DllImport(PluginName)] public static extern void PS_SetADSRClip(int handle, float[] adsrData, int count);
        [DllImport(PluginName)] public static extern void PS_NoteOn(int handle, float frequency);
        [DllImport(PluginName)] public static extern void PS_NoteOff(int handle);
        [DllImport(PluginName)] public static extern void PS_Render(int handle, float[] data, int sampleCount);
        [DllImport(PluginName)] public static extern void PS_UnlockAudio();
        [DllImport(PluginName)] public static extern int PS_IsAudioUnlocked();
#endif

    }
}
