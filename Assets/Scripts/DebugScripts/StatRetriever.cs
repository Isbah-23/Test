using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using System.Linq;
using XCharts.Runtime;
using System.Globalization;

public class StatRetriever : MonoBehaviour
{
    [SerializeField] TextMeshPro songNameText;
    [SerializeField] TextMeshPro averageScore;
    [SerializeField] TextMeshPro bestScore;
    [SerializeField] XCharts.Runtime.PieChart pieChart; // Changed from PieChart to BaseChart for more flexibility
    [SerializeField] XCharts.Runtime.LineChart lineChart;
    [SerializeField] XCharts.Runtime.BarChart barChart;
    public List<SongStatData> songStats;
    private Dictionary<string, Line> lines = new Dictionary<string, Line>();

    [Serializable]
    public class SongStatData
    {
        public string songName;
        public List<float> values;
        public List<DateTime> timestamps;
    }

    void Start()
    {
        ShowSummary("london_bridge");
        DrawPieChart(DataManager.Instance.GetSongPlayDistribution());
        DrawSongProgressions(new List<string> {"london_bridge","happy_birthday","twinkle_twinkle","test"});
        DrawMistakeBars("london_bridge");
    }

    void ShowSummary(string songName)
    {
        songNameText.text = songName;
        // var (average, best) = DataManager.Instance.GetSongPerformanceSummary(songName);
        
        // averageScore.text = $"Avg: {average:F1}%";
        // bestScore.text = $"Best: {best:F1}%";
    }
    
    void DrawPieChart(Dictionary<string, float> distributionData)
    {
        if (pieChart == null)
        {
            pieChart = gameObject.AddComponent<XCharts.Runtime.PieChart>();
            pieChart.Init();
        }
        
        pieChart.ClearData();
        float totalCount = 0;
        foreach (var entry in distributionData)
        {
            totalCount += entry.Value;
        }
        foreach (var entry in distributionData)
        {
            pieChart.AddData(0, entry.Value/totalCount, entry.Key);
        }

        pieChart.RefreshChart();
    }

    void DrawSongProgressions(List<string> songNames)
{
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

        allData[name] = (scores, parsedDates);
        songStats.Add(new SongStatData { songName = name, values = scores, timestamps = parsedDates });
    }

    if (lineChart == null) return;

    lineChart.ClearData();
    
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
    foreach (var song in allData)
    {
        Line serie = lineChart.AddSerie<Line>(song.Key);
        serie.symbol.type = SymbolType.Circle;
        serie.symbol.size = 10;
        serie.lineStyle.width = 2;
        serie.ignore = true; 
        serie.ignoreValue = 0;
        
        // Align data with master timestamp list
        for (int i = 0; i < sortedTimestamps.Count; i++)
        {
            int dataIndex = song.Value.timestamps.IndexOf(sortedTimestamps[i]);
            if (dataIndex >= 0 && dataIndex < song.Value.scores.Count)
            {
                serie.AddData(song.Value.scores[dataIndex]);
            }
            else
            {
                serie.AddData(0); // No data at this timestamp
            }
        }
    }

    lineChart.RefreshChart();
}

    void DrawMistakeBars(string songName)
    {
        // Initialize chart if needed
        if (barChart == null)
        {
            barChart = gameObject.AddComponent<BarChart>();
            barChart.Init();
            
            // Basic setup
            barChart.SetSize(800, 400);
            
            // Title setup
            Title title = barChart.EnsureChartComponent<Title>();
            title.text = "Common Mistakes";
            
            // Configure axes
            XAxis xAxis = barChart.EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Category;
            xAxis.boundaryGap = true;
            xAxis.data.Clear(); // Clear initial data
            
            YAxis yAxis = barChart.EnsureChartComponent<YAxis>();
            yAxis.type = Axis.AxisType.Value;
            
            // Add series
            Bar serie = barChart.AddSerie<Bar>("Mistakes");
            // serie.connectNulls = false;
            serie.barWidth = 0.6f;
            serie.itemStyle.color = new Color32(255, 100, 100, 255);
            
            // Configure label (correct API)
            serie.label.show = true;
            serie.label.position = LabelStyle.Position.Top; // Correct enum path
            serie.label.formatter = "{c}";
            serie.label.textStyle.color = Color.black;
            
            // Disable interactivity
            barChart.EnsureChartComponent<Tooltip>().show = false;
        }
        
        // Get data
        Dictionary<string, int> mistakeData = DataManager.Instance.GetMistakeHotspots(songName);
        
        // Clear existing data
        barChart.ClearData();
        barChart.GetChartComponent<XAxis>().data.Clear(); // Correct clearing method
        
        // Add sorted data
        foreach (var entry in mistakeData.OrderByDescending(x => x.Value))
        {
            barChart.AddXAxisData(entry.Key);
            barChart.AddData(0, entry.Value);
        }
        
        barChart.RefreshChart();
    }
}