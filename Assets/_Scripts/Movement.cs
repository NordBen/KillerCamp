using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class Movement : NetworkBehaviour
{
    [SerializeField] private float movementSpeed = 5.0f;

    private Rigidbody2D rb;

    private Vector2 movement;

    private float updateTimer = 0;

    private float updateFrequency = 1/60f;

    private AnticipatedNetworkTransform anticipatedTrans;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anticipatedTrans = GetComponent<AnticipatedNetworkTransform>();
    }


    // Update is called once per frame
    private void Update()
    {
        if(IsClient && IsOwner)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");

            updateTimer += Time.deltaTime;

            if (updateTimer >= updateFrequency) 
            {
                updateTimer -= updateFrequency;

                UpdateInputRpc(movement);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void UpdateInputRpc(Vector2 movement)
    {
        this.movement = movement;
    }

    private void FixedUpdate()
    {
        Vector3 newPosition;
        
        var oldPosition = rb.position;
        rb.MovePosition(rb.position + movement * movementSpeed * Time.fixedDeltaTime);
        newPosition = rb.position;
        rb.MovePosition(oldPosition);
        

        anticipatedTrans.AnticipateMove(newPosition);
        //rb.MovePosition(rb.position + movement * movementSpeed * Time.fixedDeltaTime);
    }
}
