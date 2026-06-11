using UnityEngine;

public class SimplePlayerMove2 : MonoBehaviour
{
    public float speed = 3f;

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 dir = new Vector3(h, 0f, v);
        transform.Translate(dir * speed * Time.deltaTime, Space.World);
    }
}
