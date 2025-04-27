using UnityEngine;
using System.IO;
using System;

[DefaultExecutionOrder(-99)] // Initializes right after DataManager
public class Logger : MonoBehaviour
{
    public static Logger Instance { get; private set; }
    private string _logFilePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        string logDir = Path.Combine(Application.persistentDataPath, "Logs");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        Log("Logger Initialized Succesfully");
    }

    public void Log(string message)
    {
        string entry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.Log(entry);
        File.AppendAllText(_logFilePath, entry + "\n");
    }

    public string GetPersistantDataPath() // print anywhere in game options/stats/etc to know in application bhi if u wanna know
    {
        return $"{Application.persistentDataPath}";
    }
}