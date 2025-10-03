using KillerCamp;
using Unity.Netcode;
using UnityEngine;

public class CarParts : NetworkBehaviour, IInteract
{
    private bool inRange;
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
        if(inRange)
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
        if(other.CompareTag("Player"))
        {
            inRange = true;
            other.GetComponent<InteractionHandler>().SetInteract(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inRange = false;
        }
    }

    private void PickUp()
    {
        ServerPickUpRpc();
    }

    [Rpc(SendTo.Server)]
    private void ServerPickUpRpc()
    {
        car.AddPart();
        gameObject.SetActive(false);
    }
}
