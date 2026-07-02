using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager1 : MonoBehaviour
{
    public Osc OSC1, OSC2, OSC3, OSC4, OSC5, OSC6, OSC7, OSC8, OSC9, OSC10;
    //OSC2, OSC3, OSC4, OSC5;
    //public AudioSource Kick, Snare;

    //public AudioClip Kickdrum, SnareDrum;


    float tempo;
    float negra;
    float blanca;
    float redonda;
    float corchea;
    float semicorchea;



    bool isPlaying = false;
    // Start is called before the first frame update
    void Start()
    {
        //Application.targetFrameRate = 50;
        PSong();
        

    }

    // Update is called once per frame
    void Update()
    {

    }


    public void PSong()
    {
        isPlaying = true;
        StartCoroutine(song());
    }


    

    public void stopSong1() {
        isPlaying = false;
    }

    public void stopSong2()
    {
        StopAllCoroutines();
        OSC1.KeyboardUp();
        /*OSC2.KeyboardUp();
        OSC3.KeyboardUp();
        OSC4.KeyboardUp();
        OSC5.KeyboardUp();*/

    }

    public void melody() {

        if (OSC1.Aud.mute) OSC1.Aud.mute = false;
        else OSC1.Aud.mute = true;
    }

   


    public void TempoChange() {

        //tempo = tempoSl.value;
        negra = 40 / tempo;
        blanca = 80 / tempo;
        redonda = 160 / tempo;
        corchea = 20 / tempo;
        semicorchea = 10 / tempo;
    }

    IEnumerator song()
    {
        tempo = 100f;
        negra = 40 / tempo;
        blanca = 80 / tempo;
        redonda = 160 / tempo;
        corchea = 20 / tempo;
        semicorchea = 10 / tempo;

        ConfigureOsc(OSC1, 1, Osc.WaveFormType.Sine);

        ConfigureOsc(OSC2, OSC2.Octava, Osc.WaveFormType.WhiteNoise);
        ConfigureOsc(OSC10, OSC10.Octava, Osc.WaveFormType.WhiteNoise);

        ConfigureOsc(OSC3, OSC3.Octava, Osc.WaveFormType.WhiteNoise);

        ConfigureOsc(OSC4, OSC4.Octava, Osc.WaveFormType.WhiteNoise);

        OSC3.Aud.mute = true;
        OSC4.Aud.mute = true;

        ConfigureOsc(OSC5, 2, Osc.WaveFormType.Square);
        OSC5.Aud.mute = true;


        ConfigureOsc(OSC6, 4, Osc.WaveFormType.Sawtooth);
        OSC6.Aud.mute = true;

        ConfigureOsc(OSC7, 5, Osc.WaveFormType.Sawtooth);
        OSC7.Aud.mute = true;

        ConfigureOsc(OSC8, 5, Osc.WaveFormType.Sawtooth);
        OSC8.Aud.mute = true;

        ConfigureOsc(OSC9, 5, Osc.WaveFormType.Sawtooth);
        OSC9.Aud.mute = true;




        while (isPlaying)
        {
            //Primer Compas
            //Kick
            OSC1.KeyboardDown("B");
            OSC2.KeyboardDown("B");
            OSC10.KeyboardDown("B");
            //acordes
            OSC7.KeyboardDown("C");
            OSC8.KeyboardDown("E");
            OSC9.KeyboardDown("G");

            //Melody1
            OSC6.KeyboardDown("C2");
            //Bass
            OSC5.KeyboardDown("C");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("D");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("C");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("D");
            yield return new WaitForSeconds(semicorchea);
            OSC1.KeyboardUp();
            OSC2.KeyboardUp();
            OSC10.KeyboardUp();
            OSC5.KeyboardUp();
            OSC6.KeyboardUp();
            //Snare
            OSC3.KeyboardDown("B");
            OSC4.KeyboardDown("B");
            //acordes

            //Melody1
            OSC6.KeyboardDown("B");
            OSC5.KeyboardDown("E");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("D");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("C");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("D");
            yield return new WaitForSeconds(semicorchea);

            OSC3.KeyboardUp();
            OSC4.KeyboardUp();
            OSC5.KeyboardUp();
            OSC6.KeyboardUp();
            //Kick
            OSC1.KeyboardDown("B");
            OSC2.KeyboardDown("B");
            OSC10.KeyboardDown("B");
            //acordes

            //Melody1
            OSC6.KeyboardDown("A#");

            OSC5.KeyboardDown("C");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("D");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("C");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("D");
            yield return new WaitForSeconds(semicorchea);
            OSC1.KeyboardUp();
            OSC2.KeyboardUp();
            OSC10.KeyboardUp();
            OSC5.KeyboardUp();
            OSC6.KeyboardUp();

            //Snare
            OSC3.KeyboardDown("B");
            OSC4.KeyboardDown("B");
            //acordes

            //Melody1
            OSC6.KeyboardDown("A");

            OSC5.KeyboardDown("E");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("D");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("C");
            yield return new WaitForSeconds(semicorchea);
            OSC5.KeyboardUp();

            OSC5.KeyboardDown("D");
            yield return new WaitForSeconds(semicorchea);
            OSC3.KeyboardUp();
            OSC4.KeyboardUp();
            OSC5.KeyboardUp();
            OSC6.KeyboardUp();
            OSC7.KeyboardUp();
            OSC8.KeyboardUp();
            OSC9.KeyboardUp();

        }
    }
    //Primera cancion (Frere Jacques)
    private void ConfigureOsc(Osc osc, int octave, Osc.WaveFormType waveFormType)
    {
        if (osc == null)
            return;

        osc.OctaveChange(octave);
        osc.WaveFormChange(waveFormType);
        osc.MarkExternalBackendDirty();
    }


}

