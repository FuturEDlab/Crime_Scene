using UnityEngine;

public class CameraCapture : MonoBehaviour
{
    public Camera captureCamera;
    public RenderTexture renderTexture;
    public PhotoLibrary photoLibrary;

    public void TakePhoto()
    {
        Texture2D photo = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

        RenderTexture.active = renderTexture;
        captureCamera.Render();
        photo.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        photo.Apply();

        photoLibrary.AddPhoto(photo);
    }
}
