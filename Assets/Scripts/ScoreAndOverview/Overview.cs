using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using UnityEngine.Networking;
using System.Linq;
using XCharts.Runtime;
using System.Globalization;


public class Overview : MonoBehaviour
{
    private string midiFolderPath;
    public TMPro.TextMeshProUGUI selectedSong;
    public GameObject selectionPanel;
    public GameObject buttonPrefab;
    public Transform content;

    private bool isSpawned = false;
    private const int buttonCount = 10;
    private const float buttonHeight = 100f;
    private const float buttonWidth = 300f;

    [SerializeField] XCharts.Runtime.PieChart pieChart;

    private Color32 HexToColor32(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
            return (Color32)color;
        else
            return new Color32(255, 255, 255, 255); // fallback white
    }
    
    private void OnEnable()
    {
        midiFolderPath = Application.streamingAssetsPath;
        SpawnButtons();
        DrawPieChart(DataManager.Instance.GetSongPlayDistribution());
    }

    private void SpawnButtons()
    {
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

        GameObject newButton1 = Instantiate(buttonPrefab, content);
        RectTransform rectTransform1 = newButton1.GetComponent<RectTransform>();
        rectTransform1.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        rectTransform1.anchoredPosition = new Vector2(0, (0 * buttonHeight) - 50);
        newButton1.GetComponentInChildren<TextMeshProUGUI>().text = "All Songs";
        newButton1.GetComponent<Button>().onClick.AddListener(() => SelectSong("All Songs"));

        for (int i = 1; i < midiFiles.Count + 1; i++)
        {
            string fileName = Path.GetFileNameWithoutExtension(midiFiles[i-1].Trim());

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
        selectedSong.text = fileName;
    }

    void DrawPieChart(Dictionary<string, float> distributionData)
    {
        if (pieChart == null)
        {
            pieChart = gameObject.AddComponent<XCharts.Runtime.PieChart>();
            pieChart.Init();
        }

        pieChart.ClearData();

        // 🟣 Custom Theme Colors
        pieChart.theme.enableCustomTheme = true;
        pieChart.theme.customColorPalette = new List<Color32> {
            HexToColor32("#00FFFF"), // Cyan
            HexToColor32("#FF69B4"), // HotPink
            HexToColor32("#FFD700"), // Gold
            HexToColor32("#32CD32"), // LimeGreen
            HexToColor32("#FF4500"), // OrangeRed
            HexToColor32("#9370DB")  // MediumPurple
        };


        // Title
        Title title = pieChart.EnsureChartComponent<Title>();
        title.text = "Song Distribution";
        title.labelStyle.textStyle.color = Color.yellow; // Match your theme
        title.labelStyle.textStyle.fontSize = 24;

        // Legend
        Legend legend = pieChart.EnsureChartComponent<Legend>();
        legend.labelStyle.textStyle.color = Color.yellow;
        legend.show = true;

        // Pie Data
        float totalCount = 0;
        foreach (var entry in distributionData)
            totalCount += entry.Value;

        foreach (var entry in distributionData)
            pieChart.AddData(0, entry.Value / totalCount, entry.Key);

        pieChart.RefreshChart();
    }
}
