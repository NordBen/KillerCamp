using UnityEngine;
using Unity.Netcode;

public class TaskManager : NetworkBehaviour
{

    public NetworkVariable<bool> HasTask;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public override void OnNetworkSpawn()
    {
        if (HasAuthority)
        {
            HasTask.Value = true;
        }
        base.OnNetworkSpawn();
    }

    [Rpc(SendTo.Server)]
    public void CompleteTaskRpc()
    {
        if (HasAuthority)
        {
            HasTask.Value = false;
        }
    }
}
