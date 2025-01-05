using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

public class MidiReader : MonoBehaviour
{
    public string midiFilePath; // Path to your MIDI file (relative to StreamingAssets)

    private MidiFile midiFile;
    private TempoMap tempoMap; // Tempo information of the MIDI file

    private float currentTime = 0f; // Current playback time in seconds
    private float timeStep = 0.01f; // Time update interval (0.01s)

    private float accumulatedTime = 0f; // Accumulator for time step tracking

    private List<float> startTimes = new List<float>();
    private List<float> endTimes = new List<float>();
    private List<int> noteNumbers = new List<int>();
    private List<bool> playedNotes = new List<bool>();

    private Dictionary<int, Transform> noteSpawners; // Maps note numbers to their spawners

    void Start()
    {
        // Load the MIDI file
        string fullPath = Application.streamingAssetsPath + "/" + midiFilePath;
        try
        {
            midiFile = MidiFile.Read(fullPath);
            Debug.Log($"Successfully loaded MIDI file: {fullPath}");

            // Get tempo map and notes
            tempoMap = midiFile.GetTempoMap();
            var notes = midiFile.GetNotes();

            // Populate the arrays with note data
            foreach (var note in notes)
            {
                float startTime = (float)TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap).TotalSeconds;
                float endTime = (float)TimeConverter.ConvertTo<MetricTimeSpan>(note.EndTime, tempoMap).TotalSeconds;

                startTimes.Add(startTime);
                endTimes.Add(endTime);
                noteNumbers.Add(note.NoteNumber - 21); // Map MIDI note numbers to 1-88 range
                playedNotes.Add(false); // Initialize all notes as not played
            }

            // Initialize the note spawner mapping
            noteSpawners = new Dictionary<int, Transform>();
            for (int i = 1; i <= 88; i++)
            {
                Transform spawner = transform.Find($"NoteSpawner{i}");
                if (spawner != null)
                {
                    noteSpawners.Add(i, spawner);
                }
                else
                {
                    Debug.LogWarning($"NoteSpawner{i} not found in the hierarchy!");
                }
            }

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading MIDI file: {ex.Message}");
        }
    }

    void Update()
    {
        accumulatedTime += Time.deltaTime;

        if (accumulatedTime >= timeStep)
        {
            currentTime += accumulatedTime;
            accumulatedTime = 0f;
            ProcessNotesAtCurrentTime();
        }
    }

    void ProcessNotesAtCurrentTime()
    {
        for (int i = 0; i < startTimes.Count; i++)
        {
            // Check if the note's start time is less than or equal to the current time and hasn't been played
            if (startTimes[i] <= currentTime && !playedNotes[i])
            {
                // Calculate note duration and play the note
                float noteDuration = endTimes[i] - startTimes[i];
                if (noteSpawners.TryGetValue(noteNumbers[i], out Transform spawner))
                {
                    NoteSpawningScript spawnerScript = spawner.GetComponent<NoteSpawningScript>();
                    if (spawnerScript != null)
                    {
                        spawnerScript.givenSpawnLength = noteDuration;
                        spawnerScript.SpawnNote(noteDuration);
                    }
                    else
                    {
                        Debug.LogError($"Spawner {spawner.name} is missing the NoteSpawningScript!");
                    }
                }
                else
                {
                    //Debug.LogWarning($"No spawner found for note {noteNumbers[i]}!");
                }

                // Mark the note as played
                playedNotes[i] = true;
            }
        }
    }
}