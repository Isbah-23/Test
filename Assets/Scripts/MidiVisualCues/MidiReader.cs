//<summary>
// Reads the midi file and loads the notes and check which cues to spawn
//<summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

public class MidiReader : MonoBehaviour
{
    public string midiFilePath; // Path to your MIDI file (relative to StreamingAssets)
    public float playbackSpeed = 1; //playback speed

    private MidiFile midiFile;
    private TempoMap tempoMap; // Tempo information of the MIDI file

    private float currentTime = 0f; // Current playback time in seconds
    private float timeStep = 0.01f; // Time update interval (0.01s)

    private float accumulatedTime = 0f; // Accumulator for time step tracking

    private List<float> startTimes = new List<float>();
    private List<float> endTimes = new List<float>();
    private List<int> noteNumbers = new List<int>();
    private List<bool> playedNotes = new List<bool>();

    private Dictionary<int, Transform> noteSpawners; // Maps note numbers to their 

    //<summary>
    // Initializes the arrays with note information and note spawners
    //<summary>
    void Start()
    {
        // Load the MIDI file
        string fullPath = Application.streamingAssetsPath + "/" + midiFilePath;
        try
        {
            midiFile = MidiFile.Read(fullPath);
            Debug.Log($"Successfully loaded MIDI file: {fullPath}");
            tempoMap = midiFile.GetTempoMap();
            var notes = midiFile.GetNotes();
            foreach (var note in notes)
            {
                float startTime = (float)TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap).TotalSeconds;
                float endTime = (float)TimeConverter.ConvertTo<MetricTimeSpan>(note.EndTime, tempoMap).TotalSeconds;

                startTimes.Add(startTime);
                endTimes.Add(endTime);
                noteNumbers.Add(note.NoteNumber - 21);
                playedNotes.Add(false); // Initialize all cues as not displayed
            }

            // Initialize the note cue spawner mapping
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

    //<summary>
    // Updates cumulative elapsed time and calls notes processor at current time
    //<summary>
    void Update()
    {
        accumulatedTime += (Time.deltaTime * playbackSpeed);

        if (accumulatedTime >= timeStep)
        {
            currentTime += accumulatedTime;
            accumulatedTime = 0f;
            ProcessNotesAtCurrentTime();
        }
    }

    //<summary>
    // checks which note cue is to be spawned at given time
    //<summary>
    void ProcessNotesAtCurrentTime()
    {
        for (int i = 0; i < startTimes.Count; i++)
        {
            if (startTimes[i] <= currentTime && !playedNotes[i])
            {
                float noteDuration = endTimes[i] - startTimes[i];
                if (noteSpawners.TryGetValue(noteNumbers[i], out Transform spawner))
                {
                    NoteSpawningScript spawnerScript = spawner.GetComponent<NoteSpawningScript>();
                    if (spawnerScript != null)
                    {
                        spawnerScript.givenSpawnLength = noteDuration;
                        spawnerScript.SpawnNote(noteDuration* (1/playbackSpeed)); // note falls 1 unit length per unit delta time
                    }
                    else
                    {
                        Debug.LogError($"Spawner {spawner.name} is missing the NoteSpawningScript!");
                    }
                }
                else
                {
                    Debug.LogWarning($"No spawner found for note {noteNumbers[i]}!");
                }
                playedNotes[i] = true;
            }
        }
    }
}