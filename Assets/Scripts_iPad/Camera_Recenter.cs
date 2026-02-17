using UnityEngine;
using UnityEngine.XR;

public class Camera_Recenter : MonoBehaviour
{
    void Start()
    {
        XRDevice.SetTrackingSpaceType(TrackingSpaceType.RoomScale);
        InputTracking.Recenter();
    }
}
