using Unity.Netcode;
using UnityEngine;
using TMPro;

// public enum PlayerRole
// {
//     Camper,
//     Killer
// }

public class PlayerRoleHandler : NetworkBehaviour
{
    private NetworkVariable<PlayerRole> role = new NetworkVariable<PlayerRole>(
        PlayerRole.Camper,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public PlayerRole Role => role.Value;

    [SerializeField] private TextMeshProUGUI roleText; // assign in inspector

    public override void OnNetworkSpawn()
    {
        if (IsServer && RoleManager.Instance != null)
        {
            RoleManager.Instance.RegisterPlayer(this);
        }

        role.OnValueChanged += OnRoleChanged;

        // Update UI immediately
        OnRoleChanged(role.Value, role.Value);
    }

    private void OnRoleChanged(PlayerRole previous, PlayerRole current)
    {
        // Only show role for the local player
        if (IsOwner && roleText != null)
        {
            roleText.text = $"Role: {current}";
            roleText.color = (current == PlayerRole.Killer) ? Color.red : Color.green;
        }

        // Optional: change player color
        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = (current == PlayerRole.Killer) ? Color.red : Color.green;
        }
    }

    public void SetRole(PlayerRole newRole)
    {
        if (IsServer)
        {
            role.Value = newRole;
        }
    }
}
