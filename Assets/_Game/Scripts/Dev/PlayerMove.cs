using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float lookSpeed = 2f;

    private CharacterController controller;
    private Transform cam;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;

        // Lock cursor so mouse look feels normal
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // WASD movement (relative to where player is facing)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Keep player grounded
        controller.Move(Vector3.down * 2f * Time.deltaTime);

        // Mouse look (turn player left/right)
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        transform.Rotate(Vector3.up * mouseX);

        // Camera pitch up/down
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
        Vector3 camRot = cam.localEulerAngles;
        camRot.x -= mouseY;
        cam.localEulerAngles = camRot;
    }
}
