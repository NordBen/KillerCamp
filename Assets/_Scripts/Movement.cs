using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 5.0f;
    // Update is called once per frame
    private void Update()
    {
        // float horizontal = Input.GetAxis("Horizontal");
        // float vertical = Input.GetAxis("Vertical");

        // Vector3 movement = new Vector3(horizontal, vertical, 0.0f);

        // Vector3 newPosition = transform.position + movement.normalized * movementSpeed * Time.deltaTime;



        // this.transform.position = newPosition;
        
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
