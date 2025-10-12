using KillerCamp;
using KillerCamp.TaskSystem;
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
            ServerTryPickupRpc();
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
            var interactionHandler = other.GetComponent<InteractionHandler>();
            if (interactionHandler == null) return;
            interactionHandler.SetInteract(NetworkObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inRange = false;
            var interactionHandler = other.GetComponent<InteractionHandler>();
            if (interactionHandler == null) return;
            interactionHandler.SetInteract(null);
        }
    }

    [Rpc(SendTo.Server)]
    private void ServerTryPickupRpc()
    {
        car.AddPart();
        PickupClientRpc();
        NetworkObject.gameObject.SetActive(false);
    }
    
    [ClientRpc]
    private void PickupClientRpc()
    {
        if (pickupSound != null) pickupSound.Play();
    }
}
