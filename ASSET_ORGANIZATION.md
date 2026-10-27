# Inventario y organización de assets

## Punto de entrada actual

La escena habilitada para el build es `Assets/Scenes/Runtime/test_NativeDSP.unity`.
Es la referencia de esta limpieza: antes de retirar algo, probar esta escena en el
Editor y hacer un build del destino que se use en clase.

## Núcleo del sintetizador procedural

Estos assets son necesarios o usados directamente por la escena de entrada:

| Ubicación | Propósito |
| --- | --- |
| `Assets/Scenes/Runtime/test_NativeDSP.unity` | Escena principal habilitada en Build Settings. |
| `Assets/scripts/ProceduralAudio/NativeDSP/` | Oscilador, polifonía, voces DSP nativas/gestionadas y gestores de canción. |
| `Assets/prefabs/ProceduralAudio/NativeDSP/OSC_Native.prefab` | Voz que instancia `Polifonia`; no eliminar. |
| `Assets/Audios/ProceduralAudio/piano.mp3` | Clip de muestreo asignado en la escena. |
| `Assets/Audios/ProceduralAudio/Kick.wav` | Clip ADSR asignado en la escena. |
| `Assets/Plugins/ProceduralSynth/` | Fuente y puente del plugin de síntesis nativa. |
| `Assets/Plugins/WebGL/ProceduralSynth.jslib` | Implementación del puente para WebGL. |
| `Assets/Melanchall/DryWetMIDI.Nativeless/` | Dependencia de `MidiPlayerDrywet.cs`. |

Unity conserva las referencias de escena y prefab porque al mover assets se
mantuvieron sus archivos `.meta` y, por tanto, sus GUID.

## Recursos opcionales del sistema

- `Assets/Wavetables/`: tablas de onda y cargador. El cargador usa por defecto
  rutas bajo `Assets/Wavetables`, por eso ese directorio no se movió.
- `Assets/midifiles/`: MIDI para `MidiPlayerDrywet.cs`. Ese script carga por ruta
  (`Application.dataPath/midifiles`), por lo que tampoco se debe renombrar o mover
  esta carpeta sin actualizar el código. La escena actual solicita `example.mid`,
  pero el archivo disponible se llama `miditest.mid`: revisar esa asignación antes
  de usar la reproducción MIDI.
- `Assets/NewAudioMixer.mixer`: mezclador disponible, aunque la escena principal
  no tiene un grupo del mezclador asignado directamente.

## Candidatos para revisión, no para borrado automático

Estas colecciones no están referenciadas directamente por la escena habilitada.
Pueden ser material de clases anteriores, demostraciones o recursos visuales. No
se han borrado ni movido para evitar eliminar contenido útil de otras escenas:

| Carpeta | Tamaño aproximado | Observación |
| --- | ---: | --- |
| `Assets/ALP_Assets/` | 665 MB | Paquete de texturas/terreno con escenas demo. |
| `Assets/Rolling_Balls-Sci-fi_Pack/` | 258 MB | Pack visual con escena demo. |
| `Assets/FantasyEnvironments/` | 220 MB | Entornos, prefabs y demos. |
| `Assets/models/` | 37 MB | Modelos y animaciones. |
| `Assets/imaghes/` | 12 MB | Imágenes de interfaz/personajes. |
| `Assets/laxer tree pkg/` | 3.4 MB | Árboles y prefabs. |
| `Assets/_Recovery/` | 720 KB | Recuperación automática de Unity. |

También conviene revisar las escenas de experimentación en `Assets/Scenes/` y los
scripts que quedan fuera de `Assets/scripts/ProceduralAudio/NativeDSP/` antes de
eliminar sus recursos asociados.

## Regla de limpieza segura

1. Cerrar Unity o trabajar con control de versiones.
2. Mover un candidato a una carpeta temporal fuera de `Assets` (no borrarlo).
3. Abrir `test_NativeDSP`, probar Play Mode y realizar un build objetivo.
4. Si no hay referencias perdidas ni errores, eliminarlo en un cambio separado.

No se debe basar la limpieza solo en búsquedas de GUID: el proyecto usa carga por
ruta para MIDI y tablas de onda, que Unity no siempre puede detectar como
dependencias serializadas.
