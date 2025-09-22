using UnityEngine;
using Unity.Netcode;

public class Movement : NetworkBehaviour
{
    [SerializeField] private float movementSpeed = 5.0f;
    // Update is called once per frame
    private void Update()
    {        
        float horizontal = Input.GetAxisRaw("Horizontal"); // instant input
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(horizontal, vertical, 0.0f);

        if (movement != Vector3.zero) // only normalize when moving
        {
            movement = movement.normalized;
        }

        transform.position += movement * movementSpeed * Time.deltaTime;
    }
}
