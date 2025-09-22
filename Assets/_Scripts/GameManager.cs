using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public string playerName;

    public NetworkList<FixedString32Bytes> players = new NetworkList<FixedString32Bytes>();
    public Dictionary<ulong, FixedString32Bytes> playersDict;

    public NetworkList<bool> playersReady = new NetworkList<bool>();

    public Image buttonBackground;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log($"On Network Spawn");
            playersDict = new Dictionary<ulong, FixedString32Bytes>();
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            base.OnNetworkSpawn();
        }
        if (IsClient) 
        {
            SetPlayerNameRpc(playerName);
        }
        
    }

    private void FixedUpdate()
    {
        if (!NetworkManager.Singleton.IsConnectedClient) return;
        if(IsClient && playersReady.Count > (int)NetworkManager.Singleton.LocalClientId)
        {
            if(playersReady[(int)NetworkManager.Singleton.LocalClientId])
            {
                buttonBackground.color = Color.green;
            }
            else
            {
                buttonBackground.color = Color.white;
            }
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
        if(IsServer)
        {
            Debug.Log($"Client Connected {obj} ");
            playersDict.TryAdd(obj, new FixedString32Bytes());
            UpdateList();
            playersReady.Add(false);
        }
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

    [Rpc(SendTo.Server)]
    public void SetPlayerReadyRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        playersReady[(int)clientId] = !playersReady[(int)clientId];
    }

    public void SetPlayerReady()
    {
        if(IsClient)
        {
            SetPlayerReadyRpc();
        }
    }
}
