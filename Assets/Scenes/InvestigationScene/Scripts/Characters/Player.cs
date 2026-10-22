using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    Rigidbody rb;
    public GameManager gameManager;
    public Camera dayCamera;
    public Camera nightCamera;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (gameManager.nightTime == false)
        {
            Vector3 movement = Vector3.zero;

            if (Keyboard.current.upArrowKey.isPressed)
                movement += Vector3.up;

            if (Keyboard.current.downArrowKey.isPressed)
                movement += Vector3.down;

            if (Keyboard.current.leftArrowKey.isPressed)
                movement += Vector3.left;

            if (Keyboard.current.rightArrowKey.isPressed)
                movement += Vector3.right;

            movement = movement.normalized;

            rb.linearVelocity = movement * 10f;
        }
        else if (gameManager.nightTime == true)
        {
            Vector3 movement = Vector3.zero;

            // Move forward relative to player's rotation
            if (Keyboard.current.upArrowKey.isPressed)
                movement += transform.up;

            // Move backward relative to player's rotation
            if (Keyboard.current.downArrowKey.isPressed)
                movement -= transform.up;

            // Rotate left
            if (Keyboard.current.leftArrowKey.isPressed)
                rb.MoveRotation(
                    rb.rotation * Quaternion.Euler(0f, 0f, 90f * 1.5f * Time.fixedDeltaTime)
                );

            // Rotate right
            if (Keyboard.current.rightArrowKey.isPressed)
                rb.MoveRotation(
                    rb.rotation * Quaternion.Euler(0f, 0f, -90f * 1.5f * Time.fixedDeltaTime)
                );

            movement = movement.normalized;

            rb.linearVelocity = movement * 10f;
        }
    }
}