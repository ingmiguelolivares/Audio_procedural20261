# Síntesis de Audio Procedural con Inteligencia Artificial

Desarrollado por: Miguel Ángel Olivares Tenorio

Este proyecto de Unity enseña síntesis procedural, wavetables, ADSR, antialiasing y generación asistida de parámetros mediante GroqCloud. El objetivo no es pedirle a la IA que genere audio terminado, sino usarla para obtener una descripción estructurada del sonido y reconstruir localmente el timbre dentro del sintetizador.

## Implementaciones de síntesis

El proyecto conserva dos rutas de reproducción:

1. **Legacy moderno**: usa `Assets/scripts/new/Osc.cs` con el backend `AudioEngineBackend.LegacyOnAudioFilterRead`. Esta ruta genera las muestras dentro de Unity.
2. **External DSP / WebGL**: usa `ProceduralSynthVoice`, `ManagedProceduralSynthVoice`, `ProceduralSynth.cpp` y `ProceduralSynth.jslib`. Esta ruta permite usar un backend DSP externo o el puente Web Audio de WebGL.

Legacy no significa código descartable. En este proyecto representa una versión educativa y funcional del algoritmo que se mantiene para comparar enfoques de reproducción.

## Núcleo compartido

Los datos comunes del timbre viven en `Assets/scripts/TimbreAI/TimbrePreset.cs`.

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

Un timbre generado por IA se representa con tres estados espectrales:

- **Attack**: armónicos iniciales del sonido.
- **Sustain**: cuerpo estable del timbre.
- **Release**: color del sonido al liberar la nota.

Durante una nota, el sintetizador interpola suavemente de Attack a Sustain. Cuando ocurre `NoteOff`, interpola de Sustain a Release. Esta transición tímbrica es independiente del ADSR de amplitud.

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

## Integración con Groq

`Assets/scripts/TimbreAI/GroqTimbreGenerator.cs` llama a GroqCloud para convertir una descripción textual en JSON estructurado.

El JSON solicita:

- Nombre del timbre.
- Valores perceptuales de timbre.
- 16 armónicos de Attack.
- 16 armónicos de Sustain.
- 16 armónicos de Release.
- ADSR.

GroqCloud no genera muestras PCM ni archivos de audio. Unity genera localmente la wavetable y la reproducción final.

## API key por estudiante

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

## Flujo de timbre generado

```text
Prompt
  -> GroqTimbreGenerator
  -> JSON estructurado
  -> TimbrePreset + wavetable + ADSR
  -> Polifonia
  -> voces Osc activas y futuras
  -> Legacy moderno o External DSP/WebGL
  -> audio
```

El enum `WaveFormType.Gemini` se conserva como nombre heredado del modo de wavetable generada por IA. Conceptualmente, Gemini o Groq son proveedores de IA, no formas de onda.

## Pruebas sugeridas

1. Generar un timbre local con `Generate Local Test Timbre` sin API.
2. Reproducir una nota en Legacy moderno.
3. Reproducir la misma nota en External DSP.
4. Cambiar a una forma normal como Sine y comprobar que sigue sonando.
5. Volver al modo `Gemini` y comprobar que la wavetable generada no se reemplaza por ceros.
6. Probar notas graves y agudas para escuchar la limitación de armónicos.
7. Introducir una API key personal y generar un timbre desde prompt.
8. Compilar WebGL y verificar el desbloqueo de audio del navegador con interacción del usuario.

## Limitaciones conocidas

- WebGL depende de las políticas del navegador para desbloquear audio.
- Una API key en WebGL es visible para el cliente; no es una solución segura de producción.
- Las muestras resultantes entre Legacy y WebGL no tienen que ser idénticas, pero deben respetar el mismo preset, ADSR y límite de armónicos.
- La reproducción audible debe validarse dentro de Unity o navegador; la compilación C# no sustituye una prueba auditiva.
