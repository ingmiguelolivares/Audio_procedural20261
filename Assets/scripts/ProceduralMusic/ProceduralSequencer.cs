using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralSequencer : MonoBehaviour
{
    public OSCController oscController;
    public bool logPlaybackEvents = false;

    [Header("Looping")]
    public bool loopSong = true;
    public bool stopAllBetweenLoops = true;
    public float loopGapSeconds = 0f;

    private Coroutine playbackRoutine;
    private SongData currentSong;

    public bool IsPlaying
    {
        get { return playbackRoutine != null; }
    }

    public void Play(SongData song, OSCController controller)
    {
        Stop();

        currentSong = song;
        oscController = controller != null ? controller : oscController;

        if (currentSong == null)
        {
            Debug.LogWarning("[ProceduralSequencer] No SongData to play.");
            return;
        }

        if (oscController == null)
        {
            Debug.LogWarning("[ProceduralSequencer] No OSCController assigned. Generated data will not produce sound.");
            return;
        }

        playbackRoutine = StartCoroutine(PlaybackRoutine());
    }

    public void Stop()
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        if (oscController != null)
            oscController.StopAll();
    }

    private IEnumerator PlaybackRoutine()
    {
        List<ScheduledPlaybackEvent> events = BuildSchedule(currentSong);

        if (events.Count == 0 && currentSong.totalBeats <= 0f)
        {
            Debug.LogWarning("[ProceduralSequencer] Current song has no events to play.");
            playbackRoutine = null;
            yield break;
        }

        do
        {
            yield return PlaySongOnce(events);

            if (stopAllBetweenLoops)
                oscController.StopAll();

            if (loopSong && loopGapSeconds > 0f)
                yield return new WaitForSeconds(loopGapSeconds);
        }
        while (loopSong && currentSong != null);

        playbackRoutine = null;
        Debug.Log("[ProceduralSequencer] Playback finished.");
    }

    private IEnumerator PlaySongOnce(List<ScheduledPlaybackEvent> events)
    {
        float lastBeat = 0f;

        for (int i = 0; i < events.Count; i++)
        {
            ScheduledPlaybackEvent evt = events[i];
            float secondsPerBeat = GetSecondsPerBeat();
            float waitBeats = Mathf.Max(0f, evt.startBeat - lastBeat);

            if (waitBeats > 0f)
                yield return new WaitForSeconds(waitBeats * secondsPerBeat);

            secondsPerBeat = GetSecondsPerBeat();

            if (evt.noteEvent != null)
            {
                NoteEvent note = evt.noteEvent;
                float durationSeconds = Mathf.Max(0.01f, note.durationBeats * secondsPerBeat);
                oscController.PlayNote(note.trackRole, note.noteName, note.octave, durationSeconds, note.velocity);

                if (logPlaybackEvents)
                    Debug.Log("[ProceduralSequencer] Note " + note.trackRole + " " + note.noteName + note.octave);
            }
            else if (evt.drumEvent != null)
            {
                DrumEvent drum = evt.drumEvent;
                string noteName;
                int octave;
                GetDrumOSCNote(drum.drumType, out noteName, out octave);
                float durationSeconds = Mathf.Max(0.02f, drum.durationBeats * secondsPerBeat);
                oscController.PlayNote(drum.trackRole, noteName, octave, durationSeconds, drum.velocity);

                if (logPlaybackEvents)
                    Debug.Log("[ProceduralSequencer] Drum " + drum.drumType);
            }

            lastBeat = evt.startBeat;
        }

        float remainingBeats = Mathf.Max(0f, currentSong.totalBeats - lastBeat);
        if (remainingBeats > 0f)
            yield return new WaitForSeconds(remainingBeats * GetSecondsPerBeat());
    }

    private float GetSecondsPerBeat()
    {
        return currentSong == null || currentSong.bpm <= 0 ? 0.5f : 60f / currentSong.bpm;
    }

    private static List<ScheduledPlaybackEvent> BuildSchedule(SongData song)
    {
        List<ScheduledPlaybackEvent> events = new List<ScheduledPlaybackEvent>();

        for (int i = 0; i < song.noteEvents.Count; i++)
        {
            events.Add(new ScheduledPlaybackEvent
            {
                startBeat = song.noteEvents[i].startBeat,
                noteEvent = song.noteEvents[i]
            });
        }

        for (int i = 0; i < song.drumEvents.Count; i++)
        {
            events.Add(new ScheduledPlaybackEvent
            {
                startBeat = song.drumEvents[i].startBeat,
                drumEvent = song.drumEvents[i]
            });
        }

        events.Sort((a, b) => a.startBeat.CompareTo(b.startBeat));
        return events;
    }

    private static void GetDrumOSCNote(DrumType drumType, out string noteName, out int octave)
    {
        switch (drumType)
        {
            case DrumType.Snare:
                noteName = "D";
                octave = 2;
                break;
            case DrumType.HiHat:
                noteName = "B";
                octave = 5;
                break;
            default:
                noteName = "C";
                octave = 1;
                break;
        }
    }

    private class ScheduledPlaybackEvent
    {
        public float startBeat;
        public NoteEvent noteEvent;
        public DrumEvent drumEvent;
    }
}
