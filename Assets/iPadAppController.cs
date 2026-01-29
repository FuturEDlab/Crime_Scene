using UnityEngine;

public class iPadAppController : MonoBehaviour
{
    public GameObject cameraApp;
    public GameObject photoLibraryApp;

    public void OpenCamera()
    {
        cameraApp.SetActive(true);
        photoLibraryApp.SetActive(false);
    }

    public void OpenLibrary()
    {
        cameraApp.SetActive(false);
        photoLibraryApp.SetActive(true);
    }
}