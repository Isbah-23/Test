using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;
using UnityEngine.Networking;

[DefaultExecutionOrder(-100)]
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    
    private string _dataPath;
    private Dictionary<string, UserData> _gameData;
    private string username = "John Doe";

    [System.Serializable]
    public class PlaySession
    {
        public string songName;
        public float score;
        public string timestamp;
        public int wrongKeyPresses;
        public SerializableDictionary wrongKeys = new SerializableDictionary();
    }

    [System.Serializable]
    public class SongStatistics
    {
        public List<float> last10Scores = new List<float>();
        public int totalPlays;
        public int totalWrongPresses;
        public SerializableDictionary commonWrongNotes = new SerializableDictionary();
    }

    [System.Serializable]
    public class UserData
    {
        public Dictionary<string, string> scores = new Dictionary<string, string>();
        public Dictionary<string, string> info = new Dictionary<string, string>();
        public List<PlaySession> playHistory = new List<PlaySession>();
        public Dictionary<string, SongStatistics> songStats = new Dictionary<string, SongStatistics>();

        public SerializableUserData ToSerializable()
        {
            var serializableStats = new SerializableDictionary();
            foreach (var stat in songStats)
            {
                serializableStats.keys.Add(stat.Key);
                serializableStats.values.Add(JsonUtility.ToJson(stat.Value));
            }

            return new SerializableUserData
            {
                scores = new SerializableDictionary(this.scores),
                info = new SerializableDictionary(this.info),
                playHistory = this.playHistory,
                songStats = serializableStats
            };
        }
    }

    [System.Serializable]
    public class SerializableUserData
    {
        public SerializableDictionary scores;
        public SerializableDictionary info;
        public List<PlaySession> playHistory;
        public SerializableDictionary songStats;
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
                var userData = new UserData
                {
                    scores = userDataList[i].scores.ToDictionary(),
                    info = userDataList[i].info.ToDictionary(),
                    playHistory = userDataList[i].playHistory
                };

                foreach (var kvp in userDataList[i].songStats.ToDictionary())
                {
                    userData.songStats[kvp.Key] = JsonUtility.FromJson<SongStatistics>(kvp.Value);
                }

                dict[usernames[i]] = userData;
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
        StartCoroutine(VR_Initialize());
    }

    private IEnumerator VR_Initialize()
    {
        yield return StartCoroutine(VR_SafeLoadData());
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
        StartCoroutine(VR_SafeSaveData());
        Debug.Log($"Set score: {scoreKey} = {value}");
    }

    public void SetInfo(string infoKey, object value)
    {
        if (!_gameData.ContainsKey(username))
        {
            _gameData[username] = new UserData();
        }
        _gameData[username].info[infoKey] = value.ToString();
        StartCoroutine(VR_SafeSaveData());
    }

    private IEnumerator VR_SafeLoadData()
    {
        _gameData = new Dictionary<string, UserData>(); // Default init

        #if UNITY_ANDROID && !UNITY_EDITOR
        string fullPath = Application.persistentDataPath + "/game_data.json";
        #else
        string fullPath = Path.Combine(Application.persistentDataPath, "game_data.json");
        #endif

        if (!File.Exists(fullPath))
        {
            yield break;
        }

        // Use UnityWebRequest for cross-platform reliability
        using (UnityWebRequest request = UnityWebRequest.Get(fullPath))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load data: {request.error}");
                yield break;
            }

            try
            {
                string json = request.downloadHandler.text;
                GameDataWrapper wrapper = JsonUtility.FromJson<GameDataWrapper>(json);
                _gameData = wrapper?.ToDictionary() ?? new Dictionary<string, UserData>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Data parse failed: {e.Message}");
            }
        }
    }

    public IEnumerator RecordPlaySession(string songName, float score, Dictionary<string, int> wrongKeyPresses)
    {
        if (!_gameData.ContainsKey(username))
        {
            _gameData[username] = new UserData();
        }

        var user = _gameData[username];
        
        var session = new PlaySession
        {
            songName = songName,
            score = score,
            timestamp = DateTime.Now.ToString("o"),
            wrongKeyPresses = wrongKeyPresses.Values.Sum(),
            wrongKeys = new SerializableDictionary()
        };

        foreach (var kvp in wrongKeyPresses)
        {
            session.wrongKeys.keys.Add(kvp.Key);
            session.wrongKeys.values.Add(kvp.Value.ToString());
        }

        user.playHistory.Insert(0, session);
        if (user.playHistory.Count > 10)
        {
            user.playHistory.RemoveAt(10);
        }

        if (!user.songStats.ContainsKey(songName))
        {
            user.songStats[songName] = new SongStatistics();
        }

        var stats = user.songStats[songName];
        stats.last10Scores.Insert(0, score);
        if (stats.last10Scores.Count > 10)
        {
            stats.last10Scores.RemoveAt(10);
        }

        stats.totalPlays++;
        stats.totalWrongPresses += session.wrongKeyPresses;

        foreach (var kvp in wrongKeyPresses)
        {
            int index = stats.commonWrongNotes.keys.IndexOf(kvp.Key);
            if (index >= 0)
            {
                int currentValue = int.Parse(stats.commonWrongNotes.values[index]);
                stats.commonWrongNotes.values[index] = (currentValue + kvp.Value).ToString();
            }
            else
            {
                stats.commonWrongNotes.keys.Add(kvp.Key);
                stats.commonWrongNotes.values.Add(kvp.Value.ToString());
            }
        }

        yield return StartCoroutine(VR_SafeSaveData());
    }

    public List<PlaySession> GetPlayHistory(int maxEntries = 10)
    {
        if (_gameData.TryGetValue(username, out UserData userData))
        {
            return userData.playHistory.Take(maxEntries).ToList();
        }
        return new List<PlaySession>();
    }

    public SongStatistics GetSongStatistics(string songName)
    {
        if (_gameData.TryGetValue(username, out UserData userData) && 
            userData.songStats.TryGetValue(songName, out SongStatistics stats))
        {
            return stats;
        }
        return new SongStatistics();
    }

    private IEnumerator VR_SafeSaveData()
    {
        GameDataWrapper wrapper = new GameDataWrapper();
        wrapper.FromDictionary(_gameData);
        string json = JsonUtility.ToJson(wrapper, true);

        #if UNITY_ANDROID && !UNITY_EDITOR
        string fullPath = Application.persistentDataPath + "/game_data.json";
        #else
        string fullPath = Path.Combine(Application.persistentDataPath, "game_data.json");
        #endif

        // Atomic write: Save to temp file first
        string tempPath = fullPath + ".tmp";

        // Write using File class (UnityWebRequest doesn't support writing)
        try
        {
            File.WriteAllText(tempPath, json);
            
            // Replace original file
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            File.Move(tempPath, fullPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data: {e.Message}");
        }

        yield return null; // Ensure coroutine completes
    }

    public string GetPersistentDataPath()
    {
        return Application.persistentDataPath;
    }

    // =================================== GET INFO FOR STATS ==================================/
    public Dictionary<string, float> GetSongPlayDistribution()
    {
        var distribution = new Dictionary<string, float>();
        if (_gameData.TryGetValue(username, out UserData userData))
        {
            int totalPlays = userData.playHistory.Count;
            if (totalPlays > 0)
            {
                foreach (var session in userData.playHistory)
                {
                    if (distribution.ContainsKey(session.songName))
                        distribution[session.songName]++;
                    else
                        distribution[session.songName] = 1;
                }

                // Convert counts to percentages
                foreach (var key in distribution.Keys.ToList())
                {
                    distribution[key] = (distribution[key] / totalPlays) * 100f;
                }
            }
        }
        return distribution;
    }

    public (List<float> scores, List<string> dates) GetScoreProgression(string songName)
    {
        var scores = new List<float>();
        var dates = new List<string>();
        
        if (_gameData.TryGetValue(username, out UserData userData))
        {
            foreach (var session in userData.playHistory.Where(s => s.songName == songName))
            {
                scores.Add(session.score);
                dates.Add(DateTime.Parse(session.timestamp).ToString("MMM dd"));
            }
        }
        return (scores, dates);
    }

    // Get accuracy trend data
    public (List<float> accuracy, List<string> dates) GetAccuracyTrend(string songName)
    {
        var accuracy = new List<float>();
        var dates = new List<string>();
        
        if (_gameData.TryGetValue(username, out UserData userData) && 
            userData.songStats.TryGetValue(songName, out SongStatistics stats))
        {
            for (int i = 0; i < stats.last10Scores.Count; i++)
            {
                accuracy.Add(stats.last10Scores[i]);
                dates.Add(DateTime.Now.AddDays(-(stats.last10Scores.Count - i - 1)).ToString("MMM dd"));
            }
        }
        return (accuracy, dates);
    }

    // Get mistake heatmap data
    public Dictionary<string, int> GetMistakeHotspots(string songName)
    {
        if (_gameData.TryGetValue(username, out UserData userData) && 
            userData.songStats.TryGetValue(songName, out SongStatistics stats))
        {
            var hotspots = new Dictionary<string, int>();
            for (int i = 0; i < stats.commonWrongNotes.keys.Count; i++)
            {
                hotspots[stats.commonWrongNotes.keys[i]] = int.Parse(stats.commonWrongNotes.values[i]);
            }
            return hotspots.OrderByDescending(kvp => kvp.Value)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        return new Dictionary<string, int>();
    }

    // Get performance summary for a song
    public (float averageScore, float bestScore) GetSongPerformanceSummary(string songName)
    {
        if (_gameData.TryGetValue(username, out UserData userData) && 
            userData.songStats.TryGetValue(songName, out SongStatistics stats))
        {
            float average = stats.last10Scores.Count > 0 ? stats.last10Scores.Average() : 0;
            float best = stats.last10Scores.Count > 0 ? stats.last10Scores.Max() : 0;
            float accuracy = 100f - (stats.totalWrongPresses / (float)(stats.totalPlays * 100)) * 100f; // Approximation
            
            return (average, best);
        }
        return (0, 0);
    }
}