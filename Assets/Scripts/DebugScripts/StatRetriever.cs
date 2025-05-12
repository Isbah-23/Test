using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using XCharts.Runtime;

public class StatRetriever : MonoBehaviour
{
    [SerializeField] TextMeshPro songNameText;
    [SerializeField] TextMeshPro averageScore;
    [SerializeField] TextMeshPro bestScore;
    [SerializeField] XCharts.Runtime.PieChart pieChart; // Changed from PieChart to BaseChart for more flexibility
    [SerializeField] XCharts.Runtime.LineChart lineChart;
    [SerializeField] XCharts.Runtime.BarChart barChart;

    void Start()
    {
        ShowSummary("london_bridge");
        DrawPieChart(DataManager.Instance.GetSongPlayDistribution());
        DrawScoreProgression("london_bridge");
        DrawMistakeBars("london_bridge");
    }

    void ShowSummary(string songName)
    {
        songNameText.text = songName;
        var (average, best) = DataManager.Instance.GetSongPerformanceSummary(songName);
        
        averageScore.text = $"Avg: {average:F1}%";
        bestScore.text = $"Best: {best:F1}%";
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

    void DrawScoreProgression(string songName)
    {
        // Initialize chart if needed
        if (lineChart == null) 
        {
            lineChart = gameObject.AddComponent<LineChart>();
            lineChart.Init();
            
            // Basic setup
            lineChart.SetSize(580, 300);
            
            Title title = lineChart.EnsureChartComponent<Title>();
            title.text = "Score Progression";
            
            Tooltip tooltip = lineChart.EnsureChartComponent<Tooltip>();
            tooltip.show = true;
            
            // Configure axes
            XAxis xAxisComponent = lineChart.EnsureChartComponent<XAxis>();
            xAxisComponent.type = Axis.AxisType.Category;
            
            YAxis yAxisComponent = lineChart.EnsureChartComponent<YAxis>();
            yAxisComponent.type = Axis.AxisType.Value;
            yAxisComponent.minMaxType = Axis.AxisMinMaxType.Custom;
            yAxisComponent.min = 0;
            yAxisComponent.max = 100;
            
            // Add series
            lineChart.AddSerie<Line>("Score");
        }
        
        // Get data
        var (scores, dates) = DataManager.Instance.GetScoreProgression(songName);
        
        // Clear only data (keep series config)
        lineChart.ClearData();
        
        // Set X axis categories (official API method)
        for (int i = 0; i < dates.Count; i++)
        {
            lineChart.AddXAxisData(dates[i]);
        }
        
        // Add Y values
        foreach (float score in scores)
        {
            lineChart.AddData(0, score);
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