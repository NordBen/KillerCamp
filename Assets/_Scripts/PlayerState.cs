using System;
using TMPro;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class PlayerState : NetworkBehaviour
{
    [SerializeField] 
    private NetworkVariable<FixedString32Bytes> playerName;

    [SerializeField] private TextMeshProUGUI nameTagText;
    
    public FixedString32Bytes PlayerName => playerName.Value;
    
    private NetworkVariable<PlayerData>  playerData = new();
    public PlayerData PlayerData => playerData.Value;
    
    private NetworkVariable<Color> playerColor = new();
    
    private SpriteRenderer spriteRenderer;
    public SpriteRenderer SpriteData => spriteRenderer;
    
    private FixedString32Bytes[] playerNames = {"Bob", "Alice", "Carol", "Dave", "Eve", "Frank", "George", "Harry", "Ian", "Jane"};

    public override void OnNetworkSpawn()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (IsServer)
        {
            playerColor.Value = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
        }
        if (IsClient && IsOwner)
        {
            //var ServerController = FindObjectOfType<ServerController>();
            //playerName = ServerController.PlayerName;
            
            AddPlayerToGameServerRpc(playerNames[Random.Range(0, playerNames.Length)], GetRole());
        }
        Debug.Log("Player spawned");

        playerColor.OnValueChanged += (oldColor, newColor) =>
        {
            ApplyColor(newColor);
        };

        ApplyColor(playerColor.Value);
        
        base.OnNetworkSpawn();
    }

    void ApplyColor(Color newColor)
    {
        spriteRenderer.color = newColor;
    }

    private CamperRole GetRole()
    {
        return GetComponent<RoleComponent>().Role;
    }

    [Rpc(SendTo.Server)]
    private void AddPlayerToGameServerRpc(FixedString32Bytes name, CamperRole role, RpcParams rpcParams = default)
    {
        Debug.Log(name.ToString());
        playerName.Value = name;
        playerData.Value = new PlayerData(name, role);
        ulong clientId = rpcParams.Receive.SenderClientId;
        GameManager.Instance.UpdatePlayer(clientId, name);
        SetupPlayerNameTagsClientRpc();
    }

    [ClientRpc]
    private void SetupPlayerNameTagsClientRpc(ClientRpcParams rpcParams = default)
    {
        nameTagText.text = playerData.Name.ToString();
    }
}

[Serializable]
public struct PlayerData : INetworkSerializable
{
    public FixedString32Bytes Name;
    public CamperRole Role;
    
    public PlayerData(FixedString32Bytes inName, CamperRole inRole)
    {
        Name = inName;
        Role = inRole;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Role);
    }
}