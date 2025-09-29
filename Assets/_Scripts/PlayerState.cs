using System;
using UnityEngine;
using Unity.Netcode;

public class PlayerState : NetworkBehaviour
{
    [SerializeField] private string playerName;
    
    public PlayerData PlayerData => playerData;
    private PlayerData playerData;
    private NetworkVariable<Color> playerColor = new ();

    private GameObject carPartOne;
    private GameObject carPartTwo;
    private GameObject carPartThree;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playerColor.Value = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
        }
        if (IsClient)
        {
            playerData = new PlayerData(playerName, GetRole());
            AddPlayerToGameServerRpc();
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
        GetComponent<SpriteRenderer>().color = newColor;
    }

    [Rpc(SendTo.Server)]
    private void ServerSetPlayerColorRpc()
    {
        Color newColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
        Debug.Log(newColor);
        ClientSetPlayerColorRpc(newColor);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ClientSetPlayerColorRpc(Color newColor)
    {
        GetComponent<SpriteRenderer>().color = newColor;
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

    public void AddCarPart(GameObject carPart)
    {

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