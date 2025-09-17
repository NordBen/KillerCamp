using UnityEngine;
using TMPro;

public class Fishing : MonoBehaviour, IInteract
{
    public void Interact()
    {
        Debug.Log("Player started fishing");
    }

    public bool CanInteract()
    {
        Debug.Log("Player can fish");
        return true;
    }
    public bool IsInteracting()
    {
        Debug.Log("Player is fishing");
        return true;
    }
}
