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
        if(IsServer && !IsClient)
        {
            rb.MovePosition(rb.position + movement * movementSpeed * Time.fixedDeltaTime);
        }
        else
        {
            float distance = (rb.position + movement * movementSpeed * Time.fixedDeltaTime).magnitude;
            RaycastHit2D[] hits = new RaycastHit2D[10];
            rb.Cast(movement, hits, distance);


            foreach (RaycastHit2D hit in hits)
            {
                if(hit.distance < distance)
                {
                    distance = hit.distance;
                }
            }
            anticipatedTrans.AnticipateMove(rb.position + movement * distance);
        }
        

        //rb.MovePosition(rb.position + movement * movementSpeed * Time.fixedDeltaTime);
    }

    private void LateUpdate()
    {
        anticipatedTrans.AnticipateMove(rb.position);
    }
}
