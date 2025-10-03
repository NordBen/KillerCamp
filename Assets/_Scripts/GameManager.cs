using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public string playerName;

    public NetworkList<FixedString32Bytes> players = new NetworkList<FixedString32Bytes>();
    public Dictionary<ulong, FixedString32Bytes> playersDict;

    [SerializeField] private NetworkList<Vector3> _playerTransform = new NetworkList<Vector3>();

    public float MinX;
    public float MaxX;
    public float MinY;
    public float MaxY;
    public float MinZ;
    public float MaxZ;

    public Action OnGameStarted;

    public NetworkList<bool> playersReady = new NetworkList<bool>();

    public Image buttonBackground;
    
    public static GameManager instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log($"On Network Spawn");
            playersDict = new Dictionary<ulong, FixedString32Bytes>();
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            base.OnNetworkSpawn();
        }/*
        if (IsClient) 
        {
            SetPlayerNameRpc(playerName);
        }*/
        
    }

    private void TryStartGame()
    {
        bool canStartGame = false;
        Debug.Log($"Trying to StartGame");
        foreach (var playerReady in playersReady)
        {
            if (!playerReady) return;
        }
        canStartGame = true;
        Debug.Log($"Managed to StartGame");

        ServerStartGameRpc();
    }

    [Rpc(SendTo.Server)]
    private void ServerStartGameRpc(RpcParams rpcParams = default)
    {
        OnGameStarted?.Invoke();
        foreach (var playerId in players)
        {
            GameObject player = NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId].PlayerObject.gameObject;
            Vector3 randomPosition = new Vector3 (UnityEngine.Random.Range(MinX, MaxX), UnityEngine.Random.Range(MinY, MaxY), UnityEngine.Random.Range(MinZ, MaxZ));
            player.transform.position = randomPosition;
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
/*
    [Rpc(SendTo.Server)]
    private void SetPlayerNameRpc(FixedString32Bytes playerName, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        playersDict[clientId] = playerName;
        UpdateList();
    }*/

    public void UpdatePlayer(ulong clientId, FixedString32Bytes playerName)
    {
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
        TryStartGame();
    }

    public void SetPlayerReady()
    {
        if(IsClient)
        {
            SetPlayerReadyRpc();
        }
    }
}
