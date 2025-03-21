using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MidiFileSelector : MonoBehaviour
{
    public GameObject songButtonPrefab; // Prefab for song button
    public Transform songListContent; // Parent object for buttons
    //public MidiReader midiReader; // Reference to the MidiReader script
    private string midiFolderPath;

    void Start()
    {
        midiFolderPath = Application.streamingAssetsPath;
        Debug.Log($"MIDI Folder Path: {midiFolderPath}");
        PopulateSongList();
    }

    void PopulateSongList()
    {
        foreach (Transform child in songListContent)
            Destroy(child.gameObject);  // Clear previous entries

        string[] midiFiles = Directory.GetFiles(midiFolderPath, "*.midi");

        foreach (string file in midiFiles)
        {
            Debug.Log("Found file: " + file);
            GameObject button = Instantiate(songButtonPrefab, songListContent);
            // button.transform.SetParent(songListContent, false);
            string fileName = Path.GetFileName(file);
            button.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = fileName;
            button.GetComponent<Button>().onClick.AddListener(() => SelectSong(fileName));
        }
    }

    void SelectSong(string fileName)
    {
        //midiReader.midiFilePath = fileName;
        Debug.Log($"Selected MIDI: {fileName}");
    }
}