using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using TMPro;

public class Score : MonoBehaviour
{
    private string midiFolderPath;
    [SerializeField] TextMeshProUGUI songNameHolder;
    [SerializeField] TextMeshProUGUI playScoreHolder;
    [SerializeField] TextMeshProUGUI practiceScoreHolder;

    private void OnEnable()
    {
        songNameHolder.text = "";
        playScoreHolder.text = "";
        practiceScoreHolder.text = "";
        midiFolderPath = Application.streamingAssetsPath;
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

        float highScore = 0;
        for (int i = 0; i < midiFiles.Count; i++)
        {
            string fileName = Path.GetFileNameWithoutExtension(midiFiles[i].Trim());
            if (string.IsNullOrEmpty(fileName)) continue; // Skip empty lines
            Debug.Log("FileName:"+fileName);
            songNameHolder.text += fileName.Length > 10 ? fileName.Substring(0, 10) + "\n" : fileName + "\n"; 
            highScore = DataManager.Instance.GetScore<float>($"{fileName}_high_score", 0f);
            playScoreHolder.text += $"{highScore:F2}%\n";
            highScore = DataManager.Instance.GetScore<float>($"{fileName}_Practice_high_score", 0f);
            practiceScoreHolder.text += $"{highScore:F0}pts\n";
        }
    }
}
