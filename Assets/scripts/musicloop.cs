using UnityEngine;
using System.Collections;

/// <summary>
/// Controla 15 loops asignados a teclas A..O (15 en total) y un clip final en Z que no hace loop.
/// - A..O: loops[0..14] (bucle infinito hasta que se elija otro; cambio al final del loop actual)
/// - Z: oneShotZ (se reproduce una sola vez y termina)
/// - Si no se presiona nada, el loop actual sigue indefinidamente.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class KeyboardLoopController : MonoBehaviour
{
    [Header("Clips")]
    [Tooltip("Exactamente 15 clips para las teclas A..O (A=0, B=1, ..., O=14).")]
    public AudioClip[] loops = new AudioClip[15];

    [Tooltip("Clip reproducido con la tecla Z (no hace loop).")]
    public AudioClip oneShotZ;

    [Header("Opcional")]
    [Tooltip("Si está activo, al iniciar reproducirá loops[0] (tecla A).")]
    public bool autoStartWithA = true;

    private AudioSource _as;
    private int currentLoopIndex = -1;   // -1 = nada o clip Z
    private int pendingLoopIndex = -1;   // solicitud en cola (A..O)
    private bool switching = false;      // evita múltiples corutinas simultáneas
    private Coroutine switchRoutine;

    void Awake()
    {
        _as = GetComponent<AudioSource>();
        _as.playOnAwake = false;
        _as.loop = false; // se ajusta dinámicamente
    }

    void Start()
    {
        if (autoStartWithA && loops != null && loops.Length >= 1 && loops[0] != null)
        {
            PlayLoop(0);
        }
    }

    void Update()
    {
        // Teclas A..O => 15 loops (A=0 .. O=14)
        for (int i = 0; i < 15; i++)
        {
            KeyCode key = KeyCode.A + i; // A..O
            if (Input.GetKeyDown(key))
            {
                QueueLoopChange(i);
                return;
            }
        }

        // Tecla Z => one-shot (sin loop)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            QueueOneShotZ();
            return;
        }
    }

    /// <summary>
    /// Solicita cambiar a un loop (A..O). Espera a que termine el loop actual.
    /// </summary>
    private void QueueLoopChange(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= 15) return;
        if (loops[targetIndex] == null) { Debug.LogWarning("Loop no asignado en índice " + targetIndex); return; }

        // Si ya está ese mismo loop sonando y no hay cambio pendiente, no hacer nada
        if (currentLoopIndex == targetIndex && pendingLoopIndex == -1) return;

        pendingLoopIndex = targetIndex;

        // Si se está cambiando, reinicia la espera para respetar el último pedido
        if (switching)
        {
            if (switchRoutine != null) StopCoroutine(switchRoutine);
        }
        switchRoutine = StartCoroutine(SwitchAtLoopEnd());
    }

    /// <summary>
    /// Solicita reproducir Z una sola vez al terminar el loop actual (o inmediatamente si no hay loop).
    /// </summary>
    private void QueueOneShotZ()
    {
        // Invalidar cualquier cambio pendiente a loops A..O
        pendingLoopIndex = -1;

        if (switching && switchRoutine != null) StopCoroutine(switchRoutine);

        // Si está sonando un loop, esperar al final del ciclo actual
        if (_as.isPlaying && _as.loop)
        {
            switchRoutine = StartCoroutine(PlayZAfterCurrentLoop());
        }
        else
        {
            PlayZ();
        }
    }

    /// <summary>
    /// Espera al final del loop actual y luego cambia al loop solicitado (pendingLoopIndex).
    /// </summary>
    private IEnumerator SwitchAtLoopEnd()
    {
        switching = true;

        // Si no está reproduciendo o no es un loop, reproducir de inmediato
        if (!_as.isPlaying || !_as.loop || _as.clip == null)
        {
            PlayLoop(pendingLoopIndex);
            pendingLoopIndex = -1;
            switching = false;
            yield break;
        }

        // Asegura que el clip actual no se repita al llegar al final de este ciclo
        _as.loop = false;

        // Calcular tiempo restante del ciclo actual
        float remaining = Mathf.Max(0f, _as.clip.length - _as.time);
        yield return new WaitForSeconds(remaining);

        if (pendingLoopIndex >= 0)
        {
            PlayLoop(pendingLoopIndex);
        }

        pendingLoopIndex = -1;
        switching = false;
    }

    /// <summary>
    /// Espera el final del ciclo actual y luego reproduce Z (sin loop).
    /// </summary>
    private IEnumerator PlayZAfterCurrentLoop()
    {
        switching = true;

        // Terminar el ciclo actual sin volver a empezar
        _as.loop = false;
        float remaining = (_as.clip != null) ? Mathf.Max(0f, _as.clip.length - _as.time) : 0f;
        yield return new WaitForSeconds(remaining);

        PlayZ();

        switching = false;
    }

    /// <summary>
    /// Reproduce un loop (A..O) en bucle.
    /// </summary>
    private void PlayLoop(int idx)
    {
        if (idx < 0 || idx >= 15) return;
        if (loops[idx] == null) { Debug.LogWarning("Loop no asignado en índice " + idx); return; }

        currentLoopIndex = idx;
        _as.clip = loops[idx];
        _as.loop = true;
        _as.time = 0f;
        _as.Play();
    }

    /// <summary>
    /// Reproduce el clip Z una sola vez (no loop). Al terminar, no reproduce nada más.
    /// </summary>
    private void PlayZ()
    {
        if (oneShotZ == null) { Debug.LogWarning("oneShotZ no asignado."); return; }

        currentLoopIndex = -1; // no estamos en un loop A..O
        _as.clip = oneShotZ;
        _as.loop = false;
        _as.time = 0f;
        _as.Play();
    }
}