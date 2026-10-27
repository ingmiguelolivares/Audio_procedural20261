# Sistema de Síntesis de Audio Procedural en Tiempo Real

Desarrollado por: Miguel Ángel Olivares Tenorio

Este proyecto de Unity implementa un sistema de síntesis de audio procedural en
tiempo real. Genera las muestras localmente durante la ejecución mediante
osciladores, síntesis aditiva, FM, wavetables, sampling, ADSR, LFO, detune y
limitación de armónicos por Nyquist.

La generación asistida por IA es un **módulo opcional**: puede proponer los
armónicos y la envolvente de un timbre a partir de una descripción textual, pero
no es necesaria para que el sintetizador funcione ni genera el audio final. El
audio siempre se sintetiza localmente en Unity, C++ o WebGL.

Incluye un paquete reutilizable para importar el sistema en otro proyecto:
[`ProceduralAudioSystem_Legacy_and_NativeDSP.unitypackage`](Exports/ProceduralAudioSystem_Legacy_and_NativeDSP.unitypackage).

## Inicio rápido: importar en un proyecto nuevo

1. Cree o abra un proyecto con **Unity 6** y los paquetes `Unity UI` y
   `TextMesh Pro` habilitados.
2. Descargue este repositorio y, desde el proyecto nuevo, seleccione
   **Assets > Import Package > Custom Package…**.
3. Seleccione `ProceduralAudioSystem_Legacy_and_NativeDSP.unitypackage` y deje
   todos los elementos marcados durante la importación.
4. Espere a que Unity termine de importar y compilar los scripts. Si Unity pide
   importar recursos esenciales de TextMesh Pro, acepte.
5. Abra una de estas escenas de demostración:

   - `Assets/Scenes/SampleScene.unity` para el sistema **Legacy histórico**.
   - `Assets/Scenes/Runtime/test_NativeDSP.unity` para la ruta **Native DSP**.

6. Pulse Play y pruebe primero una forma Sine y después los controles de ADSR,
   armónicos, FM, sampling y wavetables.

El paquete incluye scripts, prefabs, escenas de ejemplo, wavetables, clips de
audio, soporte MIDI, DryWetMIDI, recursos de UI, el puente de WebGL y el código
del backend. No incluye paquetes visuales ajenos al sintetizador.

> **Importante:** `MidiPlayerDrywet` lee archivos por la ruta
> `Assets/midifiles/`; mantenga ese directorio después de importar. El ejemplo
> disponible se llama `miditest.mid`, por lo que una escena que solicite
> `example.mid` debe actualizarse en el Inspector.

## Implementaciones de síntesis

El proyecto conserva dos sistemas comparables. Ambos generan el audio en tiempo
real y comparten conceptos musicales; lo que cambia es dónde se calculan las
muestras.

| Sistema | Punto de entrada | Cómo genera audio | Escena de ejemplo |
| --- | --- | --- | --- |
| **Legacy** | `Assets/scripts/OSC.cs` | `OnAudioFilterRead` dentro de Unity/C#. | `Assets/Scenes/SampleScene.unity` |
| **Native DSP** | `Assets/scripts/ProceduralAudio/NativeDSP/Osc.cs` | Usa `ProceduralSynthVoice`: plugin C++ en macOS, `.jslib` en WebGL, o fallback C# si el plugin no está disponible. | `Assets/Scenes/Runtime/test_NativeDSP.unity` |

La clase `Osc.cs` dentro de `NativeDSP` también permite seleccionar
`AudioEngineBackend.LegacyOnAudioFilterRead`. Es una versión migrada que permite
comparar el renderizado gestionado con `ExternalProceduralDSP` usando la misma
interfaz y parámetros.

### Sistema Legacy

`OSC.cs` implementa osciladores, síntesis aditiva, ADSR y control desde la UI
con C#. Es la ruta más directa para estudiar el flujo muestra a muestra de Unity:
la propia función de audio recibe un buffer y escribe las muestras generadas.
No requiere una librería nativa externa.

Úselo como referencia didáctica, para prototipos y para contrastar el resultado
con el backend externo. La escena `SampleScene` contiene los componentes y
prefabs históricos que lo acompañan.

### Sistema Native DSP

La implementación migrada se divide en capas:

```text
Polifonia / UI / MIDI
        -> Osc
        -> ProceduralSynthVoice
        -> C++ nativo (macOS) | .jslib (WebGL) | ManagedProceduralSynthVoice
        -> AudioSource de Unity
```

- `Polifonia.cs` conserva los ajustes globales y crea voces desde
  `OSC_Native.prefab`.
- `Osc.cs` convierte notas y parámetros de UI en una
  `ProceduralSynthVoiceConfig`.
- `ProceduralSynthVoice.cs` intenta el backend apropiado y cambia al fallback
  gestionado si no puede inicializar un plugin.
- `ManagedProceduralSynthVoice.cs` implementa en C# una versión funcional del
  motor para que el sistema siga sonando en plataformas sin binario nativo.
- `Assets/Plugins/ProceduralSynth/Source/ProceduralSynth.cpp` contiene el motor
  C++; `libProceduralSynth.dylib` es el binario incluido para macOS Apple
  Silicon; `Assets/Plugins/WebGL/ProceduralSynth.jslib` es el puente WebGL.

Para Windows, Linux o una Mac Intel el paquete conserva el fallback C# y el
código fuente C++, pero no incluye un binario precompilado específico. Compile
el plugin para ese destino y colóquelo en `Assets/Plugins/ProceduralSynth/` si
quiere ejecutar el backend nativo allí.

## Núcleo de síntesis compartido

Los datos comunes del timbre viven en `Assets/scripts/TimbreAI/TimbrePreset.cs`.
El nombre de la carpeta refleja el origen del módulo, pero el preset también se
puede crear y editar por completo sin IA desde el Inspector o el código.

Un `TimbrePreset` contiene:

- Nombre del timbre.
- 16 amplitudes armónicas para Attack.
- 16 amplitudes armónicas para Sustain.
- 16 amplitudes armónicas para Release.
- Parámetros ADSR compartidos.
- Validación de rangos.
- Reglas de antialiasing por Nyquist.

Los motores pueden reproducir el mismo preset con mecanismos internos distintos. La regla del proyecto es compartir datos y reglas, sin forzar una API de audio única para plataformas que tienen restricciones diferentes.

## Wavetables multiframe

Un timbre multiframe se representa con tres estados espectrales:

- **Attack**: armónicos iniciales del sonido.
- **Sustain**: cuerpo estable del timbre.
- **Release**: color del sonido al liberar la nota.

Durante una nota, el sintetizador interpola suavemente de Attack a Sustain.
Cuando ocurre `NoteOff`, interpola de Sustain a Release. Esta transición tímbrica
es independiente del ADSR de amplitud y se puede configurar manualmente o con
el módulo opcional de IA.

## Antialiasing

Cada backend limita los armónicos con la regla de Nyquist:

```text
frecuencia_maxima = sampleRate / 2
frecuencia_armonico = frecuencia_fundamental * indice_armonico
```

Un armónico se usa solo si su frecuencia no supera `sampleRate / 2`. Esto evita aliasing evidente, especialmente en notas agudas. La misma regla conceptual se aplica en C#, C++ y WebGL.

## ADSR

El ADSR se interpreta de forma coherente:

- Attack: segundos o milisegundos convertidos internamente.
- Decay: segundos o milisegundos convertidos internamente.
- Sustain Level: amplitud normalizada de 0 a 1.
- Release: segundos o milisegundos convertidos internamente.

El código conserva campos heredados en milisegundos para compatibilidad con la UI y escenas existentes, pero el preset compartido usa `AdsrSettings` como representación clara para estudiantes.

## Módulo opcional: diseño de timbre asistido por IA

`Assets/scripts/TimbreAI/GroqTimbreGenerator.cs` puede llamar a GroqCloud para
convertir una descripción textual en parámetros de timbre. Es una herramienta
de diseño de presets, no un requisito del motor de síntesis.

El JSON solicita:

- Nombre del timbre.
- Valores perceptuales de timbre.
- 16 armónicos de Attack.
- 16 armónicos de Sustain.
- 16 armónicos de Release.
- ADSR.

GroqCloud no genera muestras PCM ni archivos de audio. Unity genera localmente
la wavetable y la reproducción final. El sistema funciona sin API key: se pueden
usar las formas de onda, controles de UI y presets locales.

### API key por estudiante

Cada estudiante debe usar su propia API key de Groq.

En el Editor de Unity, el inspector personalizado de `GroqTimbreGenerator` permite guardar la clave en `EditorPrefs`, fuera de escenas, prefabs y archivos del proyecto. Para WebGL o modo ejecución, se puede usar el campo de UI `runtimeApiKeyInput`; esa clave permanece solo durante la sesión.

No se deben guardar claves en:

- Código fuente.
- Escenas.
- Prefabs.
- `PlayerPrefs`.
- Archivos del repositorio.
- Datos persistentes del navegador.

Importante: una API key escrita en una aplicación WebGL corre del lado del cliente y no puede considerarse secreta. Para producción se recomienda un intermediario seguro en servidor. Para laboratorio educativo, cada estudiante usa su propia clave y puede eliminarla al terminar.

### Flujo opcional de timbre asistido

```text
Prompt
  -> GroqTimbreGenerator
  -> JSON estructurado
  -> TimbrePreset + wavetable + ADSR
  -> Polifonia
  -> voces Osc activas y futuras
  -> Legacy o Native DSP
  -> audio
```

El enum `WaveFormType.Gemini` se conserva como nombre heredado del modo de wavetable generada por IA. Conceptualmente, Gemini o Groq son proveedores de IA, no formas de onda.

## Pruebas sugeridas

1. Reproducir una nota Sine en Legacy y después en Native DSP.
2. Cambiar la forma de onda, armónicos, ADSR, detune, LFO y parámetros FM.
3. Activar una wavetable o sampling y verificar el cambio de timbre.
4. Probar notas graves y agudas para escuchar la limitación de armónicos por
   Nyquist.
5. Compilar WebGL y verificar el desbloqueo de audio del navegador tras una
   interacción del usuario.
6. Opcionalmente, generar un timbre local de prueba o usar una API key personal
   para solicitar parámetros de timbre por texto.

## Limitaciones conocidas

- WebGL depende de las políticas del navegador para desbloquear audio.
- Una API key en WebGL es visible para el cliente; no es una solución segura de producción.
- Las muestras resultantes entre Legacy y WebGL no tienen que ser idénticas, pero deben respetar el mismo preset, ADSR y límite de armónicos.
- La reproducción audible debe validarse dentro de Unity o navegador; la compilación C# no sustituye una prueba auditiva.
