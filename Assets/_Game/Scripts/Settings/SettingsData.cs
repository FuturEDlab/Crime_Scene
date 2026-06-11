using System;
using UnityEngine;

[System.Serializable]
public class SettingsData
{
    public string movementType = "continuous";
    public float movementSpeed = 1.0f;
    public string timeOfDay = "day";
    public SoundVolume soundVolume = new SoundVolume();

    [System.Serializable]
    public class SoundVolume
    {
        public float background = 0.7f;
        public float narration = 0.8f;
        public float soundFX = 0.75f;
    }
}