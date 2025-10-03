using System.Collections.Generic;
using KillerCamp;
using Unity.Netcode;
using UnityEngine;

public class CarScript : NetworkBehaviour, IInteract
{
    [SerializeField] 
    private int partsCounter = 0;
    public int Parts { get => partsCounter; set => partsCounter = value; }
    
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
            CheckParts();
            Debug.Log("Checking parts");
            carSound.Play();
        }
    }

    public bool IsInteracting()
    {
        return inRange;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Help");
        inRange = true;
        other.GetComponent<InteractionHandler>().SetInteract(this);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        inRange = false;
    }

    private void CheckParts()
    {
        if(partsCounter >= 3)
        {
            Debug.Log("You won!");
            GameManager.Instance.Win();
        }
        else
        {
            Debug.Log("Not enough parts");
            GameManager.Instance.Lose();
        }
    }

    public void AddPart()
    {
        ServerAddPartRpc();
    }

    [Rpc(SendTo.Server)]
    private void ServerAddPartRpc()
    {
        partsCounter++;
        spriteRenderer.sprite = carSprites[partsCounter - 1];
    }
}
