using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class VoteManager : NetworkBehaviour
{
    [SerializeField] 
    private float timeToVote = 60f;

    [SerializeField] 
    private GameObject voteScreen;
    
    [SerializeField] 
    private GameObject votablePrefab;
    
    [SerializeField] 
    private GameObject voteePrefab;
    
    [SerializeField] private SerializedDictionary<FixedString32Bytes, int> playerVotes = new();
    [SerializeField] private SerializedDictionary<FixedString32Bytes, int> playerToButtonMap = new();
    private List<GameObject> votableButtons = new();
    private NetworkList<ulong> votedClients = new();
    private NetworkList<FixedString32Bytes> eliminatedPlayers = new();
    
    public static VoteManager Singleton;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        GameManager.Instance.OnGameStarted += InitializeVotes;
    }

    private void InitializeVotes()
    {
        if (!IsServer) return;
        
        playerVotes.Clear();
        votedClients.Clear();
        eliminatedPlayers.Clear();
        
        foreach (var kvp in GameManager.Instance.Players)
        {
            Debug.Log($"Adding vote for {kvp.Value}");
            playerVotes.Add(kvp.Value, 0);
        }
        playerVotes.Add(new FixedString32Bytes("Skip"), 0);

        CreateVoteButtonsClientRpc();
    }

    public void StartVote()
    {
        if (!IsServer) return;
        ToggleVoteScreenClientRpc(true);
        StartCoroutine(VotingCoroutine());
    }

    private IEnumerator VotingCoroutine()
    {
        
        GameManager.Instance.gameElapsedTime.Value = 0;
        
        int elapsedTime = 0;
        while (elapsedTime < timeToVote)
        {
            yield return new WaitForSecondsRealtime(1f);
            elapsedTime++;
            GameManager.Instance.gameElapsedTime.Value = elapsedTime;
        }
        ServerStopVoteRpc();
    }
    
    [Rpc(SendTo.Server)]
    private void ServerStopVoteRpc()
    {
        bool killPlayer = true;
        int highestVote = playerVotes.Values.Max();
        
        var mostVotedPlayers = playerVotes.Where(
            votedPlayer => votedPlayer.Value == highestVote)
            .Select(votedPlayer => votedPlayer.Key).ToList();

        if (mostVotedPlayers.Count > 1)
        {
            Debug.Log("The mostvotedPlayers is more than 1");
            killPlayer = false;
        }
        
        if (highestVote == 0)
        {
            Debug.Log("highest vote is 0 returning early");
            killPlayer = false;
        }

        if (killPlayer)
        {
            var mostVotedPlayer = mostVotedPlayers.First();
            Debug.Log("most voted player is " + mostVotedPlayer);
            
            eliminatedPlayers.Add(mostVotedPlayer);

            if (mostVotedPlayer != string.Empty)
            {
                Debug.Log($"removing player {mostVotedPlayer}");
                GameManager.Instance.Kill(mostVotedPlayer);
            }
        }
        ResetVotes();
        VoteTimeUnsubscribtionClientRpc();
        ToggleVoteScreenClientRpc(false);
        GameManager.Instance.RestartDayNightCycle();
    }

    [ClientRpc]
    public void ToggleVoteScreenClientRpc(bool toggle, ClientRpcParams rpcParams = default)
    {
        voteScreen.SetActive(toggle);
        VoteTimeSubscribtionClientRpc();
    }

    [ClientRpc]
    private void VoteTimeSubscribtionClientRpc(ClientRpcParams rpcParams = default)
    {
        GameManager.Instance.gameElapsedTime.OnValueChanged += UpdateVoteTimeUI;
    }
    
    [ClientRpc]
    private void VoteTimeUnsubscribtionClientRpc(ClientRpcParams rpcParams = default)
    {
        GameManager.Instance.gameElapsedTime.OnValueChanged -= UpdateVoteTimeUI;
    }

    private void UpdateVoteTimeUI(int oldValue, int newValue)
    {
        var voteTime = voteScreen.transform.GetChild(2).GetComponentInChildren<TextMeshProUGUI>();
        voteTime.text = $"Time left to vote: {timeToVote - GameManager.Instance.gameElapsedTime.Value}s";
    }
    
    [ClientRpc]
    private void CreateVoteButtonsClientRpc(ClientRpcParams rpcParams = default)
    {
        foreach (var button in votableButtons)
        {
            if (button != null) Destroy(button);
        }
        
        votableButtons.Clear();
        playerToButtonMap.Clear();
        
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        
        var localPlayerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        var localplayer = localPlayerObj.GetComponent<PlayerState>();
        
        bool isKiller = localplayer.playerData != null && localplayer.playerData.Value.Role == CamperRole.Killer;

        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            ulong playerNetworkId = kvp.Key;

            var playerObj = kvp.Value.PlayerObject;
            if (playerObj == null) continue;
        
            var player = playerObj.GetComponent<PlayerState>();
            if (player == null) continue;
            
            var playerSprite = player.SpriteData;
            FixedString32Bytes playerName = player.playerData.Value.Name;
            
            GameObject votableButton = Instantiate(votablePrefab, voteScreen.transform.GetChild(0));
            var button = votableButton.GetComponentInChildren<Button>();
            var image = votableButton.transform.GetChild(0).GetComponentInChildren<Image>();
            var text = votableButton.GetComponentInChildren<TextMeshProUGUI>();
            
            var playerRole = player.playerData != null ? player.playerData.Value.Role : CamperRole.Camper;
            
            image.sprite = playerSprite.sprite;
            image.color = playerSprite.color;
            text.text = playerName.ToString();
            text.color = (isKiller && playerRole == CamperRole.Killer) 
                ? Color.red : Color.gray;

            if (playerNetworkId != localClientId)
            {
                button.onClick.AddListener(() => OnVotableButtonClicked(playerName, localClientId));
            }
            else
            {
                button.interactable = false;
            }
            
            int buttonId = votableButtons.Count;
            votableButtons.Add(votableButton);
            playerToButtonMap.Add(playerName, buttonId);

            if (eliminatedPlayers.Contains(playerName))
            {
                button.interactable = false;
                image.color = Color.gray;
                votableButton.transform.localScale *= 0.9f;

                var cross = image.transform.GetChild(1);
                cross.gameObject.SetActive(true);
            }
        }
    }

    private void OnVotableButtonClicked(FixedString32Bytes votedPlayer, ulong voteeClientId)
    {
        Debug.Log($"Voting for {votedPlayer}");
        ServerVoteRpc(votedPlayer, voteeClientId);
    }

    [Rpc(SendTo.Server)]
    private void ServerVoteRpc(FixedString32Bytes votedPlayerId, ulong voteeClientId, RpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        if (senderId != voteeClientId)
        {
            Debug.Log($"Client {senderId} tried to vote for {votedPlayerId} but was not the votee");
            return;
        }
        
        if (votedClients.Contains(voteeClientId)) return;
        
        if (eliminatedPlayers.Contains(votedPlayerId)) return;
        
        votedClients.Add(voteeClientId);
        playerVotes[votedPlayerId]++;
        Debug.Log($"Client {voteeClientId} voted for {votedPlayerId}:Client {GameManager.Instance.Players.Where(t => t.Value == votedPlayerId)}. Total votes: {playerVotes[votedPlayerId]}");
        UpdateVoteClientRpc(votedPlayerId, voteeClientId);
    }

    [ClientRpc]
    private void UpdateVoteClientRpc(FixedString32Bytes votedPlayerId, ulong voteeNetworkId, ClientRpcParams rpcParams = default)
    {
        if (!playerToButtonMap.ContainsKey(votedPlayerId))
        {
            return;
        }
        
        var votedPlayerIndex = playerToButtonMap[votedPlayerId];
        var votedPlayer = votableButtons[votedPlayerIndex];
        
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(voteeNetworkId)) return;
        
        var voteePlayer = NetworkManager.Singleton.ConnectedClients[voteeNetworkId].PlayerObject;
        if (voteePlayer == null) return;
        
        Debug.Log($"Adding vote for {votedPlayerId} by {voteePlayer.name}");
        
        var voteeSprite = voteePlayer.GetComponent<PlayerState>().SpriteData;
        if (voteeSprite == null) return;
        
        var votee = Instantiate(voteePrefab, votedPlayer.transform.GetChild(2));
        
        var sprite = votee.GetComponentInChildren<Image>();
        sprite.sprite = voteeSprite.sprite;
        sprite.color = voteeSprite.color;
    }
    
    private void ResetVotes()
    {
        votedClients.Clear();

        foreach (var key in playerVotes.Keys.ToList())
        {
            playerVotes[key] = 0;
        }

        ClearVoteesClientRpc();
    }

    [ClientRpc]
    private void ClearVoteesClientRpc(ClientRpcParams rpcParams = default)
    {
        foreach (var button in votableButtons)
        {
            if (button == null) continue;

            var voteeButton = button.transform.GetChild(2);
            foreach (Transform child in voteeButton)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    public void DisablePlayerButton(FixedString32Bytes playerName)
    {
        if (!IsServer) return;
        
        eliminatedPlayers.Add(playerName);
        DisablePlayerButtonClientRpc(playerName);
    }

    [ClientRpc]
    private void DisablePlayerButtonClientRpc(FixedString32Bytes playerName, ClientRpcParams rpcParams = default)
    {
        if (!playerToButtonMap.ContainsKey(playerName))
        {
            return;
        }
        
        int buttonId = playerToButtonMap[playerName];
        var votableButton = votableButtons[buttonId];
        if (votableButton == null) return;
        
        var button = votableButton.GetComponent<Button>();
        if (button == null) return;
        
        button.interactable = false;
        button.onClick.RemoveAllListeners();
        
        var image = votableButton.transform.GetChild(0).GetComponentInChildren<Image>();
        if (image == null) return;
        
        image.color = Color.gray;

        votableButton.transform.localScale *= 0.9f;
        
        var cross = image.transform.GetChild(1);
        if (cross == null) return;
        
        cross.gameObject.SetActive(true);
    }
    
    private void SkipVote()
    {
        var skipVoteButton = Instantiate(votablePrefab);
        var skipButton = skipVoteButton.GetComponent<Button>();
        if (skipButton == null) return;
        
        skipButton.onClick.AddListener(() => ServerVoteRpc(new FixedString32Bytes("Skip"), NetworkManager.Singleton.LocalClientId));
    }
}
