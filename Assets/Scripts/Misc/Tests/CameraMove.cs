using UnityEngine;

public class CameraMove : MonoBehaviour
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }


    private void FixedUpdate()
    {
        Vector3 move = rb.position + GameInput.Instance.GetMovementVector() * (3f * Time.fixedDeltaTime);
        rb.transform.position = (move);
    }
}
