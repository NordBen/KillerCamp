using System.Collections.Generic;
using KillerCamp;
using Unity.Netcode;
using UnityEngine;

public class CarScript : NetworkBehaviour, IInteract
{
    [SerializeField] 
    private NetworkVariable<int> partsCounter = new(0);
    
    [SerializeField]
    private List<Sprite> carSprites;

    private bool inRange = false;

    private SpriteRenderer spriteRenderer;
    private AudioSource carSound;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        carSound = GetComponent<AudioSource>();
    }
    
    public bool CanInteract()
    {
        return inRange;
    }

    public void Interact()
    {
        if (inRange)
        {
            Debug.Log("Checking parts");
            ServerTryInteractWithCarRpc();
        }
    }

    public bool IsInteracting()
    {
        return inRange;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        inRange = true;
        var interactionHandler = other.GetComponent<InteractionHandler>();
        if (interactionHandler == null) return;
        interactionHandler.SetInteract(NetworkObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        inRange = false;
        var interactionHandler = other.GetComponent<InteractionHandler>();
        if (interactionHandler == null) return;
        interactionHandler.SetInteract(null);
    }

    [Rpc(SendTo.Server)]
    private void ServerTryInteractWithCarRpc()
    {
        CarInteractedClientRpc();
        if (partsCounter.Value >= 3)
        {
            GameManager.Instance.Win(true);
        }
    }

    public void AddPart()
    {
        if (!IsServer) return;
        
        partsCounter.Value++;
        UpdateCarSpriteClientRpc();
    }

    [ClientRpc]
    private void CarInteractedClientRpc(ClientRpcParams rpcParams = default)
    {
        if (carSound != null) carSound.Play();
    }

    [ClientRpc]
    private void UpdateCarSpriteClientRpc()
    {
        if (partsCounter.Value > 2)
        {
            spriteRenderer.sprite = carSprites[1];
        }
    }
}