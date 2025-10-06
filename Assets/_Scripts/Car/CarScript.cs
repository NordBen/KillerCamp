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
        if (other.CompareTag("Player"))
        {
            inRange = true;
            other.GetComponent<InteractionHandler>().SetInteract(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        inRange = false;
    }

    [Rpc(SendTo.Server)]
    private void ServerTryInteractWithCarRpc()
    {
        if (partsCounter.Value >= 3)
        {
            GameManager.Instance.Win();
        }
        
        CarInteractedClientRpc();
    }

    public void AddPart()
    {
        if (!IsServer) return;//ServerAddPartRpc();
        
        partsCounter.Value++;
        UpdateCarSpriteClientRpc();
    }

    [ClientRpc]
    private void CarInteractedClientRpc()
    {
        if (carSound != null) carSound.Play();
    }

    [ClientRpc]
    private void UpdateCarSpriteClientRpc()
    {
        int carSprite = 0;
        if (partsCounter.Value > 2)
        {
            carSprite = 1;
        }
        spriteRenderer.sprite = carSprites[carSprite];
    }
}
