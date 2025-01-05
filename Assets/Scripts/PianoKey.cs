using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PianoKey : MonoBehaviour
{
    // Parameters for key press and release
    private Quaternion originalRotation; 
    private readonly float rotationAngle = 7f; // angle of rotation for key press
    private readonly float releaseSpeed = 10f;
    public KeyCode keyboardKey; // temporary
    private bool isPressed = false;
    
    // Parameters for sound playback
    public AudioClip pianoSound; 
    private AudioSource audioSource;
    private Coroutine fadeCoroutine;


    // Parameters for color change
    private readonly bool changeColor = true; // true = color of key changes when pressed
    public Color pressedColor = Color.yellow; // temporary
    private Color originalColor;
    private Renderer keyRenderer; 

    // Parameters for sustain and fade-out
    private readonly float sustainTime = 2f; // min time the key must be pressed to play the sound fully
    private readonly float fadeOutDuration = 2f; 
    private float keyPressDuration = 0f;

    private PlayerControllers controls;

   void Start()
    {
        controls = new PlayerControllers();
        controls.Enable();
        
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
        // check for input
        if (/*Input.GetKeyDown(keyboardKey) Input.GetMouseButtonDown(0) */ controls.Interact.Interact.WasPerformedThisFrame() && !isPressed)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
            {
                PressKey();
            }
            // PressKey();
        }
        else if (/*Input.GetKeyUp(keyboardKey) Input.GetMouseButtonUp(0)*/ controls.Interact.Interact.WasReleasedThisFrame() && isPressed)
        {
            ReleaseKey();
        }

        if (isPressed)
        {
            keyPressDuration += Time.deltaTime;
        }
        else
        {
            keyPressDuration = 0f;
        }

        if (!isPressed)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, originalRotation, Time.deltaTime * releaseSpeed);
            keyRenderer.material.color = originalColor;
        }
    }

    public void PressKey()
    {
        // rotate the key downwards
        transform.localRotation = originalRotation * Quaternion.Euler(-rotationAngle, 0, 0);
        isPressed = true;

        if (pianoSound != null)
        {
            // audioSource.Play();
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            audioSource.volume = 1f;
            audioSource.Play();
        }

        if (keyRenderer != null && changeColor)
        {
            keyRenderer.material.color = pressedColor;
        }
    }

    public void ReleaseKey()
    {
        // transform.localRotation = originalRotation;
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
