using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    
    public float moveDistance = 1f;
    public float jumpForce = 5.0f;      // Force applied to jump
    public bool isGrounded;      // Check if the player is on the ground

    private Animator animator;
    private int movement = 1;
    private int currentLane = 1;
    private const int MinLane = 0;
    private const int MaxLane = 2;

    private Rigidbody rb;

    public Vector3 center;

    public SoundManager1 SManager;

    public int Score= 0;

    public TextMeshProUGUI ScoreText;

    bool SnareCheck = false, BassCheck = false, ChordsCheck = false, MelodyCheck = false, 
                    KickCheck = false, AllCheck = false, NoneCheck = false ;

    public Osc OSC1, OSC2, OSC3;

    public AudioSource Aud;

    public AudioClip[] Clips;

    private RectTransform mobileControlsRoot;

    void Start()
    {
        rb = GetComponent<Rigidbody>();  // Get the Rigidbody2D component for physics movement
        animator = GetComponent<Animator>();
        center = transform.position;
        SetupMobileControls();
        StartCoroutine(FootSteps());
        //animator.SetInteger("movement", movement);
        
    }

    void Update()
    {
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            ProceduralSynthVoice.TryUnlockWebAudio();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveToLane(currentLane - 1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveToLane(currentLane + 1);
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

    void MoveToLane(int lane)
    {
        currentLane = Mathf.Clamp(lane, MinLane, MaxLane);

        float xOffset = (currentLane - 1) * moveDistance;
        Vector3 position = transform.position;
        position.x = center.x + xOffset;
        transform.position = position;
    }

    public void MoveLeftButtonPressed()
    {
        ProceduralSynthVoice.TryUnlockWebAudio();
        MoveToLane(currentLane - 1);
    }

    public void MoveRightButtonPressed()
    {
        ProceduralSynthVoice.TryUnlockWebAudio();
        MoveToLane(currentLane + 1);
    }

    private void SetupMobileControls()
    {
        if (!ShouldShowMobileControls())
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject controlsRootObject = new GameObject("MobileControls", typeof(RectTransform));
        controlsRootObject.transform.SetParent(canvas.transform, false);
        mobileControlsRoot = controlsRootObject.GetComponent<RectTransform>();
        mobileControlsRoot.anchorMin = new Vector2(0, 0);
        mobileControlsRoot.anchorMax = new Vector2(1, 0);
        mobileControlsRoot.pivot = new Vector2(0.5f, 0);
        mobileControlsRoot.anchoredPosition = new Vector2(0, 16f);
        mobileControlsRoot.sizeDelta = new Vector2(0, 120f);

        CreateMobileButton("LeftButton", "◀", new Vector2(0, 0), new Vector2(0, 0), new Vector2(90, 90), new Vector2(70, 60), MoveLeftButtonPressed);
        CreateMobileButton("RightButton", "▶", new Vector2(1, 0), new Vector2(1, 0), new Vector2(90, 90), new Vector2(-70, 60), MoveRightButtonPressed);
    }

    private bool ShouldShowMobileControls()
    {
        if (Application.isMobilePlatform)
        {
            return true;
        }

        return Application.platform == RuntimePlatform.WebGLPlayer && Input.touchSupported;
    }

    private void CreateMobileButton(string objectName, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        if (mobileControlsRoot == null)
        {
            return;
        }

        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(mobileControlsRoot, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = size;
        buttonRect.anchoredPosition = anchoredPosition;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.1f, 0.35f, 0.75f, 0.72f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
        colors.pressedColor = new Color(0.85f, 0.9f, 1f, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        button.colors = colors;
        button.onClick.AddListener(action);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = textObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.font = TMP_Settings.defaultFontAsset;
        labelText.fontSize = 42;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        labelText.raycastTarget = false;
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
        OSC1.OctaveChange(6);
        
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
        OSC1.OctaveChange(6);
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
       OSC1.OctaveChange(6);
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
        OSC1.OctaveChange(6);
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
        OSC2.OctaveChange(5);
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
        OSC2.OctaveChange(5);
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
