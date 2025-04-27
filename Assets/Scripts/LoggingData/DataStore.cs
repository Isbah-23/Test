using UnityEngine;
using System.IO;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)] // Ensures early initialization
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    
    private string _dataPath;
    private Dictionary<string, object> _gameData;

    // Wrapper class for serialization
    [System.Serializable]
    private class GameDataWrapper
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();
    }

    private void Awake()
    {
        // Singleton pattern
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
        _dataPath = Path.Combine(Application.persistentDataPath, "game_data.json");
        LoadData();
        Debug.Log($"DataManager initialized. Path: {_dataPath}");
    }

    public T Get<T>(string key, T defaultValue = default)
    {
        if (_gameData.TryGetValue(key, out object value))
        {
            try
            {
                if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(value.ToString());
                if (typeof(T) == typeof(float))
                    return (T)(object)float.Parse(value.ToString());
                if (typeof(T) == typeof(bool))
                    return (T)(object)bool.Parse(value.ToString());
                
                return (T)value;
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public void Set(string key, object value)
    {
        _gameData[key] = value;
        SaveData();
    }

    private void LoadData()
    {
        if (File.Exists(_dataPath))
        {
            string json = File.ReadAllText(_dataPath);
            GameDataWrapper wrapper = JsonUtility.FromJson<GameDataWrapper>(json);
            
            _gameData = new Dictionary<string, object>();
            for (int i = 0; i < wrapper.keys.Count; i++)
            {
                _gameData[wrapper.keys[i]] = wrapper.values[i];
            }
        }
        else
        {
            _gameData = new Dictionary<string, object>();
        }
    }

    private void SaveData()
    {
        GameDataWrapper wrapper = new GameDataWrapper();
        
        foreach (var pair in _gameData)
        {
            wrapper.keys.Add(pair.Key);
            wrapper.values.Add(pair.Value.ToString());
        }
        
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(_dataPath, json);
    }

    public string GetPersistentDataPath()
    {
        return Application.persistentDataPath;
    }
}