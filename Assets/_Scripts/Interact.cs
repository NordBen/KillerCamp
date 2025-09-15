using UnityEngine;

public interface IInteract
{
    public void Interact();
    public bool CanInteract();
    public bool IsInteracting();
}

public class Berry : MonoBehaviour, IInteract
{
    public void Interact()
    {
        Debug.Log("Is interacting");
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
}