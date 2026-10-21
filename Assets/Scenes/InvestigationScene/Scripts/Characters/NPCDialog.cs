using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class NPCDialog : MonoBehaviour
{
    public TextMeshProUGUI dialog;
    public GameObject backgroundBox;
    public GameManager gameManager;
    Boolean playerNear = false;

    void Start()
    {
        dialog.gameObject.SetActive(false);
        backgroundBox.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame && playerNear)
        {
            dialog.gameObject.SetActive(true);
            backgroundBox.SetActive(true);
            if(gameManager.numberOfActions > 0)
            {
                commitAction();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            playerNear = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("Player left");
        if (other.CompareTag("Player"))
        {
            playerNear = false;
            dialog.gameObject.SetActive(false);    
            backgroundBox.SetActive(false);
        }
    }

    void commitAction()
    {
        gameManager.actionUsed();
    }
}
