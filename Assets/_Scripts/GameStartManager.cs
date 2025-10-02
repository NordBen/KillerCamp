using Unity.Netcode;
using UnityEngine;

public class GameStartManager : NetworkBehaviour
{
    private bool rolesAssigned = false;

    private void Update()
    {
        if (!IsServer || rolesAssigned) return;

        if (RoleManager.Instance != null && RoleManager.Instance.PlayerCount > 0)
        {
            RoleManager.Instance.AssignRoles();
            rolesAssigned = true;
        }
    }
}
