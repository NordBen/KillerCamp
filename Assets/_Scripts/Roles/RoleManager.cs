using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoleManager : NetworkBehaviour
{
    public static RoleManager Instance { get; private set; }

    private List<RoleComponent> players = new();

    public int PlayerCount => players.Count;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void RegisterPlayer(RoleComponent player)
    {
        if (IsServer && !players.Contains(player))
        {
            players.Add(player);
        }
    }

    public void AssignRoles()
    {
        if (!IsServer || players.Count == 0) return;

        // Pick exactly one random index to be the Killer
        int killerIndex = Random.Range(0, players.Count);

        for (int i = 0; i < players.Count; i++)
        {
            if (i == killerIndex)
            {
                players[i].SetRole(CamperRole.Killer);
                continue;
            }

            players[i].SetRole(CamperRole.Camper);
        }

        Debug.Log($"Player {killerIndex} is the Killer, rest are Campers.");
    }
}
