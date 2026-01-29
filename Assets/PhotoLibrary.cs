using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PhotoLibrary : MonoBehaviour
{
    public Image displayImage;
    public GameObject noPhotosText;

    private List<Texture2D> photos = new List<Texture2D>();
    private int currentIndex = 0;

    public void AddPhoto(Texture2D photo)
    {
        photos.Add(photo);
        currentIndex = photos.Count - 1;
        UpdateDisplay();
    }

    public void NextPhoto()
    {
        if (photos.Count == 0) return;
        currentIndex = (currentIndex + 1) % photos.Count;
        UpdateDisplay();
    }

    public void PrevPhoto()
    {
        if (photos.Count == 0) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = photos.Count - 1;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (photos.Count == 0)
        {
            displayImage.gameObject.SetActive(false);
            noPhotosText.SetActive(true);
            return;
        }

        displayImage.gameObject.SetActive(true);
        noPhotosText.SetActive(false);

        Sprite s = Sprite.Create(photos[currentIndex],
            new Rect(0, 0, photos[currentIndex].width, photos[currentIndex].height),
            new Vector2(0.5f, 0.5f));

        displayImage.sprite = s;
    }

    public void ClearPhotos()
    {
        photos.Clear();
        UpdateDisplay();
    }
}