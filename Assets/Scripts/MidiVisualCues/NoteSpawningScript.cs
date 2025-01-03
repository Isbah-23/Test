using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawningScript : MonoBehaviour
{
    public GameObject notePrefab;
    public float spawnTimestamp = 2f;
    public float givenSpawnLength;
    private float timer = 0f;
    private bool spawn = true;
    void Update() // Logic check, remove when done making spawner control script in parent
    {
        timer += Time.deltaTime;
        if (spawn && timer >= spawnTimestamp)
        {
            SpawnNote(givenSpawnLength);
            spawn = false;
        }
    }
    void SpawnNote(float spawnLength)
    {
        Vector3 spawnPosition = transform.localPosition;
        // Adjust the spawn position to align the bottom edges of notes
        Renderer noteRenderer = notePrefab.GetComponent<Renderer>();
        if (noteRenderer != null)
        {
            float noteHeight = noteRenderer.bounds.size.y;
            spawnPosition.y += spawnLength * 0.5f;
        }
        Vector3 customRotation = new Vector3(0f, 90f, 0f);
        Quaternion rotation = Quaternion.Euler(customRotation);
        GameObject note = Instantiate(notePrefab, spawnPosition, rotation);
        Vector3 scale = note.transform.localScale;
        scale.y = spawnLength;
        note.transform.localScale = scale;
    }
}
