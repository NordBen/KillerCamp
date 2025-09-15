using UnityEngine;

public class Movement : MonoBehaviour
{
    // Update is called once per frame
    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, vertical, 0.0f);

        Vector3 newPosition = transform.position + movement * Time.deltaTime;

        this.transform.position = newPosition;
    }
}
