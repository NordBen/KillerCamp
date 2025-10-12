using TMPro;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class PlayerState : NetworkBehaviour
{
    //[SerializeField] private NetworkVariable<FixedString32Bytes> playerName;

    [SerializeField] 
    private TextMeshProUGUI nameTagText;
    
    [SerializeField] 
    private TextMeshProUGUI roleText;
    
    //public FixedString32Bytes PlayerName => playerName.Value;
    
    public NetworkVariable<PlayerData>  playerData = new();
    
    private NetworkVariable<Color> playerColor = new();
    
    private SpriteRenderer spriteRenderer;
    public SpriteRenderer SpriteData => spriteRenderer;

    public override void OnNetworkSpawn()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (IsServer)
        {
            playerColor.Value = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
            PlayerData initialData = new PlayerData(new FixedString32Bytes($"Player{OwnerClientId}"), CamperRole.Camper);
            playerData.Value = initialData;
            
            if (RoleManager.Instance != null) RoleManager.Instance.RegisterPlayer(this);
        }
        
        playerData.OnValueChanged += OnPlayerDataChanged;
        //playerName.OnValueChanged += OnPlayerNameChanged;
        playerColor.OnValueChanged += (oldColor, newColor) =>
        {
            ApplyColor(newColor);
        };

        OnPlayerDataChanged(default, playerData.Value);
        //OnPlayerNameChanged(default, playerName.Value);
        ApplyColor(playerColor.Value);
        
        base.OnNetworkSpawn();
    }

    private void OnPlayerDataChanged(PlayerData previousValue, PlayerData newValue)
    {
        if (IsOwner && roleText != null)
        {
            CamperRole role = newValue.Role;
            roleText.text = $"Role: {role}";
            roleText.color = (role == CamperRole.Killer) ? Color.red : Color.green;
        }
        
        if (nameTagText == null) return;
        nameTagText.text = newValue.Name.ToString();
        
        // // Optional: change player color
        // var rend = GetComponent<Renderer>();
        // if (rend != null)
        // {
        //     rend.material.color = (current == PlayerRole.Killer) ? Color.red : Color.green;
        // }
    }

    public override void OnNetworkDespawn()
    {
        //playerName.OnValueChanged -= OnPlayerNameChanged;
        playerData.OnValueChanged -= OnPlayerDataChanged;
        base.OnNetworkDespawn();
    }

    void ApplyColor(Color newColor)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = newColor;
    }
}

[System.Serializable]
public struct PlayerData : INetworkSerializable
{
    public FixedString32Bytes Name;
    public CamperRole Role;
    public PlayerStatus Status;
    public ulong CurrentTask;
    
    public PlayerData(FixedString32Bytes inName, CamperRole inRole)
    {
        Name = inName;
        Role = inRole;
        Status = PlayerStatus.Alive;
        CurrentTask = 0;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Role);
        serializer.SerializeValue(ref Status);
        serializer.SerializeValue(ref CurrentTask);
    }
}