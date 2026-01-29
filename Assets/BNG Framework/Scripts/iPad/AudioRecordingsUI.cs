using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioRecordingsUI : MonoBehaviour
{
    [Header("Data (sorted ascending by date)")]
    public List<RecordingData> recordings = new List<RecordingData>();

    [Header("UI References")]
    public Transform listContainer;   // assign: RecordingsScrollView > Viewport > Content
    public GameObject itemPrefab;     // assign: RecordingItem prefab (must contain RecordingListItem)

    [Header("Audio")]
    public AudioSource sharedAudioSource; // assign: AudioSource on AudioRecordingsPanel (or same GO)

    private RecordingListItem currentlyPlayingItem;

    void Start()
    {
        if (listContainer == null || itemPrefab == null || sharedAudioSource == null)
        {
            Debug.LogError("AudioRecordingsUI: Missing references (listContainer / itemPrefab / sharedAudioSource).");
            return;
        }

        // Sort ascending
        recordings.Sort((a, b) =>
        {
            DateTime da = ParseDate(a != null ? a.sortDateTimeString : "");
            DateTime db = ParseDate(b != null ? b.sortDateTimeString : "");
            return da.CompareTo(db);
        });

        // Clear old children (optional safety)
        for (int i = listContainer.childCount - 1; i >= 0; i--)
            Destroy(listContainer.GetChild(i).gameObject);

        // Build UI items
        foreach (var data in recordings)
        {
            if (data == null || data.clip == null)
            {
                Debug.LogWarning("AudioRecordingsUI: Skipping a recording because data/clip is missing.");
                continue;
            }

            GameObject obj = Instantiate(itemPrefab, listContainer);

            var item = obj.GetComponent<RecordingListItem>();
            if (item == null)
            {
                Debug.LogError("AudioRecordingsUI: itemPrefab is missing RecordingListItem component.");
                continue;
            }

            // Pass callbacks so one item stops others
            item.Setup(data, sharedAudioSource, OnItemWantsToPlay);
        }
    }

    private void OnItemWantsToPlay(RecordingListItem item)
    {
        // if another item was playing, force it to reset UI
        if (currentlyPlayingItem != null && currentlyPlayingItem != item)
        {
            currentlyPlayingItem.ForceStopUIOnly();
        }

        currentlyPlayingItem = item;
    }

    private DateTime ParseDate(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return DateTime.MinValue;

        if (DateTime.TryParse(s, out var dt))
            return dt;

        return DateTime.MinValue;
    }
}
