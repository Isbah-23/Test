//<summary>
// Handles individual note behaviour
//<summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteFallingScript : MonoBehaviour
{
    public float velocity = 1.0f;
    float maxY = 2.086f;
    float n = 0.3f; // should match n in MidiReader - for note length
    GameObject PianoAndCues;

    void Start()
    {
        PianoAndCues = GameObject.FindWithTag("PianoAndCues");
        maxY = PianoAndCues.transform.position.y - PianoAndCues.transform.localScale.y * 1f;//2.086f;
    }

    //<summary>
    // Handles falling velocity of note and its destruction
    //<summary>
    void Update()
    {
        // if (!MidiReader.isPlaying) return;
        if (!SimpleReader.isPlaying) return; // -- for testing purposes in Classic Scene

        transform.Translate(Vector3.down * velocity * n * Time.deltaTime); // note falls 1 unit length per unit delta time
        float topEdgeY = transform.position.y + (transform.localScale.y * 0.5f);
        if (topEdgeY < maxY)
        {
            Destroy(gameObject);
        }
    }

    public void Rewind(float timeStepCount)
    {
        transform.position += Vector3.up * (velocity * n * timeStepCount);
    }
}
