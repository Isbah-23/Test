using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text;

[DefaultExecutionOrder(-100)]
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    
    private string _dataPath;
    private Dictionary<string, UserData> _gameData;
    private string username = "John Doe";

    [System.Serializable]
    public class UserData
    {
        public Dictionary<string, string> scores = new Dictionary<string, string>();
        public Dictionary<string, string> info = new Dictionary<string, string>();

        // Helper method for serialization
        public SerializableUserData ToSerializable()
        {
            return new SerializableUserData
            {
                scores = new SerializableDictionary(this.scores),
                info = new SerializableDictionary(this.info)
            };
        }
    }

    [System.Serializable]
    public class SerializableUserData
    {
        public SerializableDictionary scores;
        public SerializableDictionary info;
    }

    [System.Serializable]
    public class SerializableDictionary
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();

        public SerializableDictionary() { }
        
        public SerializableDictionary(Dictionary<string, string> dict)
        {
            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            for (int i = 0; i < keys.Count; i++)
            {
                dict[keys[i]] = values[i];
            }
            return dict;
        }
    }

    [System.Serializable]
    private class GameDataWrapper
    {
        public List<string> usernames = new List<string>();
        public List<SerializableUserData> userDataList = new List<SerializableUserData>();

        public Dictionary<string, UserData> ToDictionary()
        {
            var dict = new Dictionary<string, UserData>();
            for (int i = 0; i < usernames.Count; i++)
            {
                dict[usernames[i]] = new UserData
                {
                    scores = userDataList[i].scores.ToDictionary(),
                    info = userDataList[i].info.ToDictionary()
                };
            }
            return dict;
        }

        public void FromDictionary(Dictionary<string, UserData> source)
        {
            usernames.Clear();
            userDataList.Clear();
            foreach (var kvp in source)
            {
                usernames.Add(kvp.Key);
                userDataList.Add(kvp.Value.ToSerializable());
            }
        }
    }

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
        _dataPath = Path.Combine(Application.persistentDataPath, "game_data.json");
        LoadData();
        Debug.Log(GetFormattedGameData());
    }

    public void SetUserName(string name)
    {
        username = name;
    }

    private string GetFormattedGameData()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Game Data ===");
        sb.AppendLine($"Data Path: {_dataPath}");
        sb.AppendLine();

        foreach (var user in _gameData)
        {
            sb.AppendLine($"User: {user.Key}");
            sb.AppendLine("--- Scores ---");
            foreach (var score in user.Value.scores)
            {
                sb.AppendLine($"{score.Key}: {score.Value}");
            }
            
            sb.AppendLine("--- Info ---");
            foreach (var info in user.Value.info)
            {
                sb.AppendLine($"{info.Key}: {info.Value}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public T GetScore<T>(string scoreKey, T defaultValue = default)
    {
        if (_gameData.TryGetValue(username, out UserData userData) && 
            userData.scores.TryGetValue(scoreKey, out string value))
        {
            return ParseValue<T>(value, defaultValue);
        }
        return defaultValue;
    }

    public T GetInfo<T>(string infoKey, T defaultValue = default)
    {
        if (_gameData.TryGetValue(username, out UserData userData) && 
            userData.info.TryGetValue(infoKey, out string value))
        {
            return ParseValue<T>(value, defaultValue);
        }
        return defaultValue;
    }

    private T ParseValue<T>(string value, T defaultValue)
    {
        try
        {
            if (typeof(T) == typeof(int)) return (T)(object)int.Parse(value);
            if (typeof(T) == typeof(float)) return (T)(object)float.Parse(value);
            if (typeof(T) == typeof(bool)) return (T)(object)bool.Parse(value);
            return (T)(object)value;
        }
        catch
        {
            return defaultValue;
        }
    }

    public void SetScore(string scoreKey, object value)
    {
        if (!_gameData.ContainsKey(username))
        {
            _gameData[username] = new UserData();
        }
        _gameData[username].scores[scoreKey] = value.ToString();
        SaveData();
        Debug.Log($"Set score: {scoreKey} = {value}");
    }

    public void SetInfo(string infoKey, object value)
    {
        if (!_gameData.ContainsKey(username))
        {
            _gameData[username] = new UserData();
        }
        _gameData[username].info[infoKey] = value.ToString();
        SaveData();
    }

    private void LoadData()
    {
        if (File.Exists(_dataPath))
        {
            string json = File.ReadAllText(_dataPath);
            GameDataWrapper wrapper = JsonUtility.FromJson<GameDataWrapper>(json);
            _gameData = wrapper?.ToDictionary() ?? new Dictionary<string, UserData>();
        }
        else
        {
            _gameData = new Dictionary<string, UserData>();
        }
    }

    private void SaveData()
    {
        GameDataWrapper wrapper = new GameDataWrapper();
        wrapper.FromDictionary(_gameData);
        
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(_dataPath, json);
        Debug.Log($"Saved data to {_dataPath}:\n{json}");
    }

    public string GetPersistentDataPath()
    {
        return Application.persistentDataPath;
    }
}