using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraHolder : NetworkBehaviour
{
    public GameObject cameraHolder;
    public Vector3 offset;

    public override void OnNetworkSpawn()
    {
        cameraHolder.SetActive(IsOwner);
        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if(SceneManager.GetActiveScene().name == "Camp")
        {
            cameraHolder.transform.position = transform.position + offset;
        }
    }
}
