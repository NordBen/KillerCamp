using KillerCamp;
using UnityEngine;

public class CarParts : MonoBehaviour, IInteract
{
    private bool inRange;
    private GameObject currentPlayer;
    public CarScript car;

    private AudioSource pickupSound;

    private void Awake()
    {
        pickupSound = GetComponent<AudioSource>();
    }
    public bool CanInteract()
    {
        return inRange;
    }

    public void Interact()
    {
        if(inRange && currentPlayer != null)
        {
            PickUp();
            pickupSound.Play();
        }
    }

    public bool IsInteracting()
    {
        return inRange;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(currentPlayer != null)
        {
            return;
        }
        else if(other.CompareTag("Player"))
        {
            inRange = true;
            currentPlayer = other.gameObject;
            other.GetComponent<InteractionHandler>().SetInteract(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == currentPlayer)
        {
            inRange = false;
            currentPlayer = null;
        }
    }

    private void PickUp()
    {
        car.partsCounter++;
        this.gameObject.SetActive(false);
    }
}
