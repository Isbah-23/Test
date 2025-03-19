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

    private Dictionary<int, Transform> noteSpawners; // Maps note numbers to their 

    float n = 0.3f; // should match n in NoteFallingScript - for note length
    
    // for practice mode
    private bool practiceMode = true;
    public static bool isPlaying = true;
    float time_diff = 9.7f;
    private GameObject grandPiano;
    private Dictionary<int, PianoKey> pianoKeysDict = new Dictionary<int, PianoKey>(); // Dictionary to store key references
    private bool allKeysPressed;
    private bool isStarted = false;

    public void TogglePracticeMode()
    {
        practiceMode = !practiceMode;
        practiceModeButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = practiceMode ? "Practice Mode: On": "Practice Mode: Off";
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

    bool CheckKeysPressed(List<int> keyNumbersToCheck)
    {
        HashSet<int> keysToCheckSet = new HashSet<int>(keyNumbersToCheck);

        bool allKeysPressed = true;

        foreach (var keyEntry in pianoKeysDict)
        {
            bool isKeyInList = keysToCheckSet.Contains(keyEntry.Key);
            bool isKeyPressed = keyEntry.Value.isPressed;

            if (isKeyInList)
            {
                if (!isKeyPressed)
                {
                    allKeysPressed = false;
                }
                else
                    keyEntry.Value.ChangeKeyColor(true);
            }
            else
            {
                if (isKeyPressed)
                {
                    allKeysPressed = false;
                    keyEntry.Value.ChangeKeyColor(false);
                }
            }
        }

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
                isPlaying = CheckKeysPressed(UpdateActiveNotesAtKeys());
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

    List<int> UpdateActiveNotesAtKeys()
    {
        List<int> activeNotes = new List<int>();

        for (int i = 0; i < startTimes.Count; i++)
        {
            float noteReachTime = startTimes[i] + time_diff;
            float noteEndTime = noteReachTime + ((endTimes[i] - startTimes[i]));

            if (playedNotes[i] && currentTime >= noteReachTime && currentTime <= noteEndTime)
            {
                activeNotes.Add(noteNumbers[i]);
            }
        }

        // Debug.Log("Active notes at key level: " + string.Join(", ", activeNotes));
        return activeNotes;
    }
}