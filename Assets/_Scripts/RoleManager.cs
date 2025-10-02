using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoleManager : NetworkBehaviour
{
    private List<PlayerRoleHandler> players = new List<PlayerRoleHandler>();

    public void RegisterPlayer(PlayerRoleHandler player)
    {
        if (IsServer) // only the server should keep track of players
        {
            if (!players.Contains(player))
            {
                players.Add(player);
            }
        }
    }

    public void AssignRoles()
    {
        if (!IsServer) return; // safety check
        if (players.Count == 0) return;

        // Shuffle list randomly
        var shuffled = new List<PlayerRoleHandler>(players);
        shuffled.Sort((a, b) => Random.Range(-1, 2));

        // First player is Killer
        shuffled[0].SetRole(PlayerRole.Killer);

        // Rest are Campers
        for (int i = 1; i < shuffled.Count; i++)
        {
            shuffled[i].SetRole(PlayerRole.Camper);
        }

        Debug.Log("Roles assigned: 1 Killer, rest Campers.");
    }
}
