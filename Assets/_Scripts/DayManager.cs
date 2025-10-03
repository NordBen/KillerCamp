using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DayManager : MonoBehaviour
{
    public float timer = 90f;

    public Image voteScreen;

    public NetworkManager networkManager;

    public List<GameObject> players = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //players.Length = NetworkManager.Singleton.ConnectedClients.Count;
        StartCoroutine(NightCycle());
    }

    IEnumerator NightCycle()
    {
        yield return new WaitForSeconds(timer);
    }

    private void StartVoting()
    {
        voteScreen.gameObject.SetActive(true);
    }
}
