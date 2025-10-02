using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerRoleHandler : NetworkBehaviour
{
    private NetworkVariable<PlayerRole> role = new NetworkVariable<PlayerRole>(
        PlayerRole.Camper, // default
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public PlayerRole Role => role.Value;

    [SerializeField] private TextMeshProUGUI roleText; // Assign in Inspector

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            role.Value = PlayerRole.Camper; // default
            FindObjectOfType<RoleManager>().RegisterPlayer(this);
        }

        role.OnValueChanged += OnRoleChanged;

        // Update UI immediately if role already set
        OnRoleChanged(role.Value, role.Value);
    }

    private void OnRoleChanged(PlayerRole previous, PlayerRole current)
    {
        Debug.Log($"{OwnerClientId} role is now {current}");

        // Change UI text
        if (IsOwner && roleText != null) // only show on local player's screen
        {
            roleText.text = $"Role: {current}";
            roleText.color = (current == PlayerRole.Killer) ? Color.red : Color.green;
        }

        // Example: also change body color
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
