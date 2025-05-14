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
using System;


public class Overview : MonoBehaviour
{
    private string midiFolderPath;
    List<string> allSongs = new List<string>();
    [SerializeField] TMPro.TextMeshProUGUI selectedSong;
    [SerializeField] GameObject selectionPanel;
    [SerializeField] GameObject buttonPrefab;
    [SerializeField] Transform content;

    private bool isSpawned = false;
    private const int buttonCount = 10;
    private const float buttonHeight = 100f;
    private const float buttonWidth = 300f;

    [SerializeField] XCharts.Runtime.PieChart pieChart;
    [SerializeField] XCharts.Runtime.LineChart lineChart;
    [SerializeField] XCharts.Runtime.BarChart barChart;

    [SerializeField] TMPro.TextMeshProUGUI logPath;
    [SerializeField] TMPro.TextMeshProUGUI bestScore;
    [SerializeField] TMPro.TextMeshProUGUI bestPracticeScore;
    [SerializeField] TMPro.TextMeshProUGUI averageScore;

    
    private Dictionary<string, Line> lines = new Dictionary<string, Line>();

    [Serializable]
    public class SongStatData
    {
        public string songName;
        public List<float> values;
        public List<DateTime> timestamps;
    }

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
        GetLogPath();
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
        allSongs.Clear();
        selectedSong = "All Songs";
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
            allSongs.Add(fileName);
            GameObject newButton = Instantiate(buttonPrefab, content);
            RectTransform rectTransform = newButton.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            rectTransform.anchoredPosition = new Vector2(0, (-i * buttonHeight) - 50);
            newButton.GetComponentInChildren<TextMeshProUGUI>().text = fileName;
            newButton.GetComponent<Button>().onClick.AddListener(() => SelectSong(fileName));
        }

        isSpawned = true;
        DrawSongProgressions(allSongs); // Do this after allSongs has been populated
        DrawMistakeHeatmap(allSongs);
        // DrawMistakeHeatmap("london_bridge");
        GetScores(allSongs);
    }

    void SelectSong(string fileName)
    {
        selectedSong.text = fileName;
        if (selectedSong.text == "All Songs")
        {
            DrawSongProgressions(allSongs);
            DrawMistakeHeatmap(allSongs);
            GetScores(allSongs);
        }
        else
        {
            DrawSongProgressions(new List<string> {fileName});
            DrawMistakeHeatmap(new List<string> {fileName});
            GetScores(new List<string> {fileName});
        }
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
            HexToColor32("#9370DB"),  // MediumPurple
            HexToColor32("#00FF7F"),  // SpringGreen (bright cyan-green)
            HexToColor32("#FF00FF"),  // Magenta (vivid pink-purple)
            HexToColor32("#7FFFD4"),  // Aquamarine (electric teal)
            HexToColor32("#FF1493"),  // DeepPink (intense neon pink)
            HexToColor32("#9400D3"),  // DarkViolet (deep purple)
            HexToColor32("#00BFFF"),  // DeepSkyBlue (bright azure)
            HexToColor32("#FF8C00"),  // DarkOrange (vibrant orange)
            HexToColor32("#E6E6FA")   // Lavender (soft futuristic purple)
        };


        // Title
        Title title = pieChart.EnsureChartComponent<Title>();
        title.text = "Song Distribution";
        title.labelStyle.textStyle.color = HexToColor32("#E6A32B"); // Match your theme
        title.labelStyle.textStyle.fontSize = 24;

        // Legend
        Legend legend = pieChart.EnsureChartComponent<Legend>();
        legend.labelStyle.textStyle.color = HexToColor32("#E6A32B");
        legend.show = true;

        // Pie Data
        float totalCount = 0;
        foreach (var entry in distributionData)
            totalCount += entry.Value;

        foreach (var entry in distributionData)
            pieChart.AddData(0, entry.Value / totalCount, entry.Key);

        pieChart.RefreshChart();
    }

    void DrawSongProgressions(List<string> songNames)
    {
        List<SongStatData> songStats = new List<SongStatData>();
        // Define custom color palette
        List<Color32> customColors = new List<Color32>
        {
            HexToColor32("#00FFFF"), // Cyan
            HexToColor32("#FF69B4"), // HotPink
            HexToColor32("#FFD700"), // Gold
            HexToColor32("#32CD32"), // LimeGreen
            HexToColor32("#FF4500"), // OrangeRed
            HexToColor32("#9370DB"),  // MediumPurple
            HexToColor32("#00FF7F"),  // SpringGreen (bright cyan-green)
            HexToColor32("#FF00FF"),  // Magenta (vivid pink-purple)
            HexToColor32("#7FFFD4"),  // Aquamarine (electric teal)
            HexToColor32("#FF1493"),  // DeepPink (intense neon pink)
            HexToColor32("#9400D3"),  // DarkViolet (deep purple)
            HexToColor32("#00BFFF"),  // DeepSkyBlue (bright azure)
            HexToColor32("#FF8C00"),  // DarkOrange (vibrant orange)
            HexToColor32("#E6E6FA")   // Lavender (soft futuristic purple)
        };

        // First collect all data and find all unique timestamps
        var allData = new Dictionary<string, (List<float> scores, List<DateTime> timestamps)>();
        var allTimestamps = new HashSet<DateTime>();

        foreach (var name in songNames)
        {
            var (scores, dateStrings) = DataManager.Instance.GetScoreProgression(name);
            var parsedDates = new List<DateTime>();

            foreach (var dateStr in dateStrings)
            {
                if (DateTime.TryParseExact(dateStr, "MMM dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    parsedDates.Add(parsedDate);
                    allTimestamps.Add(parsedDate);
                }
            }
            string subName = name.Length > 5 ? name.Substring(0, 5) : name;
            allData[subName] = (scores, parsedDates);
            songStats.Add(new SongStatData { songName = subName, values = scores, timestamps = parsedDates });
        }

        if (lineChart == null) return;

        lineChart.ClearData();

        // Title
        Title title = lineChart.EnsureChartComponent<Title>();
        title.text = "Score Progression";
        title.labelStyle.textStyle.color = HexToColor32("#E6A32B"); // Match your theme
        title.labelStyle.textStyle.fontSize = 24;

        // Lengend
        Legend legend = lineChart.EnsureChartComponent<Legend>();
        legend.labelStyle.textStyle.color = HexToColor32("#E6A32B");
        if (songNames.Count > 1)
            legend.show = true;
        else
            legend.show = false;

        // Sort all timestamps
        var sortedTimestamps = allTimestamps.OrderBy(t => t).ToList();

        // Configure X-axis with all timestamps
        var xAxis = lineChart.GetChartComponent<XAxis>();
        xAxis.data.Clear();
        xAxis.type = Axis.AxisType.Category;
        xAxis.boundaryGap = true;

        foreach (var timestamp in sortedTimestamps)
        {
            xAxis.data.Add(timestamp.ToString("MMM dd HH:mm"));
        }

        // Add series for each song
        int colorIndex = 0;
        foreach (var song in allData)
        {
            Line serie = lineChart.AddSerie<Line>(song.Key);
            serie.symbol.type = SymbolType.Circle;
            serie.symbol.size = 10;
            serie.lineStyle.width = 2;
            serie.ignore = true;
            serie.ignoreValue = 0;

            // Set color from palette
            if (colorIndex < customColors.Count)
            {
                serie.lineStyle.color = customColors[colorIndex];
                serie.itemStyle.color = customColors[colorIndex];
            }
            else
            {
                serie.lineStyle.color = Color.gray;
                serie.itemStyle.color = Color.gray;
            }
            colorIndex++;


            // Align data with master timestamp list
            for (int i = 0; i < sortedTimestamps.Count; i++)
            {
                int dataIndex = song.Value.timestamps.IndexOf(sortedTimestamps[i]);
                if (dataIndex >= 0 && dataIndex < song.Value.scores.Count)
                {
                    float value = song.Value.scores[dataIndex];
                    serie.AddData(value);
                }
                else
                {
                    serie.AddData(0);
                }
            }

        }
    }

    void DrawMistakeHeatmap(List<string> songNames)
    {
        // Initialize chart if needed (same as working version)
        if (barChart == null)
        {
            barChart = gameObject.AddComponent<BarChart>();
            barChart.Init();
            barChart.SetSize(800, 400);
            
            XAxis xAxis = barChart.EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Category;
            xAxis.boundaryGap = true;
            
            YAxis yAxis = barChart.EnsureChartComponent<YAxis>();
            yAxis.type = Axis.AxisType.Value;
        }

        // Clear existing data
        barChart.ClearData();
        barChart.GetChartComponent<XAxis>().data.Clear();

        // Title
        Title title = barChart.EnsureChartComponent<Title>();
        title.text = "Mistake Hotspots";
        title.labelStyle.textStyle.color = HexToColor32("#E6A32B"); // Match your theme
        title.labelStyle.textStyle.fontSize = 24;

        // Lengend
        Legend legend = barChart.EnsureChartComponent<Legend>();
        legend.labelStyle.textStyle.color = HexToColor32("#E6A32B");
        legend.show = songNames.Count > 1;

        // Gather all data and piano keys
        Dictionary<string, Dictionary<string, int>> allSongData = new Dictionary<string, Dictionary<string, int>>();
        HashSet<string> allPianoKeys = new HashSet<string>();

        foreach (string song in songNames)
        {
            var mistakes = DataManager.Instance.GetMistakeHotspots(song);
            string subName = name.Length > 4 ? name.Substring(0, 4) : name;
            allSongData[song] = mistakes;
            
            foreach (string key in mistakes.Keys)
            {
                allPianoKeys.Add(key);
            }
        }

        // Sort piano keys by total mistakes across all songs
        var sortedKeys = allPianoKeys.OrderByDescending(key => 
            allSongData.Sum(song => song.Value.ContainsKey(key) ? song.Value[key] : 0)
        ).ToList();

        // Add piano keys to X-axis
        foreach (string key in sortedKeys)
        {
            barChart.AddXAxisData(key);
        }

        // Color palette
        List<Color32> colors = new List<Color32>
        {
            HexToColor32("#00FFFF"), HexToColor32("#FF69B4"), HexToColor32("#FFD700"),
            HexToColor32("#32CD32"), HexToColor32("#FF4500"), HexToColor32("#9370DB")
        };

        // Add one series per song
        for (int i = 0; i < songNames.Count; i++)
        {
            string song = songNames[i];
            string displayName = song.Length > 4 ? song.Substring(0, 4) : song;
            
            Bar serie = barChart.AddSerie<Bar>(displayName);
            serie.stack = "stack1";
            serie.barWidth = 0.6f;
            serie.itemStyle.color = colors[i % colors.Count]; // Cycle through colors
            
            // Add data points for this song
            foreach (string key in sortedKeys)
            {
                int value = allSongData[song].ContainsKey(key) ? allSongData[song][key] : 0;
                barChart.AddData(i, value); // Add to current series
            }
        }

        barChart.RefreshChart();
    }

    void GetLogPath()
    {
        logPath.text = "Log Path:" + DataManager.Instance.GetPersistentDataPath();
    }

    void GetScores(List<string> songNames)
    {
        float highScore = 0;
        float practiceHighScore = 0;
        float average = 0;

        foreach (string songName in songNames)
        {
            float current = 0;
            current = DataManager.Instance.GetScore<float>($"{songName}_high_score", 0f);
            if (current > highScore)
                highScore = current;
            current = DataManager.Instance.GetScore<float>($"{songName}_Practice_high_score", 0f);
            if (current > practiceHighScore)
                practiceHighScore = current;
            average += DataManager.Instance.GetSongPerformanceSummary(songName);
        }
        average = average / songNames.Count();
        bestScore.text = $"{highScore:F2}%";
        bestPracticeScore.text = $"{practiceHighScore:F0}pts";
        averageScore.text = $"{average:F2}%";
    }
}
