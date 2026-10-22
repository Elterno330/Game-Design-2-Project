using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public bool walkIn3D = false;
    public float moveSpeed = 10f;
    Rigidbody rb;
    Vector3 spawnPosition;
    public GameManager gameManager;
    public Camera dayCamera;
    public Camera nightCamera;
    public bool accusationMode = false;
    [System.NonSerialized] public bool inputLocked;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
    }

    void Update()
    {
        if (inputLocked) return;
        var keyboard = Keyboard.current;
        if (walkIn3D)
        {
            if (transform.position.y < spawnPosition.y - 15f ||
                (keyboard != null && keyboard.rKey.wasPressedThisFrame))
            {
                rb.position = spawnPosition;
                rb.linearVelocity = Vector3.zero;
            }
            return;
        }
        if (gameManager == null || keyboard == null) return;
        if(gameManager.gameOver)
            return;
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if(!accusationMode)
            {
                gameManager.nightPlane.SetActive(true);
                gameManager.NPCDialog.dialog.text = "Are you sure you want to make an accusation? Press F to confirm. Press D to cancel.";
                gameManager.NPCDialog.dialog.gameObject.SetActive(true);
                accusationMode = true;
            }
            else
            {
                gameManager.accusation();
                gameManager.choosingSuspect = true;
            }
        }
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            if (accusationMode)
            {
                gameManager.NPCDialog.dialog.gameObject.SetActive(false);
                gameManager.nightPlane.SetActive(false);
                accusationMode = false;
            }
        }
        if (gameManager.choosingSuspect)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                gameManager.checkAccusation("Daisy");
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                gameManager.checkAccusation("Sebes");
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                gameManager.checkAccusation("Percy");
            }
            else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                gameManager.checkAccusation("Elara");
            }
        }
    }

    void FixedUpdate()
    {
        if (inputLocked)
        {
            rb.linearVelocity = walkIn3D ? new Vector3(0f, rb.linearVelocity.y, 0f) : Vector3.zero;
            return;
        }
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (walkIn3D)
        {
            Vector3 direction = Vector3.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) direction += Vector3.forward;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) direction += Vector3.back;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction += Vector3.left;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction += Vector3.right;
            Vector3 velocity = direction.normalized * moveSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
            return;
        }
        if (gameManager == null) return;
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
