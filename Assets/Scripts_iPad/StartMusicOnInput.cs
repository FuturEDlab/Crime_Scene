using UnityEngine;

public class StartMusicOnInput : MonoBehaviour
{
    private AudioSource _audioSource;
    private bool _musicStarted;

    [Header("Player Settings")]
    public Transform playerTransform;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();

        if (_audioSource != null)
        {
            _audioSource.spatialBlend = 0f; // 2D for scene-wide ambient
            _audioSource.loop = true;
            _audioSource.Stop();
        }
    }

    void Update()
    {
        // Start music on any button press
        if (!_musicStarted && _audioSource != null)
        {
            if (Input.anyKeyDown ||
                Input.GetMouseButtonDown(0) ||
                Input.GetButtonDown("Fire1") ||
                Input.GetButtonDown("Jump"))
            {
                StartMusic();
            }
        }
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