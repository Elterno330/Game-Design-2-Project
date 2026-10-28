using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class NPCDialog : MonoBehaviour
{
    public TextMeshProUGUI dialog;
    public GameObject backgroundBox;
    public GameObject interactionPrompt;
    public GameManager gameManager;
    public MansionPrototype prototype;
    public string prototypeCharacterId;
    bool playerNear = false;

    void Start()
    {
        if (prototype != null) return;
        dialog.gameObject.SetActive(false);
        backgroundBox.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    void Update()
    {
        if (prototype != null) return;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && playerNear)
        {
            dialog.gameObject.SetActive(true);
            backgroundBox.SetActive(true);
            // The mansion exploration scene can run without the day/night manager.
            if (gameManager != null && gameManager.numberOfActions > 0)
                commitAction();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (prototype != null) return;
        if (other.CompareTag("Player"))
        {
            playerNear = true;
            if (interactionPrompt != null) interactionPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (prototype != null) return;
        if (other.CompareTag("Player"))
        {
            playerNear = false;
            dialog.gameObject.SetActive(false);
            backgroundBox.SetActive(false);
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
        }
    }

    void commitAction()
    {
        gameManager.actionUsed();
    }
}
