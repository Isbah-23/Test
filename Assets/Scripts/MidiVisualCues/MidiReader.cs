using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;

public class MidiReader : MonoBehaviour
{
    public string midiFilePath; // Path to your MIDI file (relative to StreamingAssets)
    public float readFrequency = 10f; // How many times per second to read (e.g., 10 times per second)
    private MidiFile midiFile;
    private float currentTime = 0f; // Current simulated playback time (in seconds)
    private float interval; // Time interval between reads (1/readFrequency)

    private IEnumerable<Note> notes; // All notes in the MIDI file
    private TempoMap tempoMap; // Tempo information of the MIDI file

    void Start()
    {
        // Calculate interval for the desired frequency
        interval = 1f / readFrequency;

        // Load the MIDI file
        string fullPath = Application.streamingAssetsPath + "/" + midiFilePath;
        try
        {
            midiFile = MidiFile.Read(fullPath);
            Debug.Log($"Successfully loaded MIDI file: {fullPath}");

            // Get tempo map and notes
            tempoMap = midiFile.GetTempoMap();
            notes = midiFile.GetNotes();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading MIDI file: {ex.Message}");
        }
    }

    void Update()
    {
        // Simulate playback at the desired frequency
        currentTime += Time.deltaTime;

        // Check for notes playing at the current time
        if (midiFile != null)
        {
            ReadNotesAtTime(currentTime);
        }
    }

    void ReadNotesAtTime(float timeInSeconds)
    {
        // Convert seconds to MIDI ticks using the tempoMap
        var metricTime = new MetricTimeSpan(0, 0, Mathf.FloorToInt(timeInSeconds)); // Convert seconds to integer
        long midiTime = TimeConverter.ConvertFrom(metricTime, tempoMap); // Convert to MIDI ticks

        Debug.Log($"Current Time: {timeInSeconds:F2}s -> MIDI Ticks: {midiTime}");

        List<Note> playingNotes = new List<Note>();

        foreach (var note in notes)
        {
            // Debugging note information
            Debug.Log($"Note Start: {note.Time}, Note End: {note.EndTime}");

            // Compare ticks
            if (note.Time <= midiTime && note.EndTime > midiTime)
            {
                playingNotes.Add(note);
            }
        }

        if (playingNotes.Count > 0)
        {
            Debug.Log($"Time {timeInSeconds:F2}s: Notes Playing -> {string.Join(", ", playingNotes)}");
        }
        else
        {
            Debug.Log($"Time {timeInSeconds:F2}s: No Notes Playing");
        }
    }
}