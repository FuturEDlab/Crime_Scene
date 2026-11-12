using UnityEngine;
using UnityEngine.UI;

// Parts of one Photo Library grid tile, so PhotoLibraryScreen can fill a
// cloned tile without searching its children by name.
public class PhotoTileRefs : MonoBehaviour
{
    public RawImage photo;
    public Image border;

    [Tooltip("The \"Evidence\" strip + magnifier, shown only on evidence photos.")]
    public GameObject evidenceTag;
}
