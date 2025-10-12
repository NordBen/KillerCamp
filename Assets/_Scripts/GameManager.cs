using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private Dictionary<ulong, FixedString32Bytes> playersDict;
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
    public void Win(bool campersWin = false) => WinGame(campersWin);

    [SerializeField] private GameObject playerStartsTransform;

    public NetworkVariable<int> gameElapsedTime = new(0);
    [SerializeField] private int totalRounds = 6;
    private NetworkVariable<int> roundsplayed = new(0);
    public NetworkVariable<float> fireFumes = new(0);
    
    [SerializeField] private float fireStartingValue = 600f;
    [SerializeField] private float fireMaxValue = 900f;
    [SerializeField] private float fireDecreaseValue = 1.25f;

    [SerializeField] private Image buttonBackground;

    private Dictionary<ulong, Vector3> playerStartPositions = new();
    private List<Transform> startingTransforms = new();
    private NetworkList<bool> playersReady = new();
    private bool canStartGame = false;
    private bool rolesAssigned = false;
    private bool canTick = true;
    private bool gameActive = false;
    private bool eventsSubscribed = false;
    
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
            playersDict = new Dictionary<ulong, FixedString32Bytes>();
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectedCallback;
        }

        SubscribeGameEvents();

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
        
        UnsubscribeGameEvents();
        
        base.OnNetworkDespawn();
    }

    private void SubscribeGameEvents()
    {
        if (eventsSubscribed) return;
        gameElapsedTime.OnValueChanged += UpdateDayHUDClientRpc;
        fireFumes.OnValueChanged += UpdateFireValueClientRpc;
        eventsSubscribed = true;
    }

    private void UnsubscribeGameEvents()
    {
        if (!eventsSubscribed) return;
        gameElapsedTime.OnValueChanged -= UpdateDayHUDClientRpc;
        fireFumes.OnValueChanged -= UpdateFireValueClientRpc;
        eventsSubscribed = false;
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

        canTick = true;
        gameActive = true;
        gameElapsedTime.Value = 0;
        roundsplayed.Value = 0;
        fireFumes.Value = fireStartingValue;
        fireDecreaseValue = 1.25f;
        
        SubscribeGameEvents();
        
        ToggleReadyButtonClientRpc();
        ServerTeleportPlayersRpc();
        ServerStartDayNightCycleRpc();
        
        OnGameStarted?.Invoke();
    }

    public void RestartGame()
    {
        if (!IsServer) return;

        for (int i = 0; i < playersReady.Count; i++)
        {
            playersReady[i] = false;
        }

        Debug.Log("looping playersdict with length of " + playersDict.Count);
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            var clientId = kvp.Key;
            var playerObj = kvp.Value.PlayerObject;

            if (playerObj == null)
            {
                Debug.Log($"player {kvp.Key} not found");
                continue;
            }
            var player = playerObj.GetComponent<PlayerState>();
            if (player.playerData.Value.Status == PlayerStatus.Dead)
            {
                Debug.LogError($"Setting player {kvp.Key} to alive");
                PlayerData playerData = player.playerData.Value;
                playerData.Status = PlayerStatus.Alive;
                player.playerData.Value = playerData;
                
                AlivePlayerClientRpc(playerObj);
            }
        }
        
        UpdateList();
        ToggleReadyButtonClientRpc();
        ToggleLoseScreenClientRpc(false);
        ToggleWinScreenClientRpc(false);
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
        if (!gameActive) return;
        
        gameElapsedTime.Value = 0;
        if (roundsplayed.Value >= totalRounds)
        {
            WinGame(false);
            return;
        }
        
        roundsplayed.Value++;
        fireDecreaseValue *= 2f;
        ToggleDayHUDClientRpc(true);
        StartCoroutine(DayNightCycle());
    }
    
    private IEnumerator DayNightCycle()
    {
        Debug.Log("Starting Day Night Cycle");
        if (!canTick || !gameActive) yield break;
        while (gameElapsedTime.Value < dayDuration)
        {
            yield return new WaitForSecondsRealtime(1);
            if (!gameActive) yield break;
            gameElapsedTime.Value++;
            ServerUpdateFireValueRpc(-fireDecreaseValue);
        }
        if (gameActive) ServerStartVotingRpc();
    }

    [ClientRpc]
    private void UpdateDayHUDClientRpc(int oldValue, int newValue)
    {
        dayText.text = $"Time until vote {dayDuration - newValue}s";
        roundsText.text = $"Round: {roundsplayed.Value}";
    }

    [ClientRpc]
    private void ToggleDayHUDClientRpc(bool toggle)
    {
        Debug.Log($"Toggle Day HUD {toggle}");
        dayHUD.SetActive(toggle);
        UpdateDayHUDClientRpc(0, 0);
    }

    [Rpc(SendTo.Server)]
    public void ServerUpdateFireValueRpc(float value)
    {
        if (!IsServer || !gameActive) return;
        float oldValue = fireFumes.Value;
        float newValue = MathF.Min(oldValue + value, fireMaxValue);
        newValue = MathF.Max(newValue, 0);
        fireFumes.Value = newValue;
        if (newValue <= 0 && oldValue > 0)
        {
            WinGame(false);
        }
    }

    [ClientRpc]
    private void UpdateFireValueClientRpc(float oldValue, float newValue)
    {
        fireText.text = $"Fire: {newValue}";
    }
    
    private void ServerStartVotingRpc()
    {
        if (!IsServer) return;
        ToggleDayHUDClientRpc(false);
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
            Color deadHeadColor = headSprite.color;
            deadHeadColor.a = 0.2f;
            headSprite.color = deadHeadColor;
            
            var light = playerObj.transform.GetChild(3).GetComponent<Light2D>();
            light.gameObject.SetActive(false);
            
            if (playerObj.OwnerClientId == NetworkManager.Singleton.LocalClientId)
            {
                light.gameObject.SetActive(true);
                light.falloffIntensity = 800;
            }
        }
    }
    
    [ClientRpc]
    private void AlivePlayerClientRpc(NetworkObjectReference playerObjRef, ClientRpcParams rpcParams = default)
    {
        if (playerObjRef.TryGet(out var playerObj))
        {
            playerObj.gameObject.layer = LayerMask.NameToLayer("Player");
            
            var spriteRenderer = playerObj.GetComponent<SpriteRenderer>();
            Color aliveColor = spriteRenderer.color;
            aliveColor.a = 1;
            spriteRenderer.color = aliveColor;

            var headSprite = playerObj.transform.GetChild(1).GetComponent<SpriteRenderer>();
            Color aliveHeadColor = headSprite.color;
            aliveHeadColor.a = 1;
            headSprite.color = aliveHeadColor;
            
            var light = playerObj.transform.GetChild(3).GetComponent<Light2D>();
            light.gameObject.SetActive(true);
            light.falloffIntensity = 0.5f;
        }
    }

    private void WinGame(bool campersWin = false)
    {
        if (!IsServer || !gameActive) return;

        gameActive = false;
        canTick = false;
        canStartGame = false;
        rolesAssigned = false;

        StopAllCoroutines();
        UnsubscribeGameEvents();
        
        ToggleDayHUDClientRpc(false);
        VoteManager.Singleton.ToggleVoteScreenClientRpc(false);
        VoteManager.Singleton.StopAllCoroutines();

        if (campersWin) ToggleWinScreenClientRpc(true);
        else ToggleLoseScreenClientRpc(true);
    }

    [ClientRpc]
    private void ToggleWinScreenClientRpc(bool toggle)
    {
        winScreen.SetActive(toggle);
    }
    
    [ClientRpc]
    private void ToggleLoseScreenClientRpc(bool toggle)
    {
        loseScreen.SetActive(toggle);
    }
}