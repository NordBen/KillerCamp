using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    private NetworkList<FixedString32Bytes> players = new NetworkList<FixedString32Bytes>();
    private Dictionary<ulong, FixedString32Bytes> playersDict;
    
    private List<Transform> startingTransforms = new();
    
    public Action OnGameStarted;

    private NetworkList<bool> playersReady = new NetworkList<bool>();
    
    [SerializeField] private float dayDuration = 90f;

    [SerializeField] private Image buttonBackground;
    
    private bool canStartGame = false;
    
    private bool rolesAssigned = false;
    
    public static GameManager instance;

    private void Awake()
    {
        if (instance == null) instance = this;
        
        GameObject startTransformObj = GameObject.Find("playerStarts");
        for (int i = 0; i < startTransformObj.transform.childCount; i++)
        {
            startingTransforms.Add(startTransformObj.transform.GetChild(i).gameObject.transform);
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

    private void TryStartGame()
    {
        foreach (var playerReady in playersReady)
        {
            if (!playerReady) return;
        }
        canStartGame = true;

        ServerStartGameRpc();
    }

    [Rpc(SendTo.Server)]
    private void ServerStartGameRpc(RpcParams rpcParams = default)
    {
        OnGameStarted?.Invoke();
        
        if (RoleManager.Instance != null && RoleManager.Instance.PlayerCount > 0)
        {
            RoleManager.Instance.AssignRoles();
            rolesAssigned = true;
        }

        StartCoroutine(DayNightCycle());
        
        ServerTeleportPlayersRpc();
    }

    [Rpc(SendTo.Server)]
    private void ServerTeleportPlayersRpc()
    {
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            Vector3 startPos = startingTransforms[Random.Range(0, startingTransforms.Count - 1)].position;
            kvp.Value.PlayerObject.gameObject.transform.position = startPos;
            
            var clientId = kvp.Key;
            
            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new [] { clientId } }
            };
            
            TeleportPlayersClientRpc(startPos, clientRpcParams);
        }
    }
    
    [ClientRpc]
    private void TeleportPlayersClientRpc(Vector3 startPos, ClientRpcParams rpcParams = default)
    {
        var player = NetworkManager.Singleton.LocalClient.PlayerObject;
        player.transform.position = startPos;
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

    private IEnumerator DayNightCycle()
    {
        return new WaitForSecondsRealtime(dayDuration);
        ServerStartVoting();
    }

    private void ServerStartVoting()
    {
        VoteManager.Singleton.StartVote();
    }
}
