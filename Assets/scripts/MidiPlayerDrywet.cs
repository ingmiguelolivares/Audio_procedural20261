// MidiPlayerDrywet.cs (lee MIDI desde Assets/midifiles/<archivo>.mid)
// Requiere DryWetMIDI.Nativeless
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

public class MidiPlayerDrywet : MonoBehaviour
{
    [Header("MIDI file name (without path)")]
    public string midiFileName = "example.mid"; 
    // El archivo debe estar en: Assets/midifiles/example.mid

    [Header("Default instrument (fallback)")]
    public Polifonia defaultInstrument;   // O Polifonia

    [Serializable]
    public struct ChannelRoute
    {
        [Range(0,15)] public int channel;
        public Polifonia instrument;      // O Polifonia
    }

    [Header("Channel → Instrument routes")]
    public List<ChannelRoute> routes = new List<ChannelRoute>();

    [Header("Transport")]
    public bool playOnStart = true;
    public double startOffsetSeconds = 0.0;

    private class ScheduledNote
    {
        public int channel;
        public int noteNumber;
        public int velocity;
        public double startDspRel;
        public double endDspRel;
    }

    private List<ScheduledNote> _byOn = new();
    private List<ScheduledNote> _byOff = new();
    private int _iOn = 0;
    private int _iOff = 0;
    private bool _prepared = false;
    private bool _playing = false;
    private double _transportStartDsp = 0;

    private struct ActiveKey { public int ch; public int nn; }
    private struct ActiveValue { public object instrument; public string noteName; }
    private Dictionary<ActiveKey, ActiveValue> _active = new();

    private Dictionary<int, object> _routeMap = new();

    void Start()
    {
        BuildChannelRoutes();

        string fullPath = Path.Combine(Application.dataPath, "midifiles", midiFileName);

        if (File.Exists(fullPath))
        {
            PrepareFromMidi(fullPath);
            if (playOnStart) Play();
        }
        else
        {
            Debug.LogWarning($"[MidiPlayerDrywet] No se encontró el archivo MIDI: {fullPath}");
        }
    }

    void Update()
    {
        if (!_playing || !_prepared) return;

        double nowRel = AudioSettings.dspTime - _transportStartDsp;

        while (_iOn < _byOn.Count && _byOn[_iOn].startDspRel <= nowRel)
            FireNoteOn(_byOn[_iOn++]);

        while (_iOff < _byOff.Count && _byOff[_iOff].endDspRel <= nowRel)
            FireNoteOff(_byOff[_iOff++]);

        if (_iOff >= _byOff.Count)
        {
            _playing = false;
            Debug.Log("[MidiPlayerDrywet] Playback finished.");
        }
    }

    // --- Transporte ---
    public void Play()
    {
        if (!_prepared)
        {
            Debug.LogWarning("[MidiPlayerDrywet] Llama a PrepareFromMidi primero.");
            return;
        }
        _transportStartDsp = AudioSettings.dspTime + Math.Max(0.0, startOffsetSeconds);
        _playing = true;
        _iOn = 0; _iOff = 0;
        _active.Clear();
    }

    public void StopAll()
    {
        _playing = false;
        foreach (var kv in _active)
            SafeNoteOff(kv.Value.instrument, kv.Value.noteName);
        _active.Clear();
    }

    // --- Preparación MIDI ---
    public void PrepareFromMidi(string filePath)
    {
        _byOn.Clear();
        _byOff.Clear();

        var midi = MidiFile.Read(filePath);
        var tempoMap = midi.GetTempoMap();

        foreach (var note in midi.GetNotes())
        {
            int ch = note.Channel;
            int nn = note.NoteNumber;
            int vel = note.Velocity;

            var startMts = note.TimeAs<MetricTimeSpan>(tempoMap);
            var lenMts   = note.LengthAs<MetricTimeSpan>(tempoMap);

            double startSec = MetricToSeconds(startMts);
            double endSec   = startSec + MetricToSeconds(lenMts);

            var n = new ScheduledNote
            {
                channel = ch,
                noteNumber = nn,
                velocity = vel,
                startDspRel = startSec,
                endDspRel = endSec
            };

            _byOn.Add(n);
            _byOff.Add(n);
        }

        _byOn.Sort((a, b) => a.startDspRel.CompareTo(b.startDspRel));
        _byOff.Sort((a, b) => a.endDspRel.CompareTo(b.endDspRel));

        _prepared = true;
        Debug.Log($"[MidiPlayerDrywet] Prepared {_byOn.Count} notes from {Path.GetFileName(filePath)}.");
    }

    // --- Utilidades ---
    private void BuildChannelRoutes()
    {
        _routeMap.Clear();
        foreach (var r in routes)
            if (r.instrument != null)
                _routeMap[r.channel] = r.instrument;
    }

    private object ResolveInstrument(int channel)
    {
        if (_routeMap.TryGetValue(channel, out var inst) && inst != null)
            return inst;
        if (defaultInstrument != null) return defaultInstrument;
        return null;
    }

    private static string MidiNoteToName(int midiNote)
    {
        string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int octave = (midiNote / 12) - 1;
        return $"{names[midiNote % 12]}{octave}";
    }

    private static double MetricToSeconds(MetricTimeSpan mts)
    {
        return mts.Hours * 3600.0 + mts.Minutes * 60.0 + mts.Seconds + (mts.Milliseconds / 1000.0);
    }

    private void FireNoteOn(ScheduledNote n)
    {
        var inst = ResolveInstrument(n.channel);
        if (inst == null) return;

        string noteName = MidiNoteToName(n.noteNumber);
        SafeNoteOn(inst, noteName);

        var key = new ActiveKey { ch = n.channel, nn = n.noteNumber };
        _active[key] = new ActiveValue { instrument = inst, noteName = noteName };
    }

    private void FireNoteOff(ScheduledNote n)
    {
        var key = new ActiveKey { ch = n.channel, nn = n.noteNumber };
        if (_active.TryGetValue(key, out var v))
        {
            SafeNoteOff(v.instrument, v.noteName);
            _active.Remove(key);
        }
    }

    private static void SafeNoteOn(object instrument, string noteName)
    {
        if (instrument is Polifoniav2 v2) v2.NoteOn(noteName);
        else if (instrument is Polifonia v1) v1.NoteOn(noteName);
        else Debug.LogWarning($"[MidiPlayerDrywet] Instrument type not supported for NoteOn: {instrument?.GetType()}");
    }

    private static void SafeNoteOff(object instrument, string noteName)
    {
        if (instrument is Polifoniav2 v2) v2.NoteOff(noteName);
        else if (instrument is Polifonia v1) v1.NoteOff(noteName);
        else Debug.LogWarning($"[MidiPlayerDrywet] Instrument type not supported for NoteOff: {instrument?.GetType()}");
    }
}