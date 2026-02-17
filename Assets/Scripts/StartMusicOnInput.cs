using UnityEngine;

public class StartMusicOnInput : MonoBehaviour
{
    private AudioSource audioSource;
    private bool musicStarted = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!musicStarted && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            audioSource.Play();
            musicStarted = true;
        }
    }
}