//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Melanchall.DryWetMidi.Core;
//using Melanchall.DryWetMidi.Interaction;

//public class MidiReader : MonoBehaviour
//{
//    public string midiFilePath; // Path to your MIDI file (relative to StreamingAssets)
//    public float readFrequency = 10f; // How many times per second to read
//    private MidiFile midiFile;
//    private float currentTime = 0f; // Current simulated playback time (in seconds)
//    private float interval; // Time interval between reads (1/readFrequency)

//    private IEnumerable<Note> notes; // All notes in the MIDI file
//    private TempoMap tempoMap; // Tempo information of the MIDI file

//    private Dictionary<int, Transform> noteSpawners; // Maps note numbers to their spawners
//    private HashSet<Note> spawnedNotes; // Tracks notes that have already been spawned

//    void Start()
//    {
//        // Calculate interval for the desired frequency
//        interval = 1f / readFrequency;

//        // Load the MIDI file
//        string fullPath = Application.streamingAssetsPath + "/" + midiFilePath;
//        try
//        {
//            midiFile = MidiFile.Read(fullPath);
//            Debug.Log($"Successfully loaded MIDI file: {fullPath}");

//            // Get tempo map and notes
//            tempoMap = midiFile.GetTempoMap();
//            notes = midiFile.GetNotes();

//            // Initialize the note spawner mapping
//            noteSpawners = new Dictionary<int, Transform>();
//            for (int i = 1; i <= 88; i++)
//            {
//                Transform spawner = transform.Find($"NoteSpawner{i}");
//                if (spawner != null)
//                {
//                    noteSpawners.Add(i, spawner);
//                }
//                else
//                {
//                    Debug.LogWarning($"NoteSpawner{i} not found in the hierarchy!");
//                }
//            }

//            // Initialize the spawned notes tracker
//            spawnedNotes = new HashSet<Note>();
//        }
//        catch (System.Exception ex)
//        {
//            Debug.LogError($"Error loading MIDI file: {ex.Message}");
//        }
//    }

//    void Update()
//    {
//        // Simulate playback at the desired frequency
//        currentTime += Time.deltaTime;

//        // Check for notes playing at the current time
//        if (midiFile != null)
//        {
//            ReadNotesAtTime(currentTime);
//        }
//    }

//    void ReadNotesAtTime(float timeInSeconds)
//    {
//        // Convert seconds to MIDI ticks using the tempoMap
//        var metricTime = new MetricTimeSpan(0, 0, Mathf.FloorToInt(timeInSeconds));
//        long midiTime = TimeConverter.ConvertFrom(metricTime, tempoMap);

//        foreach (var note in notes)
//        {
//            // Check if the note is active and not already spawned
//            if (note.Time <= midiTime && note.EndTime > midiTime && !spawnedNotes.Contains(note))
//            {
//                float noteDuration = (float)(note.Length / 1000000.0); // Convert microseconds to seconds
//                int noteNumber = note.NoteNumber - 21; // Map MIDI note numbers to 1-88 range

//                // Call the corresponding spawner if it exists
//                if (noteSpawners.TryGetValue(noteNumber, out Transform spawner))
//                {
//                    NoteSpawningScript spawnerScript = spawner.GetComponent<NoteSpawningScript>();
//                    if (spawnerScript != null)
//                    {
//                        spawnerScript.givenSpawnLength = noteDuration;
//                        Debug.Log("Spawned note");
//                        spawnerScript.SpawnNote(noteDuration);
//                    }
//                    else
//                    {
//                        Debug.LogError($"Spawner {spawner.name} is missing the NoteSpawningScript!");
//                    }
//                }
//                else
//                {
//                    Debug.LogWarning($"No spawner found for note {noteNumber}!");
//                }

//                // Mark the note as spawned
//                spawnedNotes.Add(note);
//            }
//        }
//    }
//}
