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
    public static bool isPlaying = true;
    float time_diff = 2.93f;
    private GameObject grandPiano;
    private Dictionary<int, PianoKey> pianoKeysDict = new Dictionary<int, PianoKey>(); // Dictionary to store key references
    private bool allKeysPressed;
    private bool isStarted = false;

    private float press_leniency = 0.01f;  // Adjust as needed
    private float release_leniency = 0.02f;
    List<(int noteNumber, bool started_playing, float endingTime, float leniencyTime)> activeNotes = new List<(int, bool, float, float)>();

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
    }

    //<summary>
    // Initializes the arrays with note information and note spawners
    //<summary>    
    public void StartPlaying()
    {
        string prefix = "Current Song: ";
        string textValue = selectedSongText.text;

        // Extract the song name
        string songName = textValue.Substring(prefix.Length).Trim();

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

            if (isKeyExpected) // For keys that shouldve been pressed
            {
                int index = activeNotes.FindIndex(n => n.noteNumber == keyEntry.Key);
                if (index != -1)
                {
                    var (noteNumber, startedPlaying, endingTime, leniencyTime) = activeNotes[index];
                    if (endingTime - currentTime <= release_leniency)
                    {
                        if (endingTime - currentTime <= 0) // ok now Allah Hafiz note sahab
                        {
                            Debug.Log($"Note {noteNumber} endingTime expired, removing.");
                            activeNotes.RemoveAt(index);
                        }
                        Debug.Log($"MAIN letting {noteNumber} go. Difference: {endingTime-currentTime} <= {release_leniency}");
                        return true; // shouldve been pressed but it oki, we nice, we let it go
                    }

                    if (isKeyPressed) // wrna once leniency goes to 0, we are stuck
                    {
                        startedPlaying = true;
                        activeNotes[index] = (noteNumber, startedPlaying, endingTime, leniencyTime);
                    }
                    if (!startedPlaying) // if the key was never touched/tapped
                    {
                        // Reduce leniency time
                        leniencyTime -= (Time.deltaTime * playbackSpeed); // be lenient if allowed
                        activeNotes[index] = (noteNumber, startedPlaying, endingTime, leniencyTime);
                        if (leniencyTime <= 0)
                        {
                            // Debug.Log("No leniency left, kill");
                            return false;
                        }
                    }
                    else
                    {
                        if (isKeyPressed) // else check that the key should still be held
                        {
                            if (endingTime - currentTime <= 0) // ok now Allah Hafiz note sahab
                            {
                                Debug.Log($"Note {noteNumber} endingTime expired, removing.");
                                activeNotes.RemoveAt(index);
                            }
                            keyEntry.Value.ChangeKeyColor(true);
                        }
                        else
                        {
                            if (endingTime - currentTime <= release_leniency)
                            {
                                if (endingTime - currentTime <= 0) // ok now Allah Hafiz note sahab
                                {
                                    Debug.Log($"Note {noteNumber} endingTime expired, removing.");
                                    activeNotes.RemoveAt(index);
                                }
                                Debug.Log($"We letting {noteNumber} go.");
                                return true; // shouldve been pressed but it oki, we nice, we let it go
                            }
                            return false; // Expected key is not pressed
                        }
                    }
                }
            }
            else // for keys that should not have been pressed
            {
                if (isKeyPressed)
                {
                    // Debug.Log("Unexpected Key pressed");
                    // If an unexpected key is pressed, fail and change color
                    keyEntry.Value.ChangeKeyColor(false);
                    return false;
                }
            }
        }

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
}