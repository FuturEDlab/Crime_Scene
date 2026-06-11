using UnityEngine;
using UnityEngine.XR.Management;

public class RigSwitcher : MonoBehaviour
{
    [Header("Assign these in the Inspector")]
    public GameObject pcRig;   // Your "Player" object (PC)
    public GameObject xrRig;   // XR Origin Hands (XR Rig)

    private void Start()
    {
        bool xrActive = false;

        var general = XRGeneralSettings.Instance;
        if (general != null && general.Manager != null)
        {
            xrActive = general.Manager.activeLoader != null;
        }

        if (pcRig != null) pcRig.SetActive(!xrActive);
        if (xrRig != null) xrRig.SetActive(xrActive);

        Debug.Log($"[RigSwitcher] XR active = {xrActive}. PC Rig active = {!xrActive}, XR Rig active = {xrActive}");
    }
}
