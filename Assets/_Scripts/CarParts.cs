using UnityEngine;

public class CarParts : MonoBehaviour, IInteract
{
    private bool inRange;
    private GameObject currentPlayer;

    public bool CanInteract()
    {
        return inRange;
    }

    public void Interact()
    {
        if(inRange && currentPlayer != null)
        {
            PickUp();
        }
    }

    public bool IsInteracting()
    {
        return inRange;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            inRange = true;
            currentPlayer = other.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inRange = false;
            currentPlayer = null;
        }
    }

    private void PickUp()
    {
        //currentPlayer.make thing happen
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
    }
}
