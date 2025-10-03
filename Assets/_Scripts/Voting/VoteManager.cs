using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
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
    
    [SerializeField] private Dictionary<FixedString32Bytes, int> playerVotes = new();
    [SerializeField] private Dictionary<FixedString32Bytes, int> playerToButtonMap = new();
    private List<GameObject> votableButtons = new();
    private HashSet<ulong> votedClients = new();
    
    public static VoteManager Singleton;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
    }

    void Start()
    {
        // create as many buttons as there are players to the vote screen on client side
        int currentPlayer = 0;
        for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
        {
            
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        GameManager.Instance.OnGameStarted += InitializeVotes;
    }

    private void InitializeVotes()
    {
        playerVotes = new Dictionary<FixedString32Bytes, int>();
        
        foreach (var kvp in GameManager.Instance.Players)
        {
            playerVotes.Add(kvp.Value, 0);
            var player = NetworkManager.Singleton.ConnectedClients[kvp.Key].PlayerObject.GetComponent<PlayerState>();
            CreateVoteButton(kvp.Value, player.SpriteData);
        }
    }

    public void StartVote()
    {
        if (!IsServer) return;
        ToggleVoteScreenClientRpc();
        StartCoroutine(VotingCoroutine());
    }

    [Rpc(SendTo.Server)]
    private void ServerStartVoteRpc()
    {
        StartVote();
    }

    private IEnumerator VotingCoroutine()
    {
        yield return new WaitForSecondsRealtime(timeToVote);
        ServerStopVoteRpc();
    }
    
    [Rpc(SendTo.Server)]
    private void ServerStopVoteRpc()
    {
        // Handle stoping the vote
        // kill the most voted player
        ToggleVoteScreenClientRpc();
        var mostVotedPlayer = playerVotes.Values.Max();
        var mostVotedPlayerId = playerVotes.First(kvp => kvp.Value == mostVotedPlayer).Key;
        Debug.Log($"[{mostVotedPlayerId}] is out");
        // Kill most voted player
    }

    [ClientRpc]
    private void ToggleVoteScreenClientRpc(ClientRpcParams rpcParams = default)
    {
        voteScreen.SetActive(!voteScreen.activeSelf);
    }

    private void CreateVoteButton(FixedString32Bytes playerName, SpriteRenderer playerSprite)
    {
        if (!IsClient) return;
        
        Debug.Log($"trying to parent to {voteScreen.transform.GetChild(0)}");
        GameObject votableButton = Instantiate(votablePrefab, voteScreen.transform.GetChild(0));
        var button = votableButton.GetComponentInChildren<Button>();
        var image = votableButton.transform.GetChild(0).GetComponentInChildren<Image>();
        var text = votableButton.GetComponentInChildren<TextMeshProUGUI>();
        
        var pre = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<RoleComponent>();
        var playerRole = CamperRole.Camper;
        if (pre != null) 
        {
            playerRole = pre.Role;
        }
        
        button.onClick.AddListener(() => OnVotableButtonClicked(playerName));
        image.sprite = playerSprite.sprite;
        image.color = playerSprite.color;
        text.text = playerName.ToString();
        text.color = (playerRole == CamperRole.Killer) ? Color.red : Color.gray;
        
        votableButtons.Add(votableButton);
        playerToButtonMap.Add(playerName, votableButtons.FindIndex(t => votableButton));
        Debug.Log($"Created vote button for {playerName} connected to index: {votableButtons.FindIndex(t => votableButton)}");
    }

    private void OnVotableButtonClicked(FixedString32Bytes votedPlayer)
    {
        ServerVoteRpc(votedPlayer);
    }

    [Rpc(SendTo.Server)]
    private void ServerVoteRpc(FixedString32Bytes votedPlayerId)
    {
        var clientId = NetworkManager.Singleton.LocalClientId;
        if (votedClients.Contains(clientId)) return;
        
        votedClients.Add(clientId);
        playerVotes[votedPlayerId]++;
        UpdateVoteClientRpc(votedPlayerId, NetworkManager.Singleton.LocalClientId);
    }

    [ClientRpc]
    private void UpdateVoteClientRpc(FixedString32Bytes votedPlayerId, ulong voteeNetworkId, ClientRpcParams rpcParams = default)
    {
        var votedPlayerIndex = playerToButtonMap[votedPlayerId];
        var votedPlayer = votableButtons[votedPlayerIndex];
        var votee = Instantiate(voteePrefab, votedPlayer.transform.GetChild(2));
        
        var voteePlayer = NetworkManager.Singleton.ConnectedClients[voteeNetworkId].PlayerObject;
        var voteeSprite = voteePlayer.GetComponent<PlayerState>().SpriteData;
        
        var sprite = votee.GetComponentInChildren<Image>();
        sprite.sprite = voteeSprite.sprite;
        sprite.color = voteeSprite.color;
    }
}
