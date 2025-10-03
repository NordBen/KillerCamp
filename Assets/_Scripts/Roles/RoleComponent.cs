using Unity.Netcode;
using UnityEngine;
using TMPro;

public class RoleComponent : NetworkBehaviour
{
    private NetworkVariable<CamperRole> role = new(CamperRole.Camper);
    
    public CamperRole Role => role.Value;

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

    private void OnRoleChanged(CamperRole previous, CamperRole current)
    {
        if (IsOwner && roleText != null)
        {
            roleText.text = $"Role: {current}";
            roleText.color = (current == CamperRole.Killer) ? Color.red : Color.green;
        }

        // // Optional: change player color
        // var rend = GetComponent<Renderer>();
        // if (rend != null)
        // {
        //     rend.material.color = (current == PlayerRole.Killer) ? Color.red : Color.green;
        // }
    }

    public void SetRole(CamperRole newRole)
    {
        if (IsServer)
        {
            role.Value = newRole;
        }
    }
}
