using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


public class GameManager : MonoBehaviour
{
    public bool gameOver = false;
    public int numberOfActions = 5;
    public Boolean nightTime = false;
    public GameObject nightPlane;
    public NPCDialog NPCDialog;
    public Player player;
    Vector3 startingPosition = new Vector3(0, 0, 0);
    Quaternion startingRotation;
    public bool choosingSuspect = false;
    public bool scriptedPrototype = false;



    void Start()
    {
        startingPosition = player.transform.position;
        startingRotation = player.transform.rotation;
    }

    public void actionUsed()
    {
        numberOfActions--;
        // The scripted prototype changes days after the interrogation finishes.
        if (scriptedPrototype) return;
        if (numberOfActions <= 0)
        {
            // Handle the case when no actions are left
            Debug.Log("No actions left!");
            nightTransition();
        }
    }

    public void nightTransition()
    {
        // Handle the transition to night time
        Debug.Log("Transitioning to night time...");
        nightTime = true;
        nightPlane.SetActive(true);
        NPCDialog.dialog.gameObject.SetActive(true);
        NPCDialog.backgroundBox.SetActive(false);
        NPCDialog.dialog.text = "The Moon Rises, and the Night Falls";
        StartCoroutine(HideNightScreenAfterDelay());
        StartCoroutine(nightTimeClock());
    }

    public void dayTransition()
    {
        // Handle the transition to day time
        Debug.Log("Transitioning to day time...");
        nightTime = false;
        numberOfActions = 5;
        nightPlane.SetActive(true);
        NPCDialog.dialog.gameObject.SetActive(true);
        NPCDialog.backgroundBox.SetActive(false);
        NPCDialog.dialog.text = "The Sun Rises, and a New Day Begins";
        StartCoroutine(HideNightScreenAfterDelay());
        player.transform.position = startingPosition;
        player.transform.rotation = startingRotation;
    }

    public void accusation()
    {
        Debug.Log("Accusation made!");
        NPCDialog.dialog.text = "Who do you want to accuse? (1)Daisy,(2)Sebes,(3)Percy,(4)Elara";
        choosingSuspect = true;
    }

    public void checkAccusation(string suspect)
    {
        player.accusationMode = false;
        choosingSuspect = false;

        if (suspect == "Sebes")
        {
            NPCDialog.dialog.text =
                "You correctly identified Sebes as the werewolf!";

            Debug.Log("Player wins!");
        }
        else
        {
            NPCDialog.dialog.text =
                "You accused the wrong person!";

            Debug.Log("Player loses!");
        }
        gameOver = true;
    }

    IEnumerator HideNightScreenAfterDelay()
    {
        if(!nightTime)
        {
            player.dayCamera.gameObject.SetActive(true);
            player.nightCamera.gameObject.SetActive(false);
        }
        yield return new WaitForSeconds(3f);
        NPCDialog.dialog.gameObject.SetActive(false);
        nightPlane.SetActive(false);
        if (nightTime)
        {
            player.dayCamera.gameObject.SetActive(false);
            player.nightCamera.gameObject.SetActive(true);
        }

    }

    IEnumerator nightTimeClock()
    {
        yield return new WaitForSeconds(10f);
        dayTransition();
    }
}
