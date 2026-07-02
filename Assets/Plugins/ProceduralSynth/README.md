# ProceduralSynth backend / Backend ProceduralSynth

## Español

### Resumen

Este proyecto tiene **dos rutas de síntesis procedural en tiempo real**:

1. **Versión legacy**
   - Genera el audio dentro de Unity usando `OnAudioFilterRead`.
   - Vive principalmente en `Assets/scripts/OSC.cs` y en el modo legacy de `Assets/scripts/new/Osc.cs`.
   - Es útil para editor y desktop, pero no es una buena base para WebGL.

2. **Versión nueva con plugin**
   - Mantiene la lógica musical en C#, pero mueve el motor de síntesis a un backend externo.
   - En plataformas nativas usa `Assets/Plugins/ProceduralSynth/Source/ProceduralSynth.cpp`.
   - En WebGL usa `Assets/Plugins/WebGL/ProceduralSynth.jslib`.
   - El puente desde Unity está en `Assets/scripts/new/ProceduralSynthVoice.cs`.

La escena y scripts migrados usan `Assets/scripts/new/Osc.cs`, que puede operar en ambos modos mediante `AudioEngineBackend`.

---

### Arquitectura general

#### 1. Capa de control musical

- `Assets/scripts/new/Osc.cs`
  - Expone parámetros del oscilador en el Inspector.
  - Convierte notas (`C`, `D`, `E`, etc.) a frecuencia con `KeyboardDown(...)`.
  - Mantiene estado musical: frecuencia, octava, ADSR, armónicos, LFO, detune, sampling.
  - Decide si renderizar por ruta **legacy** o por ruta **plugin**.

- Scripts de gameplay y secuenciación
  - `Assets/scripts/SoundManager1.cs`
  - `Assets/scripts/StartCounter.cs`
  - `Assets/scripts/PlayerController.cs`
  - Estos scripts no sintetizan audio directamente; sólo disparan notas, cambian mutes y modifican parámetros.

#### 2. Capa de configuración de voz

- `Assets/scripts/new/ProceduralSynthVoiceConfig.cs`
  - Es el contenedor de parámetros que serializa la intención sonora de `Osc`.
  - Incluye:
    - forma de onda,
    - armónicos,
    - nivel,
    - detune,
    - tremolo y vibrato,
    - ADSR,
    - parámetros FM,
    - datos de wavetable,
    - datos de sampling,
    - ADSR derivado de un `AudioClip`.

#### 3. Capa de puente

- `Assets/scripts/new/ProceduralSynthVoice.cs`
  - Es la fachada única para el backend externo.
  - Responsabilidades:
    - crear y destruir voces,
    - enviar configuración,
    - disparar `NoteOn` / `NoteOff`,
    - renderizar audio si el backend lo requiere,
    - desbloquear audio en WebGL.
  - En WebGL llama funciones `PSW_*`.
  - En desktop/mobile nativo llama funciones `PS_*`.
  - Si el backend externo falla, cae al motor administrado `ManagedProceduralSynthVoice`.

#### 4. Fallback administrado

- `Assets/scripts/new/ManagedProceduralSynthVoice.cs`
  - Replica la síntesis principal completamente en C#.
  - Se usa como respaldo cuando el backend nativo no existe o falla.
  - También sirve como referencia funcional del algoritmo implementado en C++ y JS.

---

### Módulos de síntesis en tiempo real

#### 1. Formas de onda básicas

Implementadas en:
- `Assets/scripts/new/Osc.cs`
- `Assets/scripts/new/ManagedProceduralSynthVoice.cs`
- `Assets/Plugins/ProceduralSynth/Source/ProceduralSynth.cpp`
- `Assets/Plugins/WebGL/ProceduralSynth.jslib`

Soportan:
- `Sine`
- `Square`
- `Sawtooth`
- `Triangle`
- `WhiteNoise`

Funcionamiento:
- Se genera una muestra por frame de audio a partir de frecuencia y sample rate.
- `Square`, `Sawtooth` y `Triangle` se derivan matemáticamente a partir de fase/periodo.
- `WhiteNoise` genera un valor aleatorio por muestra.

#### 2. Síntesis aditiva (`SA`)

Implementada como suma de armónicos ponderados.

Parámetros principales:
- `Narmonicos`
- `AmplitudesLv`

Funcionamiento:
- Para cada muestra se suman senoides `n * frecuencia`.
- Cada armónico usa su amplitud correspondiente.
- La salida se normaliza por el número de armónicos activos.

#### 3. Wavetables

Responsable principal:
- `Assets/scripts/new/Osc.cs`

Apoyo en runtime:
- `ManagedProceduralSynthVoice.cs`
- `ProceduralSynth.cpp`
- `ProceduralSynth.jslib`

Funcionamiento:
- `Osc` puede generar o cargar una wavetable.
- En tiempo real, la voz indexa la tabla según frecuencia y tiempo.
- Puede usar:
  - tablas internas,
  - loaders externos de texto (`WavetableTxtLoader`),
  - tablas derivadas para sampling/custom cuando corresponde.

#### 4. Síntesis FM

Parámetros principales:
- `fmMacroAmount`
- `fmMinRatio`
- `fmMaxRatio`
- `fmMaxIndex`
- `fmHighHarmonicBlend`
- `fmModFrequency`
- `fmModIndex`

Funcionamiento:
- La frecuencia portadora se modula con una señal senoidal.
- El macro controla la relación portadora/moduladora y el índice efectivo.
- Se agrega una capa brillante opcional para enriquecer armónicos altos.

#### 5. Sampling / reproducción tonal de muestras

Parámetros principales:
- `samplingClip`
- `samplingBaseFrequency`
- `samplingStartFrame`
- `samplingEndFrame`
- `samplingChannels`

Funcionamiento:
- Se copia el `AudioClip` a un buffer flotante.
- La reproducción avanza según la relación entre la frecuencia pedida y la frecuencia base del sample.
- Se hace interpolación lineal entre frames.
- Puede limitarse a un segmento del sample.

#### 6. ADSR procedural

Parámetros principales:
- `A`
- `D`
- `S`
- `SL`
- `R`

Funcionamiento:
- El nivel de amplitud se calcula por etapas:
  - Attack,
  - Decay,
  - Sustain,
  - Release.
- La envolvente modula la salida final de la voz muestra a muestra.

#### 7. ADSR derivado de `AudioClip`

Parámetros:
- `adsrSourceClip`
- `useAudioClipADSR`

Funcionamiento:
- `Osc` construye una envolvente positiva normalizada desde un `AudioClip`.
- Esa envolvente reemplaza el ADSR procedural cuando está activa.

#### 8. LFOs

Soportados:
- **Tremolo** por `tremLFOf`
- **Vibrato** por `VibLFOf` + `vibratoDepth`

Funcionamiento:
- Tremolo modula amplitud.
- Vibrato modula frecuencia instantánea antes de sintetizar la muestra.

#### 9. Detune

Parámetro:
- `detuneCents`

Funcionamiento:
- Cada voz mezcla una frecuencia principal y una detuneada.
- La frecuencia secundaria se calcula con factor `2^(cents/1200)`.

---

### Diferencias entre la versión legacy y la versión nueva

#### Legacy (`OnAudioFilterRead`)

**Ruta**
- `Assets/scripts/OSC.cs`
- `Assets/scripts/new/Osc.cs` cuando `audioEngineBackend = LegacyOnAudioFilterRead`

**Cómo trabaja**
- Unity llama `OnAudioFilterRead(float[] data, int channels)`.
- El oscilador llena directamente el buffer de audio.
- Toda la síntesis ocurre dentro del script C# del componente.

**Ventajas**
- Implementación simple.
- Fácil de depurar dentro de Unity.
- No depende de plugins externos.

**Limitaciones**
- Fuerte dependencia del callback de audio de Unity.
- Más acoplamiento entre gameplay, oscilador y render de audio.
- Menor portabilidad real para WebGL.
- Más difícil compartir exactamente el mismo motor entre plataformas.

#### Nueva versión con plugin externo

**Ruta**
- `Assets/scripts/new/Osc.cs`
- `Assets/scripts/new/ProceduralSynthVoice.cs`
- `Assets/Plugins/ProceduralSynth/Source/ProceduralSynth.cpp`
- `Assets/Plugins/WebGL/ProceduralSynth.jslib`

**Cómo trabaja**
- `Osc` prepara un `ProceduralSynthVoiceConfig`.
- `ProceduralSynthVoice` envía esa configuración al backend.
- El backend genera las muestras:
  - C++ en native platforms,
  - JavaScript/WebAudio en WebGL.

**Ventajas**
- Motor de síntesis desacoplado de Unity `OnAudioFilterRead`.
- Misma API de control desde C# para native y WebGL.
- Mejor base para Android, iOS, desktop y especialmente WebGL.
- Más fácil mantener consistencia de DSP entre plataformas.

**Limitaciones**
- Mayor complejidad de integración.
- Requiere mantener dos implementaciones de runtime:
  - C++,
  - `.jslib` para WebGL.
- Necesita más cuidado al depurar llamadas cruzadas C# ↔ plugin.

---

### Flujo de ejecución en la versión nueva

1. Un script de juego llama `KeyboardDown("C")` sobre `Osc`.
2. `Osc` convierte la nota a frecuencia.
3. `Osc` actualiza / sincroniza `ProceduralSynthVoiceConfig`.
4. `ProceduralSynthVoice` crea o reutiliza una voz del backend.
5. El backend recibe:
   - core synthesis params,
   - FM params,
   - harmonics,
   - wavetable,
   - sampling data,
   - ADSR data.
6. `NoteOn` activa la voz.
7. El backend mezcla audio en tiempo real:
   - native plugin en C++,
   - WebAudio `ScriptProcessor` en WebGL.

---

### Archivos clave

- `Assets/scripts/new/Osc.cs`
- `Assets/scripts/new/ProceduralSynthVoice.cs`
- `Assets/scripts/new/ProceduralSynthVoiceConfig.cs`
- `Assets/scripts/new/ManagedProceduralSynthVoice.cs`
- `Assets/Plugins/ProceduralSynth/Source/ProceduralSynth.cpp`
- `Assets/Plugins/WebGL/ProceduralSynth.jslib`

---

## English

### Overview

This project has **two real-time procedural synthesis paths**:

1. **Legacy version**
   - Generates audio inside Unity through `OnAudioFilterRead`.
   - Lives mainly in `Assets/scripts/OSC.cs` and in the legacy mode of `Assets/scripts/new/Osc.cs`.
   - Useful for editor and desktop, but not ideal as a WebGL foundation.

2. **New plugin-based version**
   - Keeps musical/game logic in C#, but moves the synthesis engine to an external backend.
   - Uses `Assets/Plugins/ProceduralSynth/Source/ProceduralSynth.cpp` on native platforms.
   - Uses `Assets/Plugins/WebGL/ProceduralSynth.jslib` on WebGL.
   - The Unity bridge lives in `Assets/scripts/new/ProceduralSynthVoice.cs`.

Migrated scenes and scripts use `Assets/scripts/new/Osc.cs`, which can switch between both paths using `AudioEngineBackend`.

---

### General architecture

#### 1. Musical control layer

- `Assets/scripts/new/Osc.cs`
  - Exposes oscillator parameters in the Inspector.
  - Converts notes (`C`, `D`, `E`, etc.) into frequency via `KeyboardDown(...)`.
  - Stores musical state: frequency, octave, ADSR, harmonics, LFO, detune, sampling.
  - Decides whether to render through the **legacy** path or the **plugin** path.

- Gameplay and sequencing scripts
  - `Assets/scripts/SoundManager1.cs`
  - `Assets/scripts/StartCounter.cs`
  - `Assets/scripts/PlayerController.cs`
  - These scripts do not synthesize audio directly; they trigger notes, toggle mutes and change parameters.

#### 2. Voice configuration layer

- `Assets/scripts/new/ProceduralSynthVoiceConfig.cs`
  - Parameter container that serializes the sonic intent from `Osc`.
  - Includes:
    - waveform,
    - harmonics,
    - level,
    - detune,
    - tremolo and vibrato,
    - ADSR,
    - FM parameters,
    - wavetable data,
    - sampling data,
    - ADSR derived from an `AudioClip`.

#### 3. Bridge layer

- `Assets/scripts/new/ProceduralSynthVoice.cs`
  - Single façade for the external backend.
  - Responsibilities:
    - create and destroy voices,
    - push configuration,
    - trigger `NoteOn` / `NoteOff`,
    - render audio when required,
    - unlock audio in WebGL.
  - Calls `PSW_*` on WebGL.
  - Calls `PS_*` on native desktop/mobile.
  - Falls back to `ManagedProceduralSynthVoice` if the external backend is missing or fails.

#### 4. Managed fallback

- `Assets/scripts/new/ManagedProceduralSynthVoice.cs`
  - Re-implements the main synthesis path entirely in C#.
  - Used as fallback when the native backend is unavailable.
  - Also acts as a functional reference for the C++ and JS implementations.

---

### Real-time synthesis modules

#### 1. Basic waveforms

Implemented in:
- `Assets/scripts/new/Osc.cs`
- `Assets/scripts/new/ManagedProceduralSynthVoice.cs`
- `Assets/Plugins/ProceduralSynth/Source/ProceduralSynth.cpp`
- `Assets/Plugins/WebGL/ProceduralSynth.jslib`

Supported:
- `Sine`
- `Square`
- `Sawtooth`
- `Triangle`
- `WhiteNoise`

How it works:
- One sample is generated per audio frame from frequency and sample rate.
- `Square`, `Sawtooth` and `Triangle` are mathematically derived from phase/period.
- `WhiteNoise` generates one random value per sample.

#### 2. Additive synthesis (`SA`)

Implemented as a weighted sum of harmonics.

Main parameters:
- `Narmonicos`
- `AmplitudesLv`

How it works:
- For each sample, the engine sums sine waves at `n * frequency`.
- Each harmonic uses its corresponding amplitude.
- The output is normalized by the number of active harmonics.

#### 3. Wavetables

Main owner:
- `Assets/scripts/new/Osc.cs`

Runtime support:
- `ManagedProceduralSynthVoice.cs`
- `ProceduralSynth.cpp`
- `ProceduralSynth.jslib`

How it works:
- `Osc` can generate or load a wavetable.
- At runtime, the voice indexes the table according to frequency and time.
- It can use:
  - internal tables,
  - external text loaders (`WavetableTxtLoader`),
  - derived tables for sampling/custom modes when needed.

#### 4. FM synthesis

Main parameters:
- `fmMacroAmount`
- `fmMinRatio`
- `fmMaxRatio`
- `fmMaxIndex`
- `fmHighHarmonicBlend`
- `fmModFrequency`
- `fmModIndex`

How it works:
- The carrier frequency is modulated by a sine modulator.
- The macro controls carrier/modulator ratio and effective modulation index.
- An optional bright layer adds high-frequency richness.

#### 5. Sampling / pitchable sample playback

Main parameters:
- `samplingClip`
- `samplingBaseFrequency`
- `samplingStartFrame`
- `samplingEndFrame`
- `samplingChannels`

How it works:
- The `AudioClip` is copied into a float buffer.
- Playback advances according to the ratio between target frequency and the sample base frequency.
- Linear interpolation is applied between frames.
- Playback can be limited to a selected segment.

#### 6. Procedural ADSR

Main parameters:
- `A`
- `D`
- `S`
- `SL`
- `R`

How it works:
- The amplitude level is computed in stages:
  - Attack,
  - Decay,
  - Sustain,
  - Release.
- The envelope modulates the final voice output sample by sample.

#### 7. ADSR derived from `AudioClip`

Parameters:
- `adsrSourceClip`
- `useAudioClipADSR`

How it works:
- `Osc` builds a normalized positive envelope from an `AudioClip`.
- That envelope replaces procedural ADSR when enabled.

#### 8. LFOs

Supported:
- **Tremolo** via `tremLFOf`
- **Vibrato** via `VibLFOf` + `vibratoDepth`

How it works:
- Tremolo modulates amplitude.
- Vibrato modulates instantaneous frequency before sample synthesis.

#### 9. Detune

Parameter:
- `detuneCents`

How it works:
- Each voice mixes a main frequency and a detuned one.
- The secondary frequency is computed with `2^(cents/1200)`.

---

### Differences between legacy and new plugin version

#### Legacy (`OnAudioFilterRead`)

**Path**
- `Assets/scripts/OSC.cs`
- `Assets/scripts/new/Osc.cs` when `audioEngineBackend = LegacyOnAudioFilterRead`

**How it works**
- Unity calls `OnAudioFilterRead(float[] data, int channels)`.
- The oscillator fills the audio buffer directly.
- All synthesis happens inside the component C# script.

**Advantages**
- Simple implementation.
- Easy to debug inside Unity.
- No external plugin dependency.

**Limitations**
- Strong dependency on Unity audio callback behavior.
- More coupling between gameplay, oscillator and audio rendering.
- Poorer real portability for WebGL.
- Harder to share exactly the same DSP engine across platforms.

#### New external plugin version

**Path**
- `Assets/scripts/new/Osc.cs`
- `Assets/scripts/new/ProceduralSynthVoice.cs`
- `Assets/Plugins/ProceduralSynth/Source/ProceduralSynth.cpp`
- `Assets/Plugins/WebGL/ProceduralSynth.jslib`

**How it works**
- `Osc` builds a `ProceduralSynthVoiceConfig`.
- `ProceduralSynthVoice` sends that configuration to the backend.
- The backend generates audio samples:
  - C++ on native platforms,
  - JavaScript/WebAudio on WebGL.

**Advantages**
- Synthesis engine is decoupled from Unity `OnAudioFilterRead`.
- Same C# control API for native and WebGL.
- Better foundation for Android, iOS, desktop and especially WebGL.
- Easier to keep DSP behavior consistent across platforms.

**Limitations**
- Higher integration complexity.
- Requires maintaining two runtime implementations:
  - C++,
  - `.jslib` for WebGL.
- Needs more care when debugging C# ↔ plugin boundaries.

---

### Runtime flow in the new version

1. A gameplay script calls `KeyboardDown("C")` on `Osc`.
2. `Osc` converts the note into frequency.
3. `Osc` updates / synchronizes `ProceduralSynthVoiceConfig`.
4. `ProceduralSynthVoice` creates or reuses a backend voice.
5. The backend receives:
   - core synthesis params,
   - FM params,
   - harmonics,
   - wavetable,
   - sampling data,
   - ADSR data.
6. `NoteOn` activates the voice.
7. The backend mixes audio in real time:
   - native C++ plugin on supported platforms,
   - WebAudio `ScriptProcessor` on WebGL.

---

### Key files

- `Assets/scripts/new/Osc.cs`
- `Assets/scripts/new/ProceduralSynthVoice.cs`
- `Assets/scripts/new/ProceduralSynthVoiceConfig.cs`
- `Assets/scripts/new/ManagedProceduralSynthVoice.cs`
- `Assets/Plugins/ProceduralSynth/Source/ProceduralSynth.cpp`
- `Assets/Plugins/WebGL/ProceduralSynth.jslib`
