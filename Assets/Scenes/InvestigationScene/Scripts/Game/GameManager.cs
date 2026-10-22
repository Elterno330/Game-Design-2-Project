using System;
using UnityEngine;
using System.Collections;


public class GameManager : MonoBehaviour
{
    public int numberOfActions = 5;
    public Boolean nightTime = false;
    public GameObject nightPlane;
    public NPCDialog NPCDialog;
    public Player player;
    Vector3 startingPosition = new Vector3(0, 0, 0);
    Quaternion startingRotation;



    void Start()
    {
        startingPosition = player.transform.position;
        startingRotation = player.transform.rotation;
    }

    public void actionUsed()
    {
        numberOfActions--;
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
