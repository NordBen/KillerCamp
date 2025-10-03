using Unity.Netcode;
using UnityEngine;
using TMPro;

public class RoleComponent : NetworkBehaviour
{
    private NetworkVariable<PlayerRole> role = new(PlayerRole.Camper);
    
    public PlayerRole Role => role.Value;

    [SerializeField] private TextMeshProUGUI roleText;

    public override void OnNetworkSpawn()
    {
        if (IsServer && RoleManager.Instance != null)
        {
            RoleManager.Instance.RegisterPlayer(this);
        }

        role.OnValueChanged += OnRoleChanged;
        
        OnRoleChanged(role.Value, role.Value);
    }

    private void OnRoleChanged(PlayerRole previous, PlayerRole current)
    {
        if (IsOwner && roleText != null)
        {
            roleText.text = $"Role: {current}";
            roleText.color = (current == PlayerRole.Killer) ? Color.red : Color.green;
        }

        // // Optional: change player color
        // var rend = GetComponent<Renderer>();
        // if (rend != null)
        // {
        //     rend.material.color = (current == PlayerRole.Killer) ? Color.red : Color.green;
        // }
    }

    public void SetRole(PlayerRole newRole)
    {
        if (IsServer)
        {
            role.Value = newRole;
        }
    }
}
