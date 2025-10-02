using Unity.Netcode;
using UnityEngine;

public class GameStartManager : NetworkBehaviour
{
    private bool rolesAssigned = false;

    private void Update()
    {
        // Only the server should assign roles
        if (!IsServer || rolesAssigned) return;

        // Make sure players are registered
        if (RoleManager.Instance != null && RoleManager.Instance.PlayerCount > 0)
        {
            RoleManager.Instance.AssignRoles();
            rolesAssigned = true; // prevent assigning roles multiple times
        }
    }
}
