using UnityEngine;

public class PanelControl : MonoBehaviour
{
    public GameObject CameraPanel;
    public GameObject SettingsPanel;
    public GameObject LibraryPanel;
    public GameObject NotebookPanel;
    public GameObject EvidencePanel;
    public GameObject IpadScreenPanel;

    public void OpenCamera() {
        CloseAll();
        CameraPanel.SetActive(true);
    }

    public void OpenSettings() {
        CloseAll();
        SettingsPanel.SetActive(true);
    }

    public void OpenLibrary() {
        CloseAll();
        LibraryPanel.SetActive(true);
    }

    public void OpenNotebook() {
        CloseAll();
        NotebookPanel.SetActive(true);
    }

    public void OpenIpadScreenPanel() {
        CloseAll();
        IpadScreenPanel.SetActive(true);
    }

    public void OpenEvidence() {
        CloseAll();
        EvidencePanel.SetActive(true);
    }

    private void CloseAll() {
        CameraPanel.SetActive(false);
        SettingsPanel.SetActive(false);
        LibraryPanel.SetActive(false);
        NotebookPanel.SetActive(false);
        EvidencePanel.SetActive(false);
        IpadScreenPanel.SetActive(false);
    }
}
