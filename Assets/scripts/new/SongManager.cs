using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Esta clase administra la reproducción de una secuencia musical (adaptada de "Un elefante se balanceaba"),
// utilizando el sistema de polifonía para tocar varias notas simultáneamente. Cada nota incluye su duración
// y se esperan pausas determinadas para regular la ejecución secuencial.
public class SongManager : MonoBehaviour
{
    // Referencias a los componentes de polifonía, que se encargan de la creación y ajuste
    // de los osciladores (Osc) necesarios para reproducir las notas.
    public Polifonia OSC1;
    public Polifonia OSC2, BASS;
    
    // El método Start() se llama antes del primer frame. En este caso, no se realiza
    // ninguna acción de inicialización específica.
    void Start()
    {
    }

    public void PSong1()
    {
        // Ajustes para el oscilador 1 (Polifonia) usando valores por código,
        // sin depender de sliders ni textos en la UI.
        OSC1.OctaveValue = 4;
        OSC1.WaveformValue = 3;           // 3 corresponde a Sawtooth
        OSC1.AttackValue = 7;
        OSC1.DecayValue = 400;
        OSC1.SustainLevelValue = 0.8f;
        OSC1.SustainValue = 300;
        OSC1.VolumeValue = 0.4f;
        OSC1.DetuneCentsValue = 0f;
        OSC1.TremLFOValue = 0f;
        OSC1.VibLFOValue = 0f;
        OSC1.VibratoDepthValue = 5f;
        OSC1.WavetableValue = false;

        // Aplicar los cambios por código en caso de que ya existan osciladores activos.
        OSC1.OctaveChange();
        OSC1.WaveFormChange();
        OSC1.UpdateAttack();
        OSC1.UpdateDecay();
        OSC1.UpdateSustainLevel();
        OSC1.UpdateSustain();
        OSC1.UpdateVolume();
        OSC1.DetunedChange();
        OSC1.TremFChange();
        OSC1.VibFChange();
        OSC1.VibratoDepthChange();
        OSC1.ToggleWavetable();

        // Ajustes para el oscilador 2 (Polifonia) usando valores por código.
        OSC2.OctaveValue = 3;
        OSC2.WaveformValue = 7;           
        OSC2.AttackValue = 10;
        OSC2.DecayValue = 100;
        OSC2.SustainLevelValue = 0.7f;
        OSC2.SustainValue = 2500;
        OSC2.ArmonicosValue = 10;
        OSC2.VolumeValue = 0.2f;
        OSC2.DetuneCentsValue = 10f;
        OSC2.TremLFOValue = 0f;
        OSC2.VibLFOValue = 0f;
        OSC2.VibratoDepthValue = 5f;
        OSC2.WavetableValue = true;

        OSC2.OctaveChange();
        OSC2.WaveFormChange();
        OSC2.UpdateAttack();
        OSC2.UpdateDecay();
        OSC2.UpdateSustainLevel();
        OSC2.UpdateSustain();
        OSC2.ArmonicosChange();
        OSC2.UpdateVolume();
        OSC2.DetunedChange();
        OSC2.TremFChange();
        OSC2.VibFChange();
        OSC2.VibratoDepthChange();
        OSC2.ToggleWavetable();

        // Ajustes para el bajo (Polifonia) usando valores por código.
        BASS.OctaveValue = 1;
        BASS.WaveformValue = 1;           // 1 corresponde a onda cuadrada
        BASS.AttackValue = 7;
        BASS.DecayValue = 15;
        BASS.SustainLevelValue = 0.8f;
        BASS.SustainValue = 200;
        BASS.VolumeValue = 0.2f;
        BASS.DetuneCentsValue = 0f;
        BASS.TremLFOValue = 0f;
        BASS.VibLFOValue = 0f;
        BASS.VibratoDepthValue = 5f;
        BASS.WavetableValue = false;

        BASS.OctaveChange();
        BASS.WaveFormChange();
        BASS.UpdateAttack();
        BASS.UpdateDecay();
        BASS.UpdateSustainLevel();
        BASS.UpdateSustain();
        BASS.UpdateVolume();
        BASS.DetunedChange();
        BASS.TremFChange();
        BASS.VibFChange();
        BASS.VibratoDepthChange();
        BASS.ToggleWavetable();

        // Inicia la corrutina que reproduce la secuencia de notas.
        StartCoroutine(song1());
    }

    // Update() se llama una vez por frame. No se implementa ninguna lógica dentro de este método.
    void Update()
    {
    }

    // Corrutina que define la secuencia de notas, sus duraciones y la temporización. Cada nota es
    // reproducida mediante NoteOn y detenida con NoteOff, utilizando pausas con WaitForSeconds()
    // para controlar la duración de cada nota y silencio.
    IEnumerator song1()
    {
        // Definición del tempo (beats por minuto) y asignación de las duraciones correspondientes
        // a las figuras rítmicas, en segundos.
        float tempo = 70f;
        float negra = 40 / tempo;
        float negra2 = 60 / tempo;
        float blanca = 80 / tempo;
        float redonda = 160 / tempo; // (No se utiliza en este ejemplo)
        float corchea = 20 / tempo;

        // A continuación, se reproducen las notas de la canción en varios compases.
        // Cada bloque comprende notas (NoteOn) seguidas de pausas (WaitForSeconds),
        // y la detención de cada nota (NoteOff).

        // Primer Compás
        OSC1.NoteOn("G");
        OSC2.NoteOn("C");
        OSC2.NoteOn("E");
        OSC2.NoteOn("G");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("C");
       
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");
        
        BASS.NoteOn("C");
        OSC1.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");

        BASS.NoteOn("C");
        OSC1.NoteOn("F");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("F");
        BASS.NoteOff("C");
 
        BASS.NoteOn("C");
        OSC1.NoteOn("E");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("C");
 
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("E");
        BASS.NoteOff("C");
 
        BASS.NoteOn("C");
        OSC1.NoteOn("E");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("C");
 
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("E");
        OSC2.NoteOff("C");
        OSC2.NoteOff("E");
        OSC2.NoteOff("G");
        BASS.NoteOff("C");


        // Segundo Compás
        OSC1.NoteOn("G");
        OSC2.NoteOn("C");
        OSC2.NoteOn("E");
        OSC2.NoteOn("G");
        BASS.NoteOn("C");

        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");

        OSC1.NoteOn("G");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");

        OSC1.NoteOn("G");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");

        OSC1.NoteOn("F");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("F");
        BASS.NoteOff("C");

        OSC1.NoteOn("E");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("C");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("E");
        BASS.NoteOff("C");

        OSC1.NoteOn("E");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("C");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("E");
        OSC2.NoteOff("C");
        OSC2.NoteOff("E");
        OSC2.NoteOff("G");
        BASS.NoteOff("C");

        // Tercer Compás
        OSC1.NoteOn("G");
        OSC2.NoteOn("C");
        OSC2.NoteOn("E");
        OSC2.NoteOn("G");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("C");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");

        OSC1.NoteOn("G");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");

        OSC1.NoteOn("G");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");

        OSC1.NoteOn("A");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("A");
        BASS.NoteOff("C");

        OSC1.NoteOn("G");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");


        OSC1.NoteOn("F");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("F");
        BASS.NoteOff("C");

        OSC1.NoteOn("E");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("E");
        OSC2.NoteOff("C");
        OSC2.NoteOff("E");
        OSC2.NoteOff("G");
        BASS.NoteOff("C");

        // Cuarto Compás
        OSC1.NoteOn("F");
        OSC2.NoteOn("G");
        OSC2.NoteOn("B");
        OSC2.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("F");
        BASS.NoteOff("G");

        OSC1.NoteOn("E");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("E");
        BASS.NoteOff("G");

        OSC1.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("D");
        OSC2.NoteOff("G");
        OSC2.NoteOff("B");
        OSC2.NoteOff("D");

        // Quinto Compás
        OSC1.NoteOn("F");
        OSC2.NoteOn("G");
        OSC2.NoteOn("B");
        OSC2.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("F");

        OSC1.NoteOn("F");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("F");

        OSC1.NoteOn("E");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("E");

        OSC1.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");

        OSC1.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("D");
        OSC2.NoteOff("G");
        OSC2.NoteOff("B");
        OSC2.NoteOff("D");

        // Sexto Compás
        OSC1.NoteOn("F");
        OSC2.NoteOn("G");
        OSC2.NoteOn("B");
        OSC2.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("F");

        OSC1.NoteOn("F");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("F");

        OSC1.NoteOn("F");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("F");

        OSC1.NoteOn("E");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("E");

        OSC1.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("D");

        OSC1.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("D");
        OSC2.NoteOff("G");
        OSC2.NoteOff("B");
        OSC2.NoteOff("D");

        // Séptimo Compás
        OSC1.NoteOn("G");
        OSC2.NoteOn("G");
        OSC2.NoteOn("B");
        OSC2.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("G");

        OSC1.NoteOn("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("G");

        OSC1.NoteOn("A");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("A");

        OSC1.NoteOn("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("G");

        OSC1.NoteOn("F");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("F");

        OSC1.NoteOn("E");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("E");

        OSC1.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("D");
        OSC2.NoteOff("G");
        OSC2.NoteOff("B");
        OSC2.NoteOff("D");

        // Octavo Compás
        OSC1.NoteOn("E");
        OSC2.NoteOn("G");
        OSC2.NoteOn("B");
        OSC2.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("E");
        BASS.NoteOff("G");

        OSC1.NoteOn("D");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("G");
        OSC1.NoteOff("D");
        OSC2.NoteOff("G");
        OSC2.NoteOff("B");
        OSC2.NoteOff("D");

        OSC1.NoteOn("C");
        OSC2.NoteOn("C");
        OSC2.NoteOn("E");
        OSC2.NoteOn("G");
        BASS.NoteOn("G");
        yield return new WaitForSeconds(blanca);
        BASS.NoteOff("C");
        OSC1.NoteOff("C");
        OSC2.NoteOff("C");
        OSC2.NoteOff("E");
        OSC2.NoteOff("G");
    }
}
