using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private SerializedDictionary<ulong, FixedString32Bytes> playersDict;
    public Dictionary<ulong, FixedString32Bytes> Players => playersDict;

    private NetworkList<FixedString32Bytes> players = new();

    [SerializeField] private TMP_Text dayText;
    [SerializeField] private float dayDuration = 90f;
    [SerializeField] private GameObject dayHUD;
    
    [SerializeField] private TMP_Text roundsText;
    [SerializeField] private GameObject roundsHUD;

    [SerializeField] private GameObject winScreen;
    public void Win() => WinGame();
    [SerializeField] private GameObject loseScreen;
    public void Lose() => LoseGame();

    [SerializeField] private GameObject playerStartTransform;

    public NetworkVariable<int> gameElapsedTime = new(0);

    [SerializeField] private Image buttonBackground;

    private Dictionary<ulong, Vector3> playerStartPositions = new();
    private List<Transform> startingTransforms = new();
    private NetworkList<bool> playersReady = new();
    private bool canStartGame = false;
    private bool rolesAssigned = false;

    [SerializeField] private int totalRounds = 3;
    private NetworkVariable<int> roundsplayed = new(0);

    public Action OnGameStarted;

    public static GameManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (int i = 0; i < playerStartTransform.transform.childCount; i++)
        {
            startingTransforms.Add(playerStartTransform.transform.GetChild(i).gameObject.transform);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log($"On Network Spawn");
            playersDict = new SerializedDictionary<ulong, FixedString32Bytes>();
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        }

        gameElapsedTime.OnValueChanged += UpdateDayHUDClientRpc;

        if (IsClient)
        {
            playersReady.OnListChanged += PlayersReadyListChanged;
        }

        base.OnNetworkSpawn();
    }

    private void PlayersReadyListChanged(NetworkListEvent<bool> changeEvent)
    {
        if (NetworkManager.Singleton.LocalClientId == (ulong)changeEvent.Index)
        {
            if(changeEvent.Value)
            {
                buttonBackground.color = Color.green;
            }
            else
            {
                buttonBackground.color = Color.white;
            }
        }
    }

private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        if(IsServer)
        {
            Debug.Log($"Client Connected {obj} ");
            var playerData = NetworkManager.Singleton.ConnectedClients[obj].PlayerObject.GetComponent<PlayerState>();
            var playerName = (!playerData.PlayerData.Name.IsEmpty) ? playerData.PlayerData.Name : new FixedString32Bytes("Player" + playersDict.Count + 1);
            
            Debug.Log($"Setting name for client {obj}: {playerName} ");
            playersDict.TryAdd(obj, playerName);
            UpdateList();
            playersReady.Add(false);
        }
    }

    private void TryStartGame()
    {
        if (canStartGame) return;
        
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
        if (RoleManager.Instance != null && RoleManager.Instance.PlayerCount > 0)
        {
            RoleManager.Instance.AssignRoles();
            rolesAssigned = true;
        }

        roundsplayed.Value = 0;
        
        ToggleReadyButtonClientRpc();
        ServerTeleportPlayersRpc();
        ServerStartDayNightCycleRpc();
        
        OnGameStarted?.Invoke();
    }

    public void RestartGame()
    {
        if (!IsServer) return;

        gameElapsedTime.Value = 0;
        canStartGame = false;
        rolesAssigned = false;

        for (int i = 0; i < playersReady.Count; i++)
        {
            playersReady[i] = false;
        }

        ToggleReadyButtonClientRpc();
    }

    [ClientRpc]
    private void ToggleReadyButtonClientRpc()
    {
        buttonBackground.gameObject.SetActive(!buttonBackground.gameObject.activeSelf);
    }

    [Rpc(SendTo.Server)]
    private void ServerTeleportPlayersRpc()
    {
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            var clientId = kvp.Key;
            Vector3 startPos;

            if (!playerStartPositions.TryGetValue(clientId, out startPos))
            {
                startPos = startingTransforms[Random.Range(0, startingTransforms.Count - 1)].position;
                playerStartPositions[clientId] = startPos;
            }
            
            kvp.Value.PlayerObject.gameObject.transform.position = startPos;
            
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
        //UpdateReadyButtonClientRpc();
        TryStartGame();
    }

    public void SetPlayerReady()
    {
        if(IsClient)
        {
            SetPlayerReadyRpc();
        }
    }

    public void RestartDayNightCycle()
    {
        if (!IsServer) return;
        ServerStartDayNightCycleRpc();
    }

    private IEnumerator DayNightCycle()
    {
        while (gameElapsedTime.Value < dayDuration)
        {
            yield return new WaitForSecondsRealtime(1);
            gameElapsedTime.Value++;
        }
        ServerStartVotingRpc();
    }

    [ClientRpc]
    private void UpdateDayHUDClientRpc(int oldValue, int newValue)
    {
        dayText.text = $"Time until vote {dayDuration - newValue}s";
        roundsText.text = $"Round: {roundsplayed.Value}";
    }

    [Rpc(SendTo.Server)]
    private void ServerStartDayNightCycleRpc()
    {
        gameElapsedTime.Value = 0;
        bool finishGame = roundsplayed.Value == totalRounds;
        if (finishGame)
        {
            LoseGame();
            return;
        }
        
        roundsplayed.Value++;
        ToggleDayHUDClientRpc();
        StartCoroutine(DayNightCycle());
    }

    [ClientRpc]
    private void ToggleDayHUDClientRpc()
    {
        dayHUD.SetActive(!dayHUD.activeSelf);
        UpdateDayHUDClientRpc(0, 0);
    }
    
    private void ServerStartVotingRpc()
    {
        if (!IsServer) return;
        ToggleDayHUDClientRpc();
        ServerTeleportPlayersRpc();
        VoteManager.Singleton.StartVote();
    }

    public void Kill(FixedString32Bytes playerName)
    {
        if (!IsServer) return;

        ulong clientToKill = 0;
        bool found = false;

        foreach (var kvp in playersDict)
        {
            if (kvp.Value == playerName)
            {
                clientToKill = kvp.Key;
                found = true;
                break;
            }
        }
        
        if (!found) return;
        
        bool wasKiller = false;
        VoteManager.Singleton.DisablePlayerButton(playerName);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientToKill, out var client))
        { 
            var playerObj = client.PlayerObject;
            
            var roleComponent = playerObj.GetComponent<RoleComponent>();
            if (roleComponent.Role == CamperRole.Killer) wasKiller = true;
                
            NetworkManager.Singleton.DisconnectClient(clientToKill);
            
            if (playerObj != null)
            {
                playerObj.GetComponent<NetworkObject>().Despawn();
                Destroy(playerObj.gameObject);
            }
        }
        
        playersDict.Remove(clientToKill);
        UpdateList();

        if (wasKiller)
        {
            WinGame();
        }
    }

    private void WinGame()
    {
        if (!IsServer) return;

        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObj = kvp.Value.PlayerObject;
            if (playerObj == null) continue;

            var roleComponent = playerObj.GetComponent<RoleComponent>();
            if (roleComponent == null) continue;
            
            if (roleComponent.Role == CamperRole.Killer) ToggleLoseScreenClientRpc();
            else ToggleWinScreenClientRpc();
        }
    }

    [ClientRpc]
    private void ToggleWinScreenClientRpc()
    {
        winScreen.SetActive(true);
    }
    
    private void LoseGame()
    {
        if (!IsServer) return;
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObj = kvp.Value.PlayerObject;
            if (playerObj == null) continue;

            var roleComponent = playerObj.GetComponent<RoleComponent>();
            if (roleComponent == null) continue;
            
            if (roleComponent.Role == CamperRole.Killer) ToggleWinScreenClientRpc();
            else ToggleLoseScreenClientRpc();
        }
    }
    
    [ClientRpc]
    private void ToggleLoseScreenClientRpc()
    {
        loseScreen.SetActive(true);
    }
}