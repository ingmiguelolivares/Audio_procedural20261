using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Osc; // Permite acceder de forma directa a los miembros estáticos o públicos de la clase Osc

// Esta clase maneja la polifonía de un instrumento virtual. Permite instanciar múltiples
// osciladores (Osc), cada uno reproduciendo una nota distinta, y centraliza los parámetros
// comunes (octava, forma de onda, ADSR, LFOs, detune en cents, etc.) para todos los osciladores activos.
public class Polifoniav2 : MonoBehaviour
{
    // Prefab que contiene el componente Osc (un oscilador configurado por defecto).
    public GameObject OSCprefab;

    // Diccionario que asocia el nombre de la nota con la instancia de Osc que la está reproduciendo.
    private Dictionary<string, Osc> activeOscillators = new Dictionary<string, Osc>();
  
    // Parámetros configurables desde otras clases, inspector o UI externa.
    public int Attack, Decay, Sustain, Release, Octave, Waveform, Armonicos;
    public float[] AmplitudesLv = new float[10];
    public bool Wavetable;
    public float SustainLevel, Volume;

    // Parámetros adicionales agregados en las versiones recientes del oscilador.
    public float DetuneCents;
    public float TremLFOF;
    public float VibLFOF;
    public float VibratoDepth = 5f;
    
    // El método Start() se llama antes del primer frame. En este caso, inicializa los valores
    // por defecto del arreglo de amplitudes.
    void Start()
    {
        // Inicializamos las amplitudes de los armónicos con valores decrecientes.
        AmplitudesLv = new float[10] { 1.0f, 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };
    }

    // El método Update() se llama una vez por frame. En este script no se implementa lógica específica.
    void Update()
    {
    }

    // Actualiza el volumen en todos los osciladores activos, utilizando el valor de la variable Volume.
    public void UpdateVolume()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateVolume(Volume);
        }
    }

    // Cambia la octava en todos los osciladores activos según el valor de la variable Octave.
    public void OctaveChange()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.OctaveChange(Octave);
        }
    }

    // Cambia el detune en cents en todos los osciladores activos.
    public void DetunedChange()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.detuneCents = DetuneCents;
        }
    }

    // Cambia la frecuencia del tremolo LFO en todos los osciladores activos.
    public void TremFChange()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.tremLFOf = TremLFOF;
        }
    }

    // Cambia la frecuencia del vibrato LFO en todos los osciladores activos.
    public void VibFChange()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.VibLFOf = VibLFOF;
        }
    }

    // Cambia la profundidad del vibrato en todos los osciladores activos.
    public void VibratoDepthChange()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.vibratoDepth = VibratoDepth;
        }
    }

    // Cambia la forma de onda en todos los osciladores activos según el valor de la variable Waveform.
    public void WaveFormChange()
    {
        if (Waveform == 0)
        {
            foreach (var osc in activeOscillators.Values)
            {
                osc.WaveFormChange(WaveFormType.Sine);
            }
        }
        else if (Waveform == 1)
        {
            foreach (var osc in activeOscillators.Values)
            {
                osc.WaveFormChange(WaveFormType.Square);
            }
        }
        else if (Waveform == 2)
        {
            foreach (var osc in activeOscillators.Values)
            {
                osc.WaveFormChange(WaveFormType.Triangle);
            }
        }
        else if (Waveform == 3)
        {
            foreach (var osc in activeOscillators.Values)
            {
                osc.WaveFormChange(WaveFormType.Sawtooth);
            }
        }
        else if (Waveform == 4)
        {
            foreach (var osc in activeOscillators.Values)
            {
                osc.WaveFormChange(WaveFormType.SA);
            }
        }
        else if (Waveform == 5)
        {
            foreach (var osc in activeOscillators.Values)
            {
                osc.WaveFormChange(WaveFormType.WhiteNoise);
            }
        }
        else if (Waveform == 6)
        {
            foreach (var osc in activeOscillators.Values)
            {
                osc.WaveFormChange(WaveFormType.Custom1);
            }
        }
        else if (Waveform == 7)
        {
            foreach (var osc in activeOscillators.Values)
            {
                osc.WaveFormChange(WaveFormType.Custom2);
            }
        }
        else if (Waveform == 8)
        {
            foreach (var osc in activeOscillators.Values)
            {
                osc.WaveFormChange(WaveFormType.Custom3);
            }
        }
    }

    // Cambia la cantidad de armónicos en todos los osciladores activos.
    public void ArmonicosChange()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.ArmonicosChange(Armonicos);
        }
    }

    // Actualiza las amplitudes de todos los osciladores activos para la forma de onda SA,
    // recorriendo las posiciones del arreglo AmplitudesLv.
    public void AmplitudesChange()
    {
        for (int i = 0; i < AmplitudesLv.Length; i++)
        {
            foreach (var osc in activeOscillators.Values)
            {
                osc.AmplitudesChange(i, AmplitudesLv[i]);
            }
        }
    }

    // Activa o desactiva el uso de wavetable en todos los osciladores activos.
    public void ToggleWavetable()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.ToggleWavetable(Wavetable);
        }
        // Cada oscilador regenerará su tabla de ondas si se activa o desactiva.
    }

    // A continuación, métodos que actualizan los parámetros de la envolvente ADSR
    // en todos los osciladores activos.
    public void UpdateAttack()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateAttack(Attack);
        }
    }

    public void UpdateDecay()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateDecay(Decay);
        }
    }

    public void UpdateSustain()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateSustain(Sustain);
        }
    }

    public void UpdateSustainLevel()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateSustainLevel(SustainLevel);
        }
    }

    public void UpdateRelease()
    {
        foreach (var osc in activeOscillators.Values)
        {
            osc.UpdateRelease(Release);
        }
    }

    // Este método se llama cuando se presiona una tecla. Si no existe un Osc para esa nota, se crea
    // a partir del prefab y se configura con los ajustes actuales. Luego, reproduce la nota.
    public void NoteOn(string note)
    {
        if (!activeOscillators.ContainsKey(note))
        {
            // Instanciamos un nuevo Osc a partir del prefab y lo agregamos al diccionario.
            GameObject oscInstance = Instantiate(OSCprefab, transform);
            oscInstance.name = note;
            Osc oscScript = oscInstance.GetComponent<Osc>();
            if (oscScript != null)
            {
                activeOscillators[note] = oscScript;
                // Aplicamos los ajustes actuales al nuevo oscilador.
                ApplySettingsToOsc(oscScript);
                // Inicia la reproducción de la nota.
                oscScript.KeyboardDown(note);
            }
        }
    }

    // Este método se llama cuando se suelta una tecla. Si existe un Osc para esa nota,
    // se desactiva la reproducción, se destruye el GameObject y se remueve del diccionario.
    public void NoteOff(string note)
    {
        if (activeOscillators.ContainsKey(note))
        {
            activeOscillators[note].KeyboardUp();
            Destroy(activeOscillators[note].gameObject, 0.1f);
            activeOscillators.Remove(note);
        }
    }

    // Aplica todos los valores de configuración actual a un nuevo oscilador.
    private void ApplySettingsToOsc(Osc osc)
    {
        UpdateVolume();
        OctaveChange();
        WaveFormChange();
        ArmonicosChange();
        AmplitudesChange();
        ToggleWavetable();
        UpdateAttack();
        UpdateDecay();
        UpdateSustainLevel();
        UpdateSustain();
        UpdateRelease();
        DetunedChange();
        TremFChange();
        VibFChange();
        VibratoDepthChange();
    }
}