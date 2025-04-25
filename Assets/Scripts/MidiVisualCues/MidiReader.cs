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

    private float press_leniency = 0.05f;  // Adjust as needed
    private float release_leniency = 0.02f;
    private float overpress_leniency = 0.07f;
    List<(int noteNumber, bool started_playing, float endingTime, float leniencyTime)> activeNotes = new List<(int, bool, float, float)>();

    
    float total_score = 0;
    float obtained_score = 0;

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
                noteNumbers.Add(note.NoteNumber - 21);
                playedNotes.Add(false);
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

        // Debug.Log("Checking for keys pressed");
        foreach (var keyEntry in pianoKeysDict)
        {
            // Debug.Log("In foreach");
            int key = keyEntry.Key;
            bool isKeyExpected = expectedKeys.Contains(keyEntry.Key);
            bool isKeyPressed = keyEntry.Value.isPressed;

            // if (key >= 35 && key <= 45)
            // {
            //     Debug.Log($"Key: {key}, isKeyPressed: {isKeyPressed}");
            // }

            if (!isKeyExpected) // for keys that should not have been pressed
            {
                if (isKeyPressed)
                {
                    Debug.Log($"Unexpected Key pressed: {pianoNotesNames[key-1]}");
                    // LogToPrefs($"Unexpected Key pressed: {pianoNotesNames[key-1]}");
                    // string path = Application.persistentDataPath + "/vr_debug.log";
                    // string log = $"{currentTime}:: Unexpected Key pressed - {pianoNotesNames[key-1]}";
                    // File.AppendAllText(path, log); // This creates or appends the file
                    // If an unexpected key is pressed, fail and change color
                    keyEntry.Value.ChangeKeyColor(false);
                    allKeysPressed = false;
                    increment_score = false;
                }
            }
            else // For keys that shouldve been pressed
            {
                int index = activeNotes.FindIndex(n => n.noteNumber == keyEntry.Key);
                if (index != -1)
                {
                    var (noteNumber, startedPlaying, endingTime, leniencyTime) = activeNotes[index];
                    if (endingTime - currentTime <= release_leniency)
                    {
                        if (endingTime - currentTime <= -overpress_leniency) // ok now Allah Hafiz note sahab
                        {
                            // Debug.Log($"Note {noteNumber} endingTime expired, removing.");
                            activeNotes.RemoveAt(index);
                        }
                        // Debug.Log($"MAIN letting {noteNumber} go. Difference: {endingTime-currentTime} <= {release_leniency}");
                        continue; // return true; // shouldve been pressed but it oki, we nice, we let it go
                    }
                    if (isKeyPressed) // wrna once leniency goes to 0, we are stuck
                    {
                        startedPlaying = true;
                        activeNotes[index] = (noteNumber, startedPlaying, endingTime, leniencyTime);
                    }
                    else
                    {
                        increment_score = false;
                    }
                    if (!startedPlaying) // if the key was never touched/tapped
                    {
                        // Reduce leniency time
                        leniencyTime -= (Time.deltaTime * playbackSpeed); // be lenient if allowed
                        activeNotes[index] = (noteNumber, startedPlaying, endingTime, leniencyTime);
                        if (leniencyTime <= 0)
                        {
                            // Debug.Log("No leniency left, kill");
                            allKeysPressed = false;
                        }
                    }
                    else
                    {
                        if (isKeyPressed) // else check that the key should still be held
                        {
                            if (endingTime - currentTime <= -overpress_leniency) // ok now Allah Hafiz note sahab
                            {
                                // Debug.Log($"Note {noteNumber} endingTime expired, removing.");
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

        Debug.Log($"Accuracy: {((obtained_score / total_score) * 100f).ToString("F2")}%");
        PlayerPrefs.SetFloat("Accuracy", (obtained_score / total_score) * 100f);
        PlayerPrefs.Save();

        // Debug.Log("Everything in order");
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
                        spawnerScript.SpawnNote(noteDuration* (1/playbackSpeed) * n); // note falls 1 unit length per unit delta time
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
                float endingTime = noteReachTime + ((endTimes[i] - startTimes[i])); //ending time
                // Add the note to the activeNotes list only if it's not already in the list and its not feasible to let it go
                if (!existingNotes.Contains(noteNumber) && ((endingTime - currentTime) > release_leniency))
                {
                    float noteLength = endTimes[i] - startTimes[i];
                    float leniencyTime = press_leniency;
                    activeNotes.Add((noteNumber, false, endingTime, leniencyTime)); // Dummy values for started_playing, playTime, and leniencyTime
                    existingNotes.Add(noteNumber); // Update the HashSet to include the new note
                    Debug.Log($"Added note {noteNumber} to List");
                }
            }
        }
    }

    public static void LogToPrefs(string message)
    {
        string existingLog = PlayerPrefs.GetString("debugLog", "");
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        existingLog += $"[{timestamp}] {message}\n";

        PlayerPrefs.SetString("debugLog", existingLog);
        PlayerPrefs.Save();
    }

    void OnDestroy()
    {
        string path = Application.persistentDataPath + "/vr_debug.log";
        string log = $"Accuracy: {((obtained_score / total_score) * 100f).ToString("F2")}%";
        File.AppendAllText(path, log); // This creates or appends the file
        // Debug.Log("Wrote to: " + path);
    }

}