using UnityEngine;

public class BasicKeyboardMover : MonoBehaviour
{
    public float moveSpeed = 2f;

    void Update()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S
        float y = 0;

        if (Input.GetKey(KeyCode.E)) y += 1; // Move up
        if (Input.GetKey(KeyCode.Q)) y -= 1; // Move down

        Vector3 move = new Vector3(h, y, v) * moveSpeed * Time.deltaTime;
        transform.Translate(move);
    }
}
