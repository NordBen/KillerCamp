using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerController : MonoBehaviour
{
    [SerializeField]
    TMP_InputField code;
    
    [SerializeField] RelayManager relayManager;

    public async void StartHost()
    {
        await relayManager.CreateRelay(true);
        SceneManager.sceneLoaded += SceneManager_sceneLoaded_Host;
        SceneManager.LoadSceneAsync("Camp");
    }

    private void SceneManager_sceneLoaded_Host(Scene arg0, LoadSceneMode arg1)
    {
        NetworkManager.Singleton.StartHost();
        SceneManager.sceneLoaded -= SceneManager_sceneLoaded_Host;
    }

    public async void StartClient()
    {
        await relayManager.JoinRelay(code.text);
        NetworkManager.Singleton.StartClient();
    }

    public async void StartServer()
    {
        await relayManager.CreateRelay(false);
        SceneManager.LoadScene("Camp");
        SceneManager.sceneLoaded += SceneManager_sceneLoaded_Server;
    }

    private void SceneManager_sceneLoaded_Server(Scene arg0, LoadSceneMode arg1)
    {
        NetworkManager.Singleton.StartServer();
        SceneManager.sceneLoaded -= SceneManager_sceneLoaded_Server;
    }
}
