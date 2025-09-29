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
        if (IsClient)
        {
            playerData = new PlayerData(playerName, GetRole());
            AddPlayerToGameServerRpc();
        }
        Color randomColor = UnityEngine.Random.ColorHSV();
        Debug.Log("Player spawned");

        base.OnNetworkSpawn();
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