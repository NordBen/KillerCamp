using UnityEngine;
using Unity.Netcode;

public class Movement : NetworkBehaviour
{
    [SerializeField] 
    private float movementSpeed = 5.0f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 lastPosition;
    private float _elapsedTime;
    private const float movementSyncRate = .01f;

    private NetworkVariable<bool> flippedX = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// COOL COLOR
    /// red - 29
    /// green - 219
    /// blue - 237
    /// alpha - 255
    ///
    /// hexcode - 1DDBED
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            flippedX.Value = false;
        }

        flippedX.OnValueChanged += OnDirectionChange;
        
        OnDirectionChange(false, flippedX.Value);
    }

    public override void OnNetworkDespawn()
    {
        flippedX.OnValueChanged -= OnDirectionChange;
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (!IsOwner) return;

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement.Normalize();

        HandleDirectionChange();

        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= movementSyncRate)
        {
            _elapsedTime = 0f;

            if (movement != lastPosition)
            {
                ServerUpdateMovementRpc(movement);
                lastPosition = movement;
            }
        }
    }

    private void FixedUpdate()
    {
        if (IsServer) ServerMovement();
    }

    private void HandleDirectionChange()
    {
        if (!IsOwner) return;
        
        if (movement.x > 0)
        {
            flippedX.Value = true;
        }
        else if (movement.x < 0)
        {
            flippedX.Value = false;
        }
    }

    private void OnDirectionChange(bool newValue, bool oldValue)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = newValue;
    }

    [Rpc(SendTo.Server)]
    private void ServerUpdateMovementRpc(Vector2 movementInput)
    {
        movement = movementInput;
    }

    private void ServerMovement()
    {
        if (!IsServer) return;
        
        Vector2 newPosition = rb.position + movement * movementSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }
}
