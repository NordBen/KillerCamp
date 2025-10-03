using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Berry : NetworkBehaviour, IInteract
{

    [SerializeField] private GameObject currentPlayer;
    [SerializeField] private float cooldown = 3f;
    private AudioSource berrypickingSound;

    private void Awake()
    {
        berrypickingSound = GetComponent<AudioSource>();
    }
  
    public void Interact()
    {
        Debug.Log("Is interacting");
        berrypickingSound.Play();
    }

    public bool CanInteract()
    {
        Debug.Log("Player can interact");
        return true;
    }
    public bool IsInteracting()
    {
        Debug.Log("Player is interacting");
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
