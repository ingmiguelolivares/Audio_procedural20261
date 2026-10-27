# Procedural Audio System

This folder identifies the reusable procedural-audio system exported as
`Exports/ProceduralAudioSystem_Legacy_and_NativeDSP.unitypackage`.

## Included implementations

- **Legacy**: `Assets/scripts/OSC.cs` and the sample scene
  `Assets/Scenes/SampleScene.unity`.
- **Native DSP**: `Assets/scripts/ProceduralAudio/NativeDSP/` and the sample
  scene `Assets/Scenes/Runtime/test_NativeDSP.unity`.

The package also contains the oscillator prefabs, source/plugin bridge for
WebGL, a compiled Apple Silicon macOS plugin, wavetables, MIDI support,
DryWetMIDI, UI resources, and sample audio.

## After importing

1. Open either sample scene.
2. Let Unity complete script compilation and asset import.
3. For the Native DSP path, use WebGL to exercise
   `Assets/Plugins/WebGL/ProceduralSynth.jslib`. The managed fallback remains
   available where no native backend can be initialized.
4. If using MIDI, keep the `Assets/midifiles/` path; `MidiPlayerDrywet` loads
   files from that location at runtime.
