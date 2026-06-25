using System;
using System.Collections.Generic;
using UnityEngine;

// In-memory store for every photo the player takes during the current session.
//
// Photos are kept in memory only and survive scene loads (e.g. walking outside
// then back inside) via DontDestroyOnLoad. The list is cleared exactly ONCE per
// session - when the first instance is created - so a new playthrough never
// shows photos from a previous one, but moving between scenes keeps them.
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
        // A second copy spawned by a newly loaded scene: destroy the duplicate
        // GameObject and keep the original (which still holds all the photos).
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Fresh session start => no leftover photos from any previous run.
        // This only runs for the first instance, so it does NOT fire on scene
        // changes within the same session.
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
