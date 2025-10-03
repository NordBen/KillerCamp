using KillerCamp;
using UnityEngine;

public class CarScript : MonoBehaviour, IInteract
{
    public int partsCounter = 0;
    private bool inRange = false;

    private AudioSource carSound;

    private void Awake()
    {
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
        }
        else
        {
            Debug.Log("Not enough parts");
        }
    }
}
