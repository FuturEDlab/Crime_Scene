using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class StartMusicOnInput : MonoBehaviour
{
    private AudioSource _audioSource;
    private bool _musicStarted;
    private IDisposable _anyButtonListener;

    [Header("Player Settings")]
    public Transform playerTransform;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        if (_audioSource != null)
        {
            _audioSource.spatialBlend = 0f;
            _audioSource.loop = true;
            _audioSource.Stop();
        }
    }

    // Any button on any device (keyboard, mouse, gamepad, Quest controllers)
    // starts the music once. Input System replacement for Input.anyKeyDown.
    void OnEnable()
    {
        _anyButtonListener = InputSystem.onAnyButtonPress.CallOnce(_ => StartMusic());
    }

    void OnDisable()
    {
        _anyButtonListener?.Dispose();
        _anyButtonListener = null;
    }

    public void StartMusic()
    {
        if (!_musicStarted && _audioSource != null)
        {
            _audioSource.Play();
            _musicStarted = true;
        }
    }

    public void StopMusic()
    {
        if (_musicStarted && _audioSource != null)
        {
            _audioSource.Stop();
            _musicStarted = false;
        }
    }

    public void PauseMusic()
    {
        if (_musicStarted && _audioSource != null)
            _audioSource.Pause();
    }

    public void ResumeMusic()
    {
        if (_musicStarted && _audioSource != null)
            _audioSource.UnPause();
    }
}