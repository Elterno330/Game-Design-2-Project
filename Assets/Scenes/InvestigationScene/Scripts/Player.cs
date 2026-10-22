using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public bool walkIn3D = false;
    public float moveSpeed = 10f;
    Rigidbody rb;
    Vector3 spawnPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
    }

    void Update()
    {
        if (!walkIn3D || rb == null) return;
        if (transform.position.y < spawnPosition.y - 15f ||
            (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame))
        {
            rb.position = spawnPosition;
            rb.linearVelocity = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        Vector3 movement = Vector3.zero;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.upArrowKey.isPressed || (walkIn3D && keyboard.wKey.isPressed))
            movement += walkIn3D ? Vector3.forward : Vector3.up;

        if (keyboard.downArrowKey.isPressed || (walkIn3D && keyboard.sKey.isPressed))
            movement += walkIn3D ? Vector3.back : Vector3.down;

        if (keyboard.leftArrowKey.isPressed || (walkIn3D && keyboard.aKey.isPressed))
            movement += Vector3.left;

        if (keyboard.rightArrowKey.isPressed || (walkIn3D && keyboard.dKey.isPressed))
            movement += Vector3.right;

        movement = movement.normalized;

        Vector3 velocity = movement * moveSpeed;
        if (walkIn3D) velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }
}
