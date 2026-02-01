using UnityEngine;
using BNG;

public class TabletRecall : MonoBehaviour
{

    [Header("Refs")] public Transform playerCamera; // CenterEyeAnchor / Main Camera (HMD)
    public Transform tabletRoot; // Tablet (корневой объект)
    public Grabbable tabletGrabbable; // Grabbable на Tablet

    [Header("Spawn")] public float distance = 0.6f;
    public float heightOffset = -0.05f;
    public bool facePlayer = true;

    [Header("Input")] public bool useBButton = true; // Quest: B (Right controller)
    public bool enableKeyboardTest = true; // Editor/PC: клавиша B

    void Reset()
    {
        // Editor-friendly
        if (Camera.main) playerCamera = Camera.main.transform;
    }

    void Awake()
    {
        // На всякий случай, если Reset не сработал / сцена грузится иначе
        if (!playerCamera)
        {
            if (Camera.main) playerCamera = Camera.main.transform;
            else
            {
                var cam = FindObjectOfType<Camera>();
                if (cam) playerCamera = cam.transform;
            }
        }
    }

    void Update()
    {
        Debug.Log("TabletRecall alive");
        
        bool pressed = false;

        // Quest кнопка B
        if (useBButton && InputBridge.Instance != null)
        {
            pressed |= InputBridge.Instance.BButtonDown;
        }

        // ПК тест в Editor
        if (enableKeyboardTest)
        {
            pressed |= Input.GetKeyDown(KeyCode.B);
        }

        if (pressed)
        {
            Recall();
        }
        
        if (useBButton && InputBridge.Instance != null && InputBridge.Instance.BButtonDown) {
            Debug.Log("B pressed (Quest / InputBridge)");
        }
        if (enableKeyboardTest && Input.GetKeyDown(KeyCode.B)) {
            Debug.Log("B pressed (Keyboard)");
        }
        
    }
    
    

    public void Recall()
    {
        if (!playerCamera || !tabletRoot) return;

        if (tabletGrabbable && tabletGrabbable.BeingHeld)
        {
            tabletGrabbable.DropItem(false, true);
        }

        Rigidbody rb = tabletRoot.GetComponent<Rigidbody>();
        if (!rb && tabletGrabbable) rb = tabletGrabbable.GetComponent<Rigidbody>();

        Transform t = rb ? rb.transform : tabletRoot;

        // отцепить от любых родителей (рук и т.п.)
        t.SetParent(null, true);

        Vector3 spawnPos =
            playerCamera.position +
            playerCamera.forward * distance +
            playerCamera.up * heightOffset;

        t.position = spawnPos;

        if (facePlayer)
        {
            Vector3 forwardFlat = Vector3.ProjectOnPlane(playerCamera.forward, Vector3.up).normalized;
            if (forwardFlat.sqrMagnitude < 0.001f) forwardFlat = playerCamera.forward.normalized;
            t.rotation = Quaternion.LookRotation(forwardFlat, Vector3.up);
        }

        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }
}
