using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Esta clase administra la reproducción de una secuencia musical (adaptada de "Un elefante se balanceaba"),
// utilizando el sistema de polifonía para tocar varias notas simultáneamente. Cada nota incluye su duración
// y se esperan pausas determinadas para regular la ejecución secuencial.
public class SongManagerOlD : MonoBehaviour
{
    // Referencias a los componentes de polifonía, que se encargan de la creación y ajuste
    // de los osciladores (Osc) necesarios para reproducir las notas.
    public Polifonia OSC1;
    public Polifonia OSC2, BASS, KICK, KICK1,KICK2, SNARE, SNARE1, HIHAT;
    
    // El método Start() se llama antes del primer frame. En este caso, no se realiza
    // ninguna acción de inicialización específica.
    void Start()
    {
    }

    // Método público que configura los parámetros iniciales de los osciladores (octava, forma de onda,
    // ADSR, entre otros) y, posteriormente, inicia la corrutina que reproduce la canción.
   /* public void PSong1()
    {
        // Ajustes para el oscilador 1 (Polifonia):
        OSC1.OctaveSl.value = 4;
        OSC1.WaveformSl.value = 3;           // 3 corresponde a Sawtooth
        OSC1.AttackSlider.value = 7;
        OSC1.DecaySlider.value = 400;
        OSC1.SustainLevelSlider.value = 0.8f;
        OSC1.SustainSlider.value = 300;
        OSC1.VolumeSlider.value = 0.4f;

        // Ajustes para el oscilador 2 (Polifoniav2):
        OSC2.Octave = 3;
        OSC2.Waveform = 4;                   // 4 corresponde a Síntesis aditiva
        OSC2.Attack = 7;
        OSC2.Decay = 100;
        OSC2.SustainLevel = 0.99f;
        OSC2.Sustain = 2500;
        OSC2.Armonicos = 10;
        OSC2.Volume = 0.1f;
        OSC2.Detuneframes = 300;

        // Ajustes para el oscilador 3 (Polifoniav2):
        BASS.Octave = 1;
        BASS.Waveform = 1;                   // 4 corresponde a onda cuadrada
        BASS.Attack = 7;
        BASS.Decay = 40;
        BASS.SustainLevel = 0.2f;
        BASS.Sustain = 300;
        
        BASS.Volume = 0.2f;
        BASS.Detuneframes = 0;

         // Ajustes para el oscilador 3 (Polifoniav2):
        KICK.Octave = 0;
        KICK.Waveform = 0;                   // 4 corresponde a onda cuadrada
        KICK.Attack = 5;
        KICK.Decay = 10;
        KICK.SustainLevel = 0.1f;
        KICK.Sustain = 200;
        
        KICK.Volume = 0.5f;
        KICK.Detuneframes = 0;

        KICK1.Octave = 0;
        KICK1.Waveform = 5;                   // 4 corresponde a onda cuadrada
        KICK1.Attack = 5;
        KICK1.Decay = 10;
        KICK1.SustainLevel = 0.1f;
        KICK1.Sustain = 200;
        KICK1.Volume = 1.0f;
        KICK1.Detuneframes = 0;

        KICK2.Octave = 0;
        KICK2.Waveform = 5;                   // 4 corresponde a onda cuadrada
        KICK2.Attack = 5;
        KICK2.Decay = 5;
        KICK2.SustainLevel = 0.1f;
        KICK2.Sustain = 50;
        KICK2.Volume = 0.7f;
        KICK2.Detuneframes = 0;

        SNARE.Octave = 0;
        SNARE.Waveform = 5;                   // 4 corresponde a onda cuadrada
        SNARE.Attack = 5;
        SNARE.Decay = 5;
        SNARE.SustainLevel = 0.1f;
        SNARE.Sustain = 20;
        SNARE.Volume = 1.0f;
        SNARE.Detuneframes = 0;

        SNARE1.Octave = 0;
        SNARE1.Waveform = 5;                   // 4 corresponde a onda cuadrada
        SNARE1.Attack = 5;
        SNARE1.Decay = 5;
        SNARE1.SustainLevel = 0.1f;
        SNARE1.Sustain = 50;
        SNARE1.Volume = 1.0f;
        SNARE1.Detuneframes = 0;

        HIHAT.Octave = 0;
        HIHAT.Waveform = 5;                   // 4 corresponde a onda cuadrada
        HIHAT.Attack = 10;
        HIHAT.Decay = 100;
        HIHAT.SustainLevel = 0.1f;
        HIHAT.Sustain = 100;
        HIHAT.Volume = 0.1f;
        HIHAT.Detuneframes = 0;



        

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
        KICK.NoteOn("E");
        KICK1.NoteOn("B");
        KICK2.NoteOn("B");
        HIHAT.NoteOn("B");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("C");
        HIHAT.NoteOff("B");
        HIHAT.NoteOn("B");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");
        KICK.NoteOff("E");
        KICK1.NoteOff("B");
        KICK2.NoteOff("B");
        HIHAT.NoteOff("B");

        HIHAT.NoteOn("B");
        BASS.NoteOn("C");
        OSC1.NoteOn("G");
        SNARE.NoteOn("B");
        SNARE1.NoteOn("B");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");
        HIHAT.NoteOff("B");

        HIHAT.NoteOn("B");
        BASS.NoteOn("C");
        OSC1.NoteOn("F");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("F");
        BASS.NoteOff("C");
        SNARE.NoteOff("B");
        SNARE1.NoteOff("B");
        HIHAT.NoteOff("B");

        HIHAT.NoteOn("B");
        BASS.NoteOn("C");
        OSC1.NoteOn("E");
        KICK.NoteOn("E");
        KICK1.NoteOn("B");
        KICK2.NoteOn("B");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("C");
        HIHAT.NoteOff("B");
        HIHAT.NoteOn("B");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("E");
        BASS.NoteOff("C");
        KICK.NoteOff("E");
        KICK1.NoteOff("B");
        KICK2.NoteOff("B");
        HIHAT.NoteOff("B");
        HIHAT.NoteOn("B");
        BASS.NoteOn("C");
        OSC1.NoteOn("E");
        KICK.NoteOn("E");
        KICK1.NoteOn("B");
        KICK2.NoteOn("B");
        SNARE.NoteOn("B");
        SNARE1.NoteOn("B");
        yield return new WaitForSeconds(corchea);
        BASS.NoteOff("C");
        HIHAT.NoteOff("B");
        HIHAT.NoteOn("B");
        BASS.NoteOn("C");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("E");
        OSC2.NoteOff("C");
        OSC2.NoteOff("E");
        OSC2.NoteOff("G");
        BASS.NoteOff("C");
        KICK.NoteOff("E");
        SNARE.NoteOff("B");
        SNARE1.NoteOff("B");
        KICK1.NoteOff("B");
        KICK2.NoteOff("B");
        HIHAT.NoteOff("B");
       


        // Segundo Compás
        OSC1.NoteOn("G");
        OSC2.NoteOn("C");
        OSC2.NoteOn("E");
        OSC2.NoteOn("G");
        BASS.NoteOn("C");

        KICK.NoteOn("E");
        KICK1.NoteOn("B");
        KICK2.NoteOn("B");
        HIHAT.NoteOn("B");
        yield return new WaitForSeconds(corchea);
        OSC1.NoteOff("G");
        BASS.NoteOff("C");
        HIHAT.NoteOff("B");

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
*/}
