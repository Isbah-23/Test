using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using TMPro;

public class SelectionPanelController : MonoBehaviour
{
    public GameObject selectionPanel;
    public GameObject buttonPrefab;
    public GameObject mainOptions;
    public TMPro.TextMeshProUGUI selectedSongText;
    public Transform content;
    private string midiFolderPath;

    private bool isSpawned = false;
    private const int buttonCount = 10;
    private const float buttonHeight = 100f;
    private const float buttonWidth = 300f;
    private bool flag=false;

    void Awake()
    {
        midiFolderPath = Application.streamingAssetsPath;
        Debug.Log($"MIDI Folder Path: {midiFolderPath}");
    }

    

    public void ToggleSelectionPanel()
    {
        selectionPanel.SetActive(true);
        if (selectionPanel.activeSelf && !isSpawned)
            SpawnButtons();
    }

    private void SpawnButtons()
{
    midiFolderPath = Application.streamingAssetsPath;
    Debug.Log($"MIDI Folder Path: {midiFolderPath}");

    // Clear previous buttons
    foreach (Transform child in content)
    {
        Destroy(child.gameObject);
        isSpawned = false;
    }

    StartCoroutine(LoadMidiFiles());
}

private IEnumerator LoadMidiFiles()
{
    List<string> midiFiles = new List<string>();

    string path = Application.streamingAssetsPath + "/midi_files.txt";

    #if UNITY_ANDROID
    UnityWebRequest request = UnityWebRequest.Get(path);
    yield return request.SendWebRequest();

    if (request.result != UnityWebRequest.Result.Success)
    {
        Debug.LogError("Failed to load MIDI file list: " + request.error);
        yield break;
    }

    string fileContent = request.downloadHandler.text;
    midiFiles.AddRange(fileContent.Split('\n'));
    #else
    // For PC VR
    midiFiles.AddRange(Directory.GetFiles(Application.streamingAssetsPath, "*.midi"));
    #endif

    if (midiFiles.Count == 0)
    {
        Debug.LogWarning("No MIDI files found.");
        yield break;
    }

    for (int i = 0; i < midiFiles.Count; i++)
    {
        string fileName = Path.GetFileNameWithoutExtension(midiFiles[i].Trim());

        if (string.IsNullOrEmpty(fileName)) continue; // Skip empty lines

        Debug.Log($"Creating button for: {fileName}");

        GameObject newButton = Instantiate(buttonPrefab, content);
        RectTransform rectTransform = newButton.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        rectTransform.anchoredPosition = new Vector2(0, (-i * buttonHeight) - 50);
        newButton.GetComponentInChildren<TextMeshProUGUI>().text = fileName;
        newButton.GetComponent<Button>().onClick.AddListener(() => SelectSong(fileName));
    }

    isSpawned = true;
}

    void SelectSong(string fileName)
    {
        //midiReader.midiFilePath = fileName;
        selectionPanel.SetActive(false);
        mainOptions.SetActive(true);
        selectedSongText.text = "Current Song: " + fileName;
        // Debug.Log($"Selected MIDI: {fileName}");
    }

    void start()
    {
        SpawnButtons();
    }
    void onEnable()
    {
        SpawnButtons();
    }
}
