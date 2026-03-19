using System;
using UnityEngine;

namespace BNG_Framework.Scripts.iPad
{
    [Serializable]
    public class AudioRecordingData
    {
        [Tooltip("Name of the person giving the statement")]
        public string speakerName;

        [Tooltip("Text shown to the player, e.g. 'Nov 25, 2025 14:30'")]
        public string displayDateTime;

        [Tooltip("For sorting, e.g. '2025-11-25 14:30'")]
        public string sortDateTimeString;

        [Tooltip("Audio clip for this statement")]
        public AudioClip clip;

        [NonSerialized] public DateTime sortDateTime;
    }
}
