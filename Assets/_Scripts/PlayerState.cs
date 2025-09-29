using System;
using UnityEngine;
using Unity.Netcode;

public class PlayerState : NetworkBehaviour
{
    [SerializeField] private string playerName;
    
    public PlayerData PlayerData => playerData;
    private PlayerData playerData;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ServerSetPlayerColorRpc();
        }
        if (IsClient)
        {
            playerData = new PlayerData(playerName, GetRole());
            AddPlayerToGameServerRpc();
        }
        Debug.Log("Player spawned");

        base.OnNetworkSpawn();
    }
    [Rpc(SendTo.Server)]
    private void ServerSetPlayerColorRpc()
    {
        ClientSetPlayerColorRpc(UnityEngine.Random.ColorHSV());
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ClientSetPlayerColorRpc(Color newColor)
    {
        GetComponent<SpriteRenderer>().color = new Color();
    }

    private CamperRole GetRole()
    {
        return CamperRole.Camper;
    }

    [Rpc(SendTo.Server)]
    private void AddPlayerToGameServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        GameManager.instance.UpdatePlayer(clientId, playerData.Name);
    }
}

[Serializable]
public struct PlayerData
{
    public string Name;
    public CamperRole Role;
    
    public PlayerData(string inName, CamperRole inRole)
    {
        Name = inName;
        Role = inRole;
    }
}

[Serializable]
public enum CamperRole { Camper, Killer }