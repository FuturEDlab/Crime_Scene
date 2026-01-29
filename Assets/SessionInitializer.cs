using UnityEngine;

public class SessionInitializer : MonoBehaviour
{
    void Start()
    {
        var photoLibrary = Object.FindFirstObjectByType<PhotoLibrary>();
        if (photoLibrary != null)
        {
            photoLibrary.ClearPhotos();
        }
    }
}