using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
    [SerializeField] private TMP_Text fireText;

    [SerializeField] 
    private GameObject winScreen;
    [SerializeField] 
    private GameObject loseScreen;
    public void Win() => WinGame();

    [SerializeField] private GameObject playerStartsTransform;

    public NetworkVariable<int> gameElapsedTime = new(0);
    [SerializeField] private int totalRounds = 3;
    private NetworkVariable<int> roundsplayed = new(0);
    public NetworkVariable<float> fireFumes = new(0);
    
    [SerializeField] private float fireStartingValue = 250f;
    [SerializeField] private float fireMaxValue = 350f;
    [SerializeField] private float fireDecreaseValue = 5f;

    [SerializeField] private Image buttonBackground;

    private Dictionary<ulong, Vector3> playerStartPositions = new();
    private List<Transform> startingTransforms = new();
    private NetworkList<bool> playersReady = new();
    private bool canStartGame = false;
    private bool rolesAssigned = false;
    private bool killersWon = false;
    
    private FixedString32Bytes[] playerNames = {
        "Frank", "Maya", "Mikael", "Benjamin", "Trond Olav", "Halldór", 
        "Inga", "Hilmir", "Steven", "Ivar", "Einar", "Marcela", "Lara", 
        "Nico", "Valdi", "Rares", "Emma", "David", "Andreas", "Gabriel", 
        "Ari", "Víctor", "Helga", "Adam", "Chris", "TO"};
    private HashSet<FixedString32Bytes> assignedNames = new();

    public Action OnGameStarted;

    public static GameManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (int i = 0; i < playerStartsTransform.transform.childCount; i++)
        {
            startingTransforms.Add(playerStartsTransform.transform.GetChild(i).gameObject.transform);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log($"On Network Spawn");
            playersDict = new SerializedDictionary<ulong, FixedString32Bytes>();
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectedCallback;
        }

        gameElapsedTime.OnValueChanged += UpdateDayHUDClientRpc;
        fireFumes.OnValueChanged += UpdateFireValueClientRpc;

        if (IsClient)
        {
            playersReady.OnListChanged += PlayersReadyListChanged;
        }

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectedCallback;
        }
        
        gameElapsedTime.OnValueChanged -= UpdateDayHUDClientRpc;
        fireFumes.OnValueChanged -= UpdateFireValueClientRpc;
        
        base.OnNetworkDespawn();
    }
    
    private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        if(IsServer)
        {
            Debug.Log($"Client Connected {obj} ");
            var newPlayer = NetworkManager.Singleton.ConnectedClients[obj].PlayerObject.GetComponent<PlayerState>();
            
            var availableNames = 
                playerNames.Where(name => !assignedNames.Contains(name)).ToArray();
            var playerName = availableNames[Random.Range(0, availableNames.Length - 1)];
            assignedNames.Add(playerName);
            
            PlayerData playerData = newPlayer.playerData.Value;
            playerData.Name = playerName;
            newPlayer.playerData.Value = playerData;
            
            Debug.Log($"Setting name for client {obj}: {playerName} ");
            playersDict.TryAdd(obj, playerName);
            UpdateList();
            playersReady.Add(false);
        }
    }

    private void Singleton_OnClientDisconnectedCallback(ulong obj)
    {
        if(IsServer)
        {
            Debug.Log($"Client Disconnected {obj} ");
            assignedNames.Remove(playersDict[obj]);
            if (playersDict.ContainsKey(obj)) playersDict.Remove(obj);
            playersReady.Remove(false);
            UpdateList();
        }
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

    private void UpdateList()
    {
        players.Clear();
        foreach (var kv in playersDict)
        {
            players.Add(kv.Value);
        }
        players.SetDirty(true);
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
        if (RoleManager.Instance != null && RoleManager.Instance.PlayerCount > 0 && !rolesAssigned)
        {
            RoleManager.Instance.AssignRoles();
            rolesAssigned = true;
        }

        roundsplayed.Value = 0;
        fireFumes.Value = fireStartingValue;
        
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
        
        ToggleReadyButtonClientRpc();

        for (int i = 0; i < playersReady.Count; i++)
        {
            playersReady[i] = false;
        }

        foreach (var kvp in playersDict)
        {
            NetworkObject playerObj = null;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(kvp.Key, out var client))
            {
                playerObj = client.PlayerObject;
            }

            if (playerObj == null) continue;
            var player = playerObj.GetComponent<PlayerState>();
            if (player.playerData.Value.Status == PlayerStatus.Dead)
            {
                Debug.Log($"Setting player {kvp.Key} to alive");
                AlivePlayerClientRpc(playerObj);
            }
        }

        ToggleReadyButtonClientRpc();
        
        if (killersWon) ToggleLoseScreenClientRpc();
        else ToggleWinScreenClientRpc();
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

    public void RestartDayNightCycle()
    {
        if (!IsServer) return;
        ServerStartDayNightCycleRpc();
    }
    
    [Rpc(SendTo.Server)]
    private void ServerStartDayNightCycleRpc()
    {
        gameElapsedTime.Value = 0;
        bool finishGame = roundsplayed.Value == totalRounds;
        if (finishGame)
        {
            Debug.Log("Finishing Game killers win");
            WinGame();
            return;
        }
        
        roundsplayed.Value++;
        fireDecreaseValue *= 2f;
        ToggleDayHUDClientRpc();
        StartCoroutine(DayNightCycle());
    }
    
    private IEnumerator DayNightCycle()
    {
        Debug.Log("Starting Day Night Cycle");
        while (gameElapsedTime.Value < dayDuration)
        {
            yield return new WaitForSecondsRealtime(1);
            gameElapsedTime.Value++;
            ServerUpdateFireValueRpc(-fireDecreaseValue);
        }
        ServerStartVotingRpc();
    }

    [ClientRpc]
    private void UpdateDayHUDClientRpc(int oldValue, int newValue)
    {
        dayText.text = $"Time until vote {dayDuration - newValue}s";
        roundsText.text = $"Round: {roundsplayed.Value}";
    }

    [ClientRpc]
    private void ToggleDayHUDClientRpc()
    {
        Debug.Log($"Toggle Day HUD {dayHUD.gameObject.activeSelf}");
        dayHUD.SetActive(!dayHUD.activeSelf);
        UpdateDayHUDClientRpc(0, 0);
    }

    [Rpc(SendTo.Server)]
    public void ServerUpdateFireValueRpc(float value)
    {
        if (!IsServer) return;
        float oldValue = fireFumes.Value;
        float newValue = MathF.Max(oldValue + value, fireMaxValue);
        fireFumes.Value = newValue;
    }

    [ClientRpc]
    private void UpdateFireValueClientRpc(float oldValue, float newValue)
    {
        fireText.text = $"Fire: {newValue}";
    }

    [ClientRpc]
    private void ToggleFireHUDClientRpc()
    {
        fireText.transform.parent.gameObject.SetActive(!fireText.gameObject.activeSelf);
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
            
            var player = playerObj.GetComponent<PlayerState>();
            if (player.playerData.Value.Role == CamperRole.Killer) wasKiller = true;
            
            if (playerObj != null)
            {
                PlayerData playerData = player.playerData.Value;
                playerData.Status = PlayerStatus.Dead;
                player.playerData.Value = playerData;
                KillPlayerClientRpc(playerObj);
            }
        }
        
        playersDict.Remove(clientToKill);
        UpdateList();

        if (wasKiller)
        {
            WinGame(true);
        }
    }
    
    [ClientRpc]
    private void KillPlayerClientRpc(NetworkObjectReference playerObjRef, ClientRpcParams rpcParams = default)
    {
        if (playerObjRef.TryGet(out var playerObj))
        {
            playerObj.gameObject.layer = LayerMask.NameToLayer("Spectate");
            
            var spriteRenderer = playerObj.GetComponent<SpriteRenderer>();
            Color deadColor = spriteRenderer.color;
            deadColor.a = 0.05f;
            spriteRenderer.color = deadColor;

            var headSprite = playerObj.transform.GetChild(1).GetComponent<SpriteRenderer>();
            deadColor.a = 0.2f;
            headSprite.color = deadColor;
            
            var light = playerObj.transform.GetChild(3).GetComponent<Light2D>();
            light.gameObject.SetActive(false);
            
            if (playerObj.OwnerClientId == NetworkManager.Singleton.LocalClientId)
            {
                light.gameObject.SetActive(true);
                light.falloffIntensity = 80;
            }
        }
    }
    
    [ClientRpc]
    private void AlivePlayerClientRpc(NetworkObjectReference playerObjRef, ClientRpcParams rpcParams = default)
    {
        if (playerObjRef.TryGet(out var playerObj))
        {
            playerObj.gameObject.layer = LayerMask.NameToLayer("Default");
            
            var spriteRenderer = playerObj.GetComponent<SpriteRenderer>();
            Color aliveColor = spriteRenderer.color;
            aliveColor.a = 1;
            spriteRenderer.color = aliveColor;

            var headSprite = playerObj.transform.GetChild(1).GetComponent<SpriteRenderer>();
            headSprite.color = aliveColor;
            
            var light = playerObj.transform.GetChild(3).GetComponent<Light2D>();
            light.gameObject.SetActive(true);
            light.falloffIntensity = .5f;
        }
    }

    private void WinGame(bool killerVotedOut = false)
    {
        Debug.Log("Win Game");
        if (!IsServer) return;

        bool skipCheck = killerVotedOut;
        if (!skipCheck)
        {
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                var playerObj = kvp.Value.PlayerObject;
                if (playerObj == null) continue;

                var player = playerObj.GetComponent<PlayerState>();
                if (player == null) continue;

                if (player.playerData.Value.Role == CamperRole.Killer &&
                    player.playerData.Value.Status != PlayerStatus.Dead)
                {
                    killersWon = true;
                    Debug.Log("[WinGame] killers won");
                    ToggleLoseScreenClientRpc();
                    break;
                }
            }
        }
        killersWon = false;
        ToggleWinScreenClientRpc();
    }

    [ClientRpc]
    private void ToggleWinScreenClientRpc()
    {
        winScreen.SetActive(!winScreen.activeSelf);
        Debug.Log($"Toggle Win Screen {winScreen.gameObject.activeSelf}");
    }
    
    [ClientRpc]
    private void ToggleLoseScreenClientRpc()
    {
        loseScreen.SetActive(!loseScreen.activeSelf);
        Debug.Log($"Toggle Lose Screen {loseScreen.gameObject.activeSelf}");
    }
}