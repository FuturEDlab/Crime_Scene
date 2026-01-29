using System;
using UnityEngine;

[Serializable]
public class RecordingData
{
    public string speakerName;

    // What you show in the UI
    public string displayDateTime;      // ex: "Dec 1, 2025 11:35"

    // Used only for sorting (safe format)
    public string sortDateTimeString;   // ex: "2025-12-01 11:35"

    public AudioClip clip;
}