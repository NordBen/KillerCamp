using UnityEngine;
using Unity.Netcode;

public class Movement : NetworkBehaviour
{
    [SerializeField] 
    private float movementSpeed = 5.0f;

    private Rigidbody2D rb;

    private Vector2 movement;

    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// red - 29
    /// green - 219
    /// blue - 237
    /// alpha - 255
    ///
    /// hexcode - 1DDBED
    /// </summary>
    
    // Update is called once per frame
    private void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (movement.x < 0)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * movementSpeed * Time.fixedDeltaTime);
    }
}
