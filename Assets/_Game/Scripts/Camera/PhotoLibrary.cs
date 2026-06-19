using System;
using System.Collections.Generic;
using UnityEngine;

// In-memory store for every photo the player takes during the current session.
//
// Photos are deliberately kept in memory only and the list is cleared on Awake,
// so a new session never shows photos taken in a previous one.
public class PhotoLibrary : MonoBehaviour
{
    public static PhotoLibrary Instance { get; private set; }

    private readonly List<EvidencePhoto> _photos = new();

    // Raised whenever a photo is added or the library is cleared, so UI can refresh.
    public event Action OnLibraryChanged;

    public IReadOnlyList<EvidencePhoto> Photos => _photos;
    public int Count => _photos.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // New session => no leftover photos from any previous run.
        ClearInternal(notify: false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddPhoto(EvidencePhoto photo)
    {
        if (photo == null)
            return;

        _photos.Add(photo);
        OnLibraryChanged?.Invoke();
    }

    public EvidencePhoto GetPhoto(int index)
    {
        if (index < 0 || index >= _photos.Count)
            return null;
        return _photos[index];
    }

    public void Clear()
    {
        ClearInternal(notify: true);
    }

    private void ClearInternal(bool notify)
    {
        // Release the GPU textures we created so they don't leak.
        foreach (EvidencePhoto photo in _photos)
        {
            if (photo != null && photo.Image != null)
                Destroy(photo.Image);
        }
        _photos.Clear();

        if (notify)
            OnLibraryChanged?.Invoke();
    }
}
