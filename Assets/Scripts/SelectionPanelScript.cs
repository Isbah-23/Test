using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

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
        foreach (Transform child in content){
            Destroy(child.gameObject);  // Clear previous buttons
            isSpawned = false;
        }

        string[] midiFiles = Directory.GetFiles(midiFolderPath, "*.midi"); // Use correct extension

        if (midiFiles.Length == 0)
        {
            Debug.LogWarning("No MIDI files found.");
            return;
        }

        for (int i = 0; i < midiFiles.Length; i++)
        {
            string fileName = Path.GetFileNameWithoutExtension(midiFiles[i]);

            GameObject newButton = Instantiate(buttonPrefab, content);
            RectTransform rectTransform = newButton.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            rectTransform.anchoredPosition = new Vector2(0, (-i * buttonHeight) - 50);
            newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = fileName;
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
}
