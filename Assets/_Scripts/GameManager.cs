using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using System;

public class GameManager : NetworkBehaviour
{
    public string playerName;

    public NetworkList<FixedString32Bytes> players = new NetworkList<FixedString32Bytes>();
    public Dictionary<ulong, FixedString32Bytes> playersDict;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playersDict = new Dictionary<ulong, FixedString32Bytes>();
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            base.OnNetworkSpawn();
        }
        if (IsOwner && IsClient) 
        {
            SetPlayerNameRpc(playerName);
        }
        
    }

    [Rpc(SendTo.Server)]
    private void SetPlayerNameRpc(FixedString32Bytes playerName, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        playersDict[clientId] = playerName;
        UpdateList();
    }

    private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        playersDict.Add(obj, new FixedString32Bytes());
        UpdateList();
    }

    private void UpdateList()
    {
        players.Clear();
        foreach (var kv in playersDict)
        {
            players.Add(kv.Value);
        }
        players.SetDirty(true);
    }
}
