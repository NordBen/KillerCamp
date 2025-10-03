using UnityEngine;
using System.Collections;
using TMPro;

public class Fishing : MonoBehaviour, IInteract
{

    [SerializeField] private GameObject currentPlayer;
    [SerializeField] private float cooldown = 5f;
    private AudioSource fishingSound;

    private void Awake()
    {
        fishingSound = GetComponent<AudioSource>();
    }
    public void Interact()
    {
        Debug.Log("Player started fishing");
        fishingSound.Play();
    }

    public bool CanInteract()
    {
        Debug.Log("Player can fish");
        return true;
    }
    public bool IsInteracting()
    {
        Debug.Log("Player is fishing");
        return true;
    }
    private IEnumerator Countdown()
    {
        Debug.Log("Task Started");
        yield return new WaitForSeconds(cooldown);

        Debug.Log("You're all done!");

        //currentPlayer.GetComponent<TaskManager>().CompleteTaskRpc();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            currentPlayer = collision.gameObject;
            CanInteract();
            Debug.Log("Press E to interact!");
            /*
            if (currentPlayer.GetComponent<TaskManager>().HasTask.Value == true && Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(Countdown());
            }*/
        }
    }
}
