using UnityEngine;

public class PianoKey : MonoBehaviour
{
    // Parameters for key press and release
    private Quaternion originalRotation; 
    public float rotationAngle = 5f; // angle of rotation for key press
    public float releaseSpeed = 7f;
    public KeyCode keyboardKey; // temporary
    private bool isPressed = false;
    
    // Parameters for sound playback
    public AudioClip pianoSound; 
    private AudioSource audioSource;

    // Parameters for color change
    private readonly bool changeColor = true; // true = color of key changes when pressed
    public Color pressedColor = Color.yellow; // temporary
    private Color originalColor;
    private Renderer keyRenderer; 

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
        // check for input
        if (Input.GetMouseButtonDown(0) /*Input.GetKeyDown(keyboardKey)*/ && !isPressed)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
            {
                PressKey();
            }
            // PressKey();
        }
        else if (/*Input.GetKeyUp(keyboardKey)*/ Input.GetMouseButtonUp(0) && isPressed)
        {
            ReleaseKey();
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
    }
}
