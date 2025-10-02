using UnityEngine;

public class CarScript : MonoBehaviour
{
    public int partsCounter = 0;
    private bool inRange = false;

    private void Update()
    {
        if (inRange && Input.GetKeyDown(KeyCode.E))
        {
            CheckParts();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        inRange = true;
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
