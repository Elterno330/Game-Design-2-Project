using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    Rigidbody rb;
    public GameManager gameManager;
    public Camera dayCamera;
    public Camera nightCamera;
    public bool accusationMode = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
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