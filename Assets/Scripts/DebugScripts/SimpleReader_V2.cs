//<summary>
// Reads the midi file and loads the notes and check which cues to spawn
//<summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.IO;
using UnityEngine.Networking;
using TMPro;
using System.Linq;

class SimpleReader_V2 : MonoBehaviour
{
    private string midiFilePath;
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
    private List<GameObject> spawnedNoteObjects = new List<GameObject>();

    private Dictionary<int, Transform> noteSpawners = new Dictionary<int, Transform>(); // Maps note numbers to their spawners

    float n = 0.3f; // should match n in NoteFallingScript - for note length

    // for practice mode
    private bool practiceMode = true;
    public static bool isPlaying = true;
    float time_diff = 2.95f;
    private GameObject grandPiano;
    private Dictionary<int, PianoKey> pianoKeysDict = new Dictionary<int, PianoKey>(); // Dictionary to store key references
    private bool allKeysPressed;

    private float overpress_leniency = 0.09f;
    private float[] tap_time = {0.01f, 0.5f};
    private float[] hold_time = {1.0f, 2.0f}; //Adjust as needed
    private float[] long_hold_time = {2.0f, 2.5f};
    List<(int key, int noteNumber, int press_type, bool started_playing, float end_time, float pressTime, float activationTime)> activeNotes = new List<(int, int, int, bool, float, float, float)>();

    //<summary>
    // Initializes the arrays with note information and note spawners
    //<summary>    
    void Start()
    {
        midiFilePath = "happy_birthday.midi";
        Debug.Log($"Button clicked with path: {midiFilePath}");

        time_diff = time_diff * playbackSpeed;

        // Load the MIDI file asynchronously
        StartCoroutine(LoadMidiFile(midiFilePath));

        for (int i = 1; i <= 88; i++)
        {
            Transform spawner = transform.Find($"NoteSpawner{i}");
            if (spawner != null)
            {
                noteSpawners.Add(i, spawner);
                Debug.Log($"NoteSpawner{i} found");
            }
            else
            {
                Debug.LogWarning($"NoteSpawner{i} not found in the hierarchy!");
            }
        }
        grandPiano = GameObject.Find("GrandPiano");
        if (grandPiano != null)
            InitializeKeys();
        else
            Debug.LogError("GrandPiano object not found in the scene!");
    }

    private IEnumerator LoadMidiFile(string fileName)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, fileName);

        #if UNITY_ANDROID
        fullPath = Application.streamingAssetsPath + "/" + fileName; // Correct Android path
        #endif

        Debug.Log($"Attempting to load MIDI file from: {fullPath}");

        using (UnityWebRequest request = UnityWebRequest.Get(fullPath))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error loading MIDI file: {request.error}");
                yield break;
            }

            try
            {
                byte[] midiData = request.downloadHandler.data;
                using (MemoryStream midiStream = new MemoryStream(midiData))
                {
                    midiFile = MidiFile.Read(midiStream);
                }

                Debug.Log($"Successfully loaded MIDI file: {fileName}");

                tempoMap = midiFile.GetTempoMap();
                var notes = midiFile.GetNotes();
                foreach (var note in notes)
                {
                    float startTime = (float)TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap).TotalSeconds;
                    float endTime = (float)TimeConverter.ConvertTo<MetricTimeSpan>(note.EndTime, tempoMap).TotalSeconds;

                    startTimes.Add(startTime);
                    endTimes.Add(endTime);
                    noteNumbers.Add(note.NoteNumber - 21);
                    playedNotes.Add(false);
                    spawnedNoteObjects.Add(null);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error processing MIDI file: {ex.Message}");
            }
        }
    }

    void InitializeKeys()
    {
        Transform pianoKeysTransform = grandPiano.transform.Find("PianoKeys");
        for (int i = 1; i <= 88; i++)
        {
            string keyName = "PianoKey." + i.ToString("D3");  // Construct key name, e.g. "PianoKey.001"
            Transform keyTransform = pianoKeysTransform.transform.Find(keyName);

            if (keyTransform != null)
            {
                PianoKey keyScript = keyTransform.GetComponent<PianoKey>();

                if (keyScript != null)
                    pianoKeysDict.Add(i, keyScript);
                else
                    Debug.LogError("PianoKey script not found on " + keyName);
            }
            else
                Debug.LogError("Key " + keyName + " not found in the hierarchy.");
        }
    }

    bool CheckKeysPressed()
    {
        HashSet<int> expectedKeys = new HashSet<int>(activeNotes.Select(n => n.noteNumber));
        bool allKeysPressed = true;

        // Debug.Log("Checking for keys pressed");
        foreach (var keyEntry in pianoKeysDict)
        {
            int key = keyEntry.Key;
            bool isKeyExpected = expectedKeys.Contains(keyEntry.Key);
            bool isKeyPressed = keyEntry.Value.isPressed;

            if (!isKeyExpected) // for keys that should not have been pressed
            {
                if (isKeyPressed)
                {
                    // Debug.Log("Unexpected Key pressed");
                    // If an unexpected key is pressed, fail and change color
                    keyEntry.Value.ChangeKeyColor(false);
                    allKeysPressed = false;
                }
            }
            else // For keys that shouldve been pressed
            {
                int index = activeNotes.FindIndex(n => n.noteNumber == keyEntry.Key);
                if (index != -1) // Find the index of the key in played list 
                {
                    var (param_key, noteNumber, pressType, startedPlaying, endingTime, pressTime, activationTime) = activeNotes[index];
                    if (isKeyPressed)
                    {
                        startedPlaying = true;
                        pressTime += (Time.deltaTime * playbackSpeed); // increase press time
                        keyEntry.Value.ChangeKeyColor(true);
                    }
                    else if (!isKeyPressed)
                    {
                        if (startedPlaying) // if the player ever started playing the key
                        {
                            if (CheckPressTime(pressTime, pressType))
                            {
                                activeNotes.RemoveAt(index);
                                Destroy(spawnedNoteObjects[param_key]);
                            }
                            else
                            {
                                //rollback logic
                                Debug.LogWarning("They see me rolling, they hating");
                                // move all notes up by given time dist
                                for (int i = 0; i < spawnedNoteObjects.Count; i++) // ismai btaon msla kya hai, for when multiple notes were pressed, aik hi har reset hoye ga
                                {
                                    if (spawnedNoteObjects[i] != null)
                                        spawnedNoteObjects[i].GetComponent<NoteFallingScript>().Rewind(currentTime - activationTime);
                                }
                                activeNotes[index] = (param_key, noteNumber, pressType, false, endingTime, 0.0f, activationTime); // hard reset this note
                                currentTime = activationTime; // hard reset current time as well
                            }
                        }
                        else
                        {
                            if (endingTime - currentTime <= GetMinPressTime(pressType) + 0.02f) //0.02f for floating point precision issues
                            {
                                allKeysPressed = false;
                            }
                        }
                    }
                }
            }
        }

        return allKeysPressed;
    }

    void CheckKeysPressedAlt(List<int> keyNumbersToCheck)
    {
        HashSet<int> keysToCheckSet = new HashSet<int>(keyNumbersToCheck);

        foreach (var keyEntry in pianoKeysDict)
        {
            bool isKeyInList = keysToCheckSet.Contains(keyEntry.Key);
            bool isKeyPressed = keyEntry.Value.isPressed;

            if (isKeyInList)
            {
                if (isKeyPressed)
                    keyEntry.Value.ChangeKeyColor(true);
            }
            else
            {
                if (isKeyPressed)
                    keyEntry.Value.ChangeKeyColor(false);
            }
        }
    }

    //<summary>
    // Updates cumulative elapsed time and calls notes processor at current time
    //<summary>
    void Update()
    {
        // if (!isStarted) return;
        accumulatedTime += (Time.deltaTime * playbackSpeed);
        if (accumulatedTime >= timeStep)
        {
            if (practiceMode)
            {
                UpdateActiveNotesAtKeys();
                isPlaying = CheckKeysPressed(); 
                if (isPlaying){
                    currentTime += accumulatedTime;
                    ProcessNotesAtCurrentTime();
                }
            }
            else
            {
                // will light up with Practice mode off bhi ab
                isPlaying = true; // but wont stop
                CheckKeysPressedAlt(UpdateActiveNotesAtKeysAlt());
                currentTime += accumulatedTime;
                ProcessNotesAtCurrentTime();
            }
            accumulatedTime = 0f;
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
                if (!noteSpawners.ContainsKey(noteNumbers[i]))
                {
                    Debug.LogError($"noteSpawners does not contain key {noteNumbers[i]}");
                }
                if (noteSpawners.TryGetValue(noteNumbers[i], out Transform spawner))
                {
                    NoteSpawningScript spawnerScript = spawner.GetComponent<NoteSpawningScript>();
                    if (spawnerScript != null)
                    {
                        spawnerScript.givenSpawnLength = noteDuration;
                        GameObject note = spawnerScript.SpawnNote(noteDuration* (1/playbackSpeed) * n); // note falls 1 unit length per unit delta time
                        spawnedNoteObjects[i] = note;
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

    void UpdateActiveNotesAtKeys()
    {
        List<int> activeNotesTemp = new List<int>();
        HashSet<int> existingNotes = new HashSet<int>(activeNotes.Select(n => n.noteNumber)); // Store active notes for quick lookup

        for (int i = 0; i < startTimes.Count; i++)
        {
            float noteReachTime = startTimes[i] + time_diff;

            if (currentTime >= noteReachTime)
            {
                int noteNumber = noteNumbers[i];
                activeNotesTemp.Add(noteNumber);
                float endingTime = noteReachTime + ((endTimes[i] - startTimes[i])) + overpress_leniency;
                // Add the note to the activeNotes list only if it's not already in the list
                if (!existingNotes.Contains(noteNumber))
                {
                    float noteLength = endTimes[i] - startTimes[i];
                    activeNotes.Add((i, noteNumber, GetPressType(endTimes[i]-startTimes[i]), false, endingTime, 0.0f, currentTime)); // values for note
                    existingNotes.Add(noteNumber); // Update the HashSet to include the new note
                    // Debug.Log($"Added note {noteNumber} to List");
                }
            }
        }
    }

    List<int> UpdateActiveNotesAtKeysAlt()
    {
        List<int> activeNotesAlt = new List<int>();

        for (int i = 0; i < startTimes.Count; i++)
        {
            float noteReachTime = startTimes[i] + time_diff;
            float noteEndTime = noteReachTime + ((endTimes[i] - startTimes[i]));

            if (playedNotes[i] && currentTime >= noteReachTime && currentTime <= noteEndTime)
            {
                activeNotesAlt.Add(noteNumbers[i]);
            }
        }

        // Debug.Log("Active notes at key level: " + string.Join(", ", activeNotes));
        return activeNotesAlt;
    }

    int GetPressType(float note_length)
    {
        if (note_length > 2.0f)
        {    
            Debug.Log("Note is of type 2");
            return 2;
        }
        if (note_length > 1.0f)
        {
            Debug.Log("Note is of type 1");
            return 1;
        }
        Debug.Log("Note is of type 0");
        return 0;
    }

    bool CheckPressTime(float pressTime, int pressType)
    {
        if(pressType == 0)
        {
            return (pressTime > tap_time[0] && pressTime < tap_time[1])  ;
        }
        if (pressType == 1)
        {
            return (pressTime > hold_time[0] && pressTime < hold_time[1]);
        }
        if (pressType == 2)
        {
            return (pressTime > long_hold_time[0] && pressTime < long_hold_time[1]);
        }
        return false;
    }

    float GetMinPressTime(int pressType)
    {
        if(pressType == 0)
        {
            return tap_time[0];
        }
        if (pressType == 1)
        {
            return hold_time[0];
        }
        if (pressType == 2)
        {
            return long_hold_time[0];
        }
        return 0.0f;
    }
}