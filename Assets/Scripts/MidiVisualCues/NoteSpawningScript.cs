//<summary>
// Handles note spawning logic
//<summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawningScript : MonoBehaviour
{
    public GameObject notePrefab;
    public float spawnTimestamp = 2f;
    public float givenSpawnLength;
    public float zOffset = 0;

    //<summary>
    // Spawns note of given length
    // spawnLength - length (y-scale) of the note to be spawned
    //<summary>
    public GameObject SpawnNote(float spawnLength)
    {
        Vector3 spawnPosition = transform.position;
        // Adjust the spawn position to align the bottom edges of notes
        Renderer noteRenderer = notePrefab.GetComponent<Renderer>();
        if (noteRenderer != null)
        {
            float noteHeight = noteRenderer.bounds.size.y;
            spawnPosition.y += spawnLength * 0.5f; // 0.5 = half up
        }
        //Quaternion rotation = Quaternion.Euler(customRotation);
        //GameObject note = Instantiate(notePrefab, spawnPosition, rotation);
        Quaternion additionalRotation = Quaternion.Euler(0f, 90f, 0f);
        Quaternion finalRotation = transform.rotation * additionalRotation;

        GameObject note = Instantiate(notePrefab, spawnPosition, finalRotation);// Quaternion.identity);
        GameObject pianoAndCues = GameObject.FindWithTag("PianoAndCues");
        Vector3 scale = note.transform.localScale;
        note.transform.position = new Vector3(note.transform.position.x, note.transform.position.y, note.transform.position.z + zOffset);
        scale.x *= pianoAndCues.transform.localScale.x;
        scale.y = spawnLength;
        scale.z *= pianoAndCues.transform.localScale.z;
        note.transform.localScale = scale;

        return note;
    }
}
