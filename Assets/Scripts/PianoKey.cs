
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Text.RegularExpressions; // for incorrect key press log

public class PianoKey : MonoBehaviour, IInteractable
{
    // Parameters for key press and release
    private Quaternion originalRotation; 
    private readonly float rotationAngle = 7f; // angle of rotation for key press
    private readonly float releaseSpeed = 10f;
    public KeyCode keyboardKey; // temporary
    public bool isPressed = false; // changing accessibility cause i need to check state for visual cues ~Isbah
    // private bool isReleased = false;
    
    // Parameters for sound playback
    public AudioClip pianoSound; 
    private AudioSource audioSource;
    private Coroutine fadeCoroutine;


    // Parameters for color change
    private readonly bool changeColor = true; // true = color of key changes when pressed
    // public Color pressedColor = Color.yellow; // temporary
    private Color pressedColor = Color.green;
    private Color wrongPress = Color.red;
    private Color originalColor;
    private Renderer keyRenderer; 

    // Parameters for sustain and fade-out
    private readonly float sustainTime = 2f; // min time the key must be pressed to play the sound fully
    private readonly float fadeOutDuration = 2f; 
    private float keyPressDuration = 0f;

    //For VR
    private XRBaseInteractable interactable;

    // for reader
    public int colorValue = 0; // 0 - original, 1 - red, 2 - green

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelected);
            interactable.selectExited.AddListener(OnUnselected);
        }
        
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelected);
            interactable.selectExited.RemoveListener(OnUnselected);
        }
    }

    public void OnSelected(SelectEnterEventArgs args)
    {
        Interact();
    }
    public void OnUnselected(SelectExitEventArgs args)
    {
        InteractReleased();
    }

   void Start()
    {
        originalRotation = transform.localRotation;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = pianoSound;
        audioSource.playOnAwake = false;

        keyRenderer = GetComponent<Renderer>();
        if (keyRenderer != null)
        {
            originalColor = keyRenderer.material.color;
        }

        if (!GetComponent<Collider>())
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    void Update()
    {
        if (isPressed)
        {
            keyPressDuration += Time.deltaTime;
        }
        else //if (isReleased)
        {
            keyPressDuration = 0f;
                
            transform.localRotation = Quaternion.Slerp(transform.localRotation, originalRotation, Time.deltaTime * releaseSpeed);
            keyRenderer.material.color = originalColor;
            if (colorValue == 1) // log the end time of incorrect press
            {
                Match match = Regex.Match(gameObject.name, @"\.(\d+)$");
                if (match.Success)
                {
                    int noteNum = int.Parse(match.Groups[1].Value);
                    // SimpleReader_V2.LogIncorrectPressWrapper(noteNum - 1);
                    MidiReader.LogIncorrectPressWrapper(noteNum - 1);
                }
            }
            colorValue = 0;
            // isReleased = false;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        PressKey();
    }

    public void Interact()
    {
        PressKey();
    }

    public void InteractReleased()
    {
        ReleaseKey();
    }

    public void PressKey()
    {
        // Debug.Log("Press Key called");
        // rotate the key downwards
        transform.localRotation = originalRotation * Quaternion.Euler(-rotationAngle, 0, 0);
        isPressed = true;

        if (pianoSound != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            audioSource.volume = 1f;
            audioSource.Play();
        }

        // if (keyRenderer != null && changeColor)
        // {
        //     keyRenderer.material.color = pressedColor;
        // }
    }

    public void ChangeKeyColor(bool type)
    {
        if (keyRenderer != null && changeColor && type)
        {
            keyRenderer.material.color = pressedColor;
            colorValue = 2;
        }
        else if (keyRenderer != null && changeColor && !type)
        {
            keyRenderer.material.color = wrongPress;
            colorValue = 1;
        }
    }

    public void ReleaseKey()
    {
        // Debug.Log("Release Key called");
        // isReleased = true;
        isPressed = false;

        if (keyPressDuration < sustainTime && audioSource.isPlaying)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeOutAudio());
        }
    }

    private IEnumerator FadeOutAudio()
    {
        float startVolume = audioSource.volume;

        for (float t = 0; t < fadeOutDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeOutDuration);
            yield return null;
        }

        audioSource.volume = 0;
        audioSource.Stop();
    }
}
