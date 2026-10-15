using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class NPCDialog : MonoBehaviour
{
    public GameObject dialog;
    Boolean playerNear = false;

    void Start()
    {
        dialog.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current.eKey.isPressed && playerNear)
        {
            dialog.SetActive(true);
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
            dialog.SetActive(false);
        }
    }
}
