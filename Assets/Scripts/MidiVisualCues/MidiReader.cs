
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

public class MidiReader : MonoBehaviour
{

    private string midiFilePath;
    public TMPro.TextMeshProUGUI selectedSongText;
    public GameObject practiceModeButton;
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
    private List<bool> done_and_dusted = new List<bool>();

    private Dictionary<int, Transform> noteSpawners = new Dictionary<int, Transform>(); // Maps note numbers to their 

    float n = 0.3f; // should match n in NoteFallingScript - for note length
    
    // for practice mode
    private bool practiceMode = true;
    public static bool isPlaying = false;
    float time_diff = 2.95f;
    private GameObject grandPiano;
    private Dictionary<int, PianoKey> pianoKeysDict = new Dictionary<int, PianoKey>(); // Dictionary to store key references
    private bool allKeysPressed;
    private bool isStarted = false;

    private float[] tap_time = {0.01f, 0.3f};
    private float[] hold_time = {0.3f, 0.6f}; //Adjust as needed
    private float[] long_hold_time = {0.6f, Mathf.Infinity};
    List<(int key, int noteNumber, int press_type, float press_time, float note_reach_time, float end_time)> activeNotes = new List<(int, int, int, float, float, float)>();
    
    // for practice mode
    private int hardScore = 0;
    // for play mode
    float total_score = 0;
    float obtained_score = 0;
    float song_end_time = 0f;

    string[] pianoNotesNames = new string[88]
    {
        "A0", "A#0/Bb0", "B0",
        "C1", "C#1/Db1", "D1", "D#1/Eb1", "E1", "F1", "F#1/Gb1", "G1", "G#1/Ab1",
        "A1", "A#1/Bb1", "B1",
        "C2", "C#2/Db2", "D2", "D#2/Eb2", "E2", "F2", "F#2/Gb2", "G2", "G#2/Ab2",
        "A2", "A#2/Bb2", "B2",
        "C3", "C#3/Db3", "D3", "D#3/Eb3", "E3", "F3", "F#3/Gb3", "G3", "G#3/Ab3",
        "A3", "A#3/Bb3", "B3",
        "C4", "C#4/Db4", "D4", "D#4/Eb4", "E4", "F4", "F#4/Gb4", "G4", "G#4/Ab4",
        "A4", "A#4/Bb4", "B4",
        "C5", "C#5/Db5", "D5", "D#5/Eb5", "E5", "F5", "F#5/Gb5", "G5", "G#5/Ab5",
        "A5", "A#5/Bb5", "B5",
        "C6", "C#6/Db6", "D6", "D#6/Eb6", "E6", "F6", "F#6/Gb6", "G6", "G#6/Ab6",
        "A6", "A#6/Bb6", "B6",
        "C7", "C#7/Db7", "D7", "D#7/Eb7", "E7", "F7", "F#7/Gb7", "G7", "G#7/Ab7",
        "A7", "A#7/Bb7", "B7",
        "C8"
    };

    public void TogglePracticeMode()
    {
        practiceMode = !practiceMode;
        practiceModeButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = practiceMode ? "Practice Mode: On": "Practice Mode: Off";
    }

    public void DeleteAllNotes()
    {
        GameObject[] glowingNotes = GameObject.FindGameObjectsWithTag("GlowingNote");
        GameObject[] colouredGlowingNotes = GameObject.FindGameObjectsWithTag("ColoredGlowingNote");

        foreach (GameObject note in glowingNotes)
        {
            Destroy(note);
        }

        foreach (GameObject note in colouredGlowingNotes)
        {
            Destroy(note);
        }

        Debug.Log("Deleted all GlowingNote and ColouredGlowingNote objects.");
        isStarted = false;
        currentTime = 0;
        accumulatedTime = 0;
        activeNotes.Clear(); // Removes all items from the list - This should hopefully fix the Practice Mode switch bug
        startTimes.Clear();
        endTimes.Clear();
        noteNumbers.Clear();
        playedNotes.Clear();
        done_and_dusted.Clear();
        spawnedNoteObjects.Clear();
    }

    //<summary>
    // Initializes the arrays with note information and note spawners
    //<summary>    
    public void StartPlaying()
    {
        Debug.Log("Persistent path: " + Application.persistentDataPath);
        string path = Application.persistentDataPath + "/vr_debug.log";
        string log = $"[{System.DateTime.Now}] Logging For this session!\n";
        File.AppendAllText(path, log); // This creates or appends the file

        // string prefix = "Current Song: ";
        string songName = selectedSongText.text;

        // Extract the song name
        // string songName = textValue.Substring(prefix.Length).Trim();

        // Build the file path
        midiFilePath = songName + ".midi";
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
                if ((startTime + time_diff) + ((endTime - startTime)) > song_end_time)
                {
                    song_end_time = (startTime + time_diff) + ((endTime - startTime)) + 1f;
                }
                noteNumbers.Add(note.NoteNumber - 21);
                playedNotes.Add(false);
                spawnedNoteObjects.Add(null);
                done_and_dusted.Add(false);
            }

            isStarted = true;
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
        bool increment_score = true;

        foreach (var keyEntry in pianoKeysDict)
        {
            int key = keyEntry.Key;
            bool isKeyExpected = expectedKeys.Contains(keyEntry.Key); // should the key be pressed?
            bool isKeyPressed = keyEntry.Value.isPressed; // is the key pressed?
            bool keyColor = keyEntry.Value.colorValue; // have we registered the change in state already?
            

            if (!isKeyExpected) // The key should not have been pressed
            {
                if (isKeyPressed) // but it is pressed 
                {
                    if (!keyColor) // first press?
                    {
                        keyEntry.Value.ChangeKeyColor(false);
                    }
                    // Debug.Log($"Point B is stopping for note: {key}");
                    allKeysPressed = false;
                    increment_score = false;
                }
            }
            else // key should have been pressed
            {
                int index = activeNotes.FindIndex(n => n.noteNumber == keyEntry.Key); // get information about the note associated with the key
                if (index != -1) 
                {
                    var (param_key, noteNumber, pressType, pressTime, reachTime, endingTime) = activeNotes[index];
                    if (isKeyPressed)
                    {
                        if (!keyColor) // first press?
                        {
                            keyEntry.Value.ChangeKeyColor(true);
                        }
                        pressTime += (Time.deltaTime * playbackSpeed); // increase press time
                        activeNotes[index] = (param_key, noteNumber, pressType, pressTime, reachTime, endingTime);
                        continue;
                    }
                    else  // but it is not pressed
                    {
                        increment_score = false;
                        if (practiceMode)
                        {
                            if (pressTime > 0.0f) // just released
                            {
                                activeNotes.RemoveAt(index);
                                done_and_dusted[param_key] = true;
                                Destroy(spawnedNoteObjects[param_key]);
                                spawnedNoteObjects[param_key] = null;
                                if (CheckPressTime(pressTime, pressType) == 0) // was pressed for correct amount of time
                                {
                                    hardScore += 1;
                                    Debug.Log($"Good Press! Score: {hardScore}");
                                }
                                else if (CheckPressTime(pressTime, pressType) == 1) // press time was too high
                                {
                                    hardScore -= 1;
                                    Debug.LogWarning($"Too High! Score: {hardScore}");
                                }
                                else if (CheckPressTime(pressTime, pressType) == -1) // press time was too low
                                {
                                    hardScore -= 1;
                                    Debug.LogWarning($"Too Low! Score: {hardScore}");
                                }
                            }
                            else // was never pressed
                            {
                                if (reachTime - currentTime <= -0.2f)
                                {
                                    // Debug.Log($"Point A is stopping for note: {noteNumber}");
                                    allKeysPressed = false; // stop till user plays that note
                                }
                            }
                        }
                        if (endingTime - currentTime <= 0)
                        {
                            activeNotes.RemoveAt(index);
                            if (spawnedNoteObjects[param_key] != null)
                                Destroy(spawnedNoteObjects[param_key]);
                            done_and_dusted[param_key] = true;
                            // Debug.Log($"{noteNumber} should be removed");
                            // PrintActiveNotes("Out of bounds");
                        }
                    }
                }
            }
        }

        total_score += 1;
        if (increment_score)
            obtained_score += 1;

        if(!practiceMode)
            Debug.Log($"Accuracy: {((obtained_score / total_score) * 100f).ToString("F2")}%");

        return allKeysPressed;
    }

    //<summary>
    // Updates cumulative elapsed time and calls notes processor at current time
    //<summary>
    void Update()
    {
        if (!isStarted) return;
        accumulatedTime += (Time.deltaTime * playbackSpeed);
        if (accumulatedTime >= timeStep)
        {
            UpdateActiveNotesAtKeys();
            isPlaying = CheckKeysPressed(); // will light up with Practice mode off bhi ab
            if (practiceMode)
            {
                if (isPlaying){
                    currentTime += accumulatedTime;
                    ProcessNotesAtCurrentTime();
                }
            }
            else
            {
                isPlaying = true; // but wont stop
                currentTime += accumulatedTime;
                ProcessNotesAtCurrentTime();
            }
            accumulatedTime = 0f;
        }
        if (isStarted && (currentTime > song_end_time))
        {
            Debug.Log("Song Ended :)");
            isStarted = false;
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

            if (currentTime >= noteReachTime && !done_and_dusted[i])
            {
                int noteNumber = noteNumbers[i];
                activeNotesTemp.Add(noteNumber);
                float endingTime = noteReachTime + ((endTimes[i] - startTimes[i]));
                if (endingTime > song_end_time)
                {
                    song_end_time = endingTime + 1f;
                }
                // Add the note to the activeNotes list only if it's not already in the list
                if (!existingNotes.Contains(noteNumber))
                {
                    float noteLength = endTimes[i] - startTimes[i];
                    activeNotes.Add((i, noteNumber, GetPressType(endTimes[i]-startTimes[i]), 0.0f, noteReachTime, endingTime)); // values for note
                    existingNotes.Add(noteNumber); // Update the HashSet to include the new note
                    // Debug.Log($"Added note {noteNumber} to List");
                }
            }
        }
    }

    int GetPressType(float note_length)
    {
        if (note_length >= 2.0f)
        {    
            Debug.Log($"Note length {note_length} is of type 2");
            return 2;
        }
        if (note_length >= 1.0f)
        {
            Debug.Log($"Note length {note_length} is of type 1");
            return 1;
        }
        Debug.Log($"Note length {note_length} is of type 0");
        return 0;
    }

    int CheckPressTime(float pressTime, int pressType)
    {
        Debug.Log($"Press Time was: {pressTime}");
        if(pressType == 0)
        {
            if (pressTime > tap_time[0] && pressTime < tap_time[1])
                return 0;
            if (pressTime < tap_time[0])
                return -1;
            if (pressTime > tap_time[1])
                return 1;
        }
        if (pressType == 1)
        {
            if (pressTime > hold_time[0] && pressTime < hold_time[1])
                return 0;
            if (pressTime < hold_time[0])
                return -1;
            if (pressTime > hold_time[1])
                return 1;
        }
        if (pressType == 2)
        {
            if (pressTime > long_hold_time[0] && pressTime < long_hold_time[1])
                return 0;
            if (pressTime < long_hold_time[0])
                return -1;
            if (pressTime > long_hold_time[1])
                return 1;
        }
        return 0;
    }

    void PrintActiveNotes(string origin)
    {
        // Join all noteNumbers into a single string, separated by commas
        string allNoteNumbers = string.Join(", ", activeNotes.Select(note => note.noteNumber.ToString()).ToArray());

        // Print the entire string in one line
        Debug.Log(origin + " Note Numbers: " + allNoteNumbers);
    }

    void OnDestroy()
    {
        string path = Application.persistentDataPath + "/vr_debug.log";
        string log = $"Accuracy: {((obtained_score / total_score) * 100f).ToString("F2")}%";
        File.AppendAllText(path, log); // This creates or appends the file
        // Debug.Log("Wrote to: " + path);
    }

}