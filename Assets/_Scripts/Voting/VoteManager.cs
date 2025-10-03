using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class VoteManager : NetworkBehaviour
{
    [SerializeField] private float timeToVote = 60f;

    [SerializeField] private Image voteScreen;
    
    private Dictionary<FixedString32Bytes, int> playerVotes = new();
    
    private List<Button> votableButtons = new();
    
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
        playerVotes = new Dictionary<FixedString32Bytes, int>();
        base.OnNetworkSpawn();
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
    }

    [ClientRpc]
    private void ToggleVoteScreenClientRpc(ClientRpcParams rpcParams = default)
    {
        // toggle vote screen for all clients
    }
}
