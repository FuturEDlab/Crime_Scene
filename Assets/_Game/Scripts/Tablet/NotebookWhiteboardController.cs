using UnityEngine;

public class NotebookWhiteboardController : MonoBehaviour
{
    public GameObject whiteboard;
    public GameObject marker;

    public void OpenNotebook()
    {
        whiteboard.SetActive(true);
        marker.SetActive(true);
    }

    public void CloseNotebook()
    {
        whiteboard.SetActive(false);
        marker.SetActive(false);
    }
}