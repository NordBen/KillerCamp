using UnityEngine;
using System.Collections;

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
    private IEnumerator Countdown()
    {
        Debug.Log("3");
        yield return new WaitForSeconds(1);
        Debug.Log("2");
        yield return new WaitForSeconds(1);
            Debug.Log("1");
        yield return new WaitForSeconds(1);
        Debug.Log("You're all done!");
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        CanInteract();
        Debug.Log("Press E to interact!");

        if (Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(Countdown());
        }
    }
}
