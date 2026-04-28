using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

public class PlayerController : MonoBehaviour
{
    
    public float moveDistance = 0.1f;   // Distance to move horizontally (1 unit)
    public float jumpForce = 5.0f;      // Force applied to jump
    public bool isGrounded;      // Check if the player is on the ground

    private Animator animator;
    private int movement = 1;

    private Rigidbody rb;

    public Vector3 center;

    public SoundManager1 SManager;

    public int Score= 0;

    public TextMeshProUGUI ScoreText;

    bool SnareCheck = false, BassCheck = false, ChordsCheck = false, MelodyCheck = false, 
                    KickCheck = false, AllCheck = false, NoneCheck = false ;

    public OSC OSC1, OSC2, OSC3;

    public AudioSource Aud;

    public AudioClip[] Clips;

    void Start()
    {
        rb = GetComponent<Rigidbody>();  // Get the Rigidbody2D component for physics movement
        animator = GetComponent<Animator>();
        center = transform.position;
        StartCoroutine(FootSteps());
        //animator.SetInteger("movement", movement);
        
    }

    void Update()
    {
        if (transform.position.x <= center.x + 1 && transform.position.x >= center.x - 1){
            // Check for horizontal movement (left or right)
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Move(-moveDistance);   // Move left by one unit
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Move(moveDistance);    // Move right by one unit
            }

        }
        

        // Check for jumping
        if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded)
        {
            Jump();   // Jump if on the ground
            movement = 2;
        }

        if (isGrounded) movement = 1;
        

        // Set the movement parameter in the Animator
        animator.SetInteger("movement", movement);
        transform.position += new Vector3(0, 0, 2*Time.deltaTime);

        if (SnareCheck) Score += 30;
        if (BassCheck) Score += 25;
        if (MelodyCheck) Score += 30;
        if (ChordsCheck) Score += 20;
        if (KickCheck) Score -= 35;
        if (AllCheck) Score += 100;
        if (NoneCheck) Score -= 120;
        ScoreText.SetText(Score.ToString());
    }

    void Move(float distance)
    {
        // Move the object by the specified distance in the x-axis
        transform.position += new Vector3(distance, 0, 0);
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);  // Apply an upward force to jump
        isGrounded = false;   // The object is now in the air
    }

    // Check if the object is grounded (use colliders to determine when it's touching the ground)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;   // The object is touching the ground again
            //print("colisiono");
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Kick"))
        {
           
            Destroy(col.gameObject);
            StartCoroutine(MuteKick());
        }
        else if (col.gameObject.CompareTag("Snare"))
        {
            //print("Snare");
            Destroy(col.gameObject);
            StartCoroutine(UnMuteSnare());

        }
        if (col.gameObject.CompareTag("Bass"))
        {
            //print("Bass");
            StartCoroutine(UnMuteBass());
            Destroy(col.gameObject);
        }
        if (col.gameObject.CompareTag("Chords"))
        {
            //print("Chords");
            StartCoroutine(UnMuteChords());
            Destroy(col.gameObject);
        }
        if (col.gameObject.CompareTag("Melody"))
        {
            //print("Melody");
            Destroy(col.gameObject);
            StartCoroutine(UnMuteMelody());
        }
         if (col.gameObject.CompareTag("All"))
        {
           
            Destroy(col.gameObject);
            StartCoroutine(UnMuteAll());
        }
        if (col.gameObject.CompareTag("None"))
        {
           
            Destroy(col.gameObject);
            StartCoroutine(MuteAll());
        }
    }

     IEnumerator MuteKick()
    {
        KickCheck = true;
        SManager.OSC1.Aud.mute = true;
        SManager.OSC2.Aud.mute = true;
        SManager.OSC10.Aud.mute = true;
        OSC2.KeyboardDown("C2");
        yield return new WaitForSeconds(0.05f);
        OSC2.KeyboardUp();
        OSC2.KeyboardDown("B");
        yield return new WaitForSeconds(0.05f);
        OSC2.KeyboardUp();
         OSC2.KeyboardDown("A#");
        yield return new WaitForSeconds(0.05f);
        OSC2.KeyboardUp();
         OSC2.KeyboardDown("A"); 
        yield return new WaitForSeconds(0.05f);
        OSC2.KeyboardUp();
       
        yield return new WaitForSeconds(4.8f);
        SManager.OSC1.Aud.mute = false;
        SManager.OSC2.Aud.mute = false;
        SManager.OSC10.Aud.mute = false;
        KickCheck = false;
       
    }

    IEnumerator UnMuteSnare()
    {
        OSC1.Octava = 6;
        
        SnareCheck = true;
        SManager.OSC3.Aud.mute = false;
        SManager.OSC4.Aud.mute = false;
        OSC1.KeyboardDown("C");
        yield return new WaitForSeconds(0.1f);
        OSC1.KeyboardUp();
        OSC1.KeyboardDown("G");
        yield return new WaitForSeconds(0.1f);
        OSC1.KeyboardUp();
        yield return new WaitForSeconds(4.8f);
        SManager.OSC3.Aud.mute = true;
        SManager.OSC4.Aud.mute = true;
        SnareCheck = false;
       
    }

    IEnumerator UnMuteBass()
    {
        OSC1.Octava = 6;
        BassCheck = true;
        SManager.OSC5.Aud.mute = false;
        OSC1.KeyboardDown("C");
        yield return new WaitForSeconds(0.1f);
        OSC1.KeyboardUp();
        OSC1.KeyboardDown("G");
        yield return new WaitForSeconds(0.1f);
        OSC1.KeyboardUp();
        yield return new WaitForSeconds(4.8f);
        SManager.OSC5.Aud.mute = true;
        BassCheck = false;
    }

    IEnumerator UnMuteMelody()
    {
       OSC1.Octava = 6;
       MelodyCheck = true;
        SManager.OSC6.Aud.mute = false;
        OSC1.KeyboardDown("C");
        yield return new WaitForSeconds(0.1f);
        OSC1.KeyboardUp();
        OSC1.KeyboardDown("G");
        yield return new WaitForSeconds(0.1f);
        OSC1.KeyboardUp();
        yield return new WaitForSeconds(4.8f);
        SManager.OSC6.Aud.mute = true;
        MelodyCheck = false;
    }

    IEnumerator UnMuteChords()
    {
        OSC1.Octava = 6;
        ChordsCheck = true;
        SManager.OSC7.Aud.mute = false;
        SManager.OSC8.Aud.mute = false;
        SManager.OSC9.Aud.mute = false;
        OSC1.KeyboardDown("C");
        yield return new WaitForSeconds(0.1f);
        OSC1.KeyboardUp();
        OSC1.KeyboardDown("G");
        yield return new WaitForSeconds(0.1f);
        OSC1.KeyboardUp();
        yield return new WaitForSeconds(4.8f);
        SManager.OSC7.Aud.mute = true;
        SManager.OSC8.Aud.mute = true;
        SManager.OSC9.Aud.mute = true;
        ChordsCheck = false;
    }

    IEnumerator UnMuteAll()
    {
        OSC2.Octava = 5;
        AllCheck = true;
        SManager.OSC1.Aud.mute = false;
        SManager.OSC2.Aud.mute = false;
        SManager.OSC3.Aud.mute = false;
        SManager.OSC4.Aud.mute = false;
        SManager.OSC5.Aud.mute = false;
        SManager.OSC6.Aud.mute = false;
        SManager.OSC7.Aud.mute = false;
        SManager.OSC8.Aud.mute = false;
        SManager.OSC9.Aud.mute = false;
        SManager.OSC10.Aud.mute = false;
        OSC2.KeyboardDown("C");
        yield return new WaitForSeconds(0.3f);
        OSC2.KeyboardUp();
        OSC2.KeyboardDown("E");
        yield return new WaitForSeconds(0.3f);
        OSC2.KeyboardUp();
         OSC2.KeyboardDown("G");
        yield return new WaitForSeconds(0.3f);
        OSC2.KeyboardUp();
         OSC2.KeyboardDown("C2");
        yield return new WaitForSeconds(0.3f);
        OSC2.KeyboardUp();
       
        yield return new WaitForSeconds(3.8f);
       
        SManager.OSC3.Aud.mute = true;
        SManager.OSC4.Aud.mute = true;
        SManager.OSC5.Aud.mute = true;
        SManager.OSC6.Aud.mute = true;
        SManager.OSC7.Aud.mute = true;
        SManager.OSC8.Aud.mute = true;
        SManager.OSC9.Aud.mute = true;
        AllCheck = false;
    }

    IEnumerator MuteAll()
    {
        OSC2.Octava = 5;
        NoneCheck = true;
        SManager.OSC1.Aud.mute = true;
        SManager.OSC2.Aud.mute = true;
        SManager.OSC3.Aud.mute = true;
        SManager.OSC4.Aud.mute = true;
        SManager.OSC5.Aud.mute = true;
        SManager.OSC6.Aud.mute = true;
        SManager.OSC7.Aud.mute = true;
        SManager.OSC8.Aud.mute = true;
        SManager.OSC9.Aud.mute = true;
        SManager.OSC10.Aud.mute = true;
        
        OSC2.KeyboardDown("C2");
        yield return new WaitForSeconds(0.05f);
        OSC2.KeyboardUp();
        OSC2.KeyboardDown("B");
        yield return new WaitForSeconds(0.05f);
        OSC2.KeyboardUp();
         OSC2.KeyboardDown("A#");
        yield return new WaitForSeconds(0.05f);
        OSC2.KeyboardUp();
         OSC2.KeyboardDown("A"); 
        yield return new WaitForSeconds(0.05f);
        OSC2.KeyboardUp();
       
        yield return new WaitForSeconds(4.8f);
       
        SManager.OSC1.Aud.mute = false;
        SManager.OSC2.Aud.mute = false;
        SManager.OSC10.Aud.mute = false;
       
        NoneCheck = false;
    }

    bool running = true;
    [Range(0.1f, 1f)]
    public float footspeed = 0.3f;
    IEnumerator FootSteps()
    {
       while (running)
       {
            if (isGrounded){
                var rN = (int)Random.Range(0, 6);
                Aud.clip = Clips[rN];
                Aud.volume = Random.Range(0.1f, 0.4f);
                Aud.pitch = Random.Range(0.8f, 0.9f);
                Aud.Play();
                yield return new WaitForSeconds(footspeed);
            }else{
                yield return new WaitForSeconds(footspeed);
            }

       }
       
        
        
    }
}

