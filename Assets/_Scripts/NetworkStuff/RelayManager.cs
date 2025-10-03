using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class RelayManager : MonoBehaviour
{
    [SerializeField] private TMP_Text hostedCode;
    
    async void Start()
    {
        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log(AuthenticationService.Instance.AccessToken);

    }

    public async Task CreateRelay(bool host)
    {
        var allocation = await RelayService.Instance.CreateAllocationAsync(4);

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log(joinCode);
        string hostCodeString = $"Hosted Code: {joinCode}";
        hostedCode.text = hostCodeString;

        var unityTranport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (host)
        {
            unityTranport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
        }
        else
        {
            unityTranport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
        }

    }

    internal async Task JoinRelay(string text)
    {
        JoinAllocation joinAllocation = await RelayService
            .Instance.JoinAllocationAsync(text);

        var unityTranport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        unityTranport.SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData
        );
    }
}
