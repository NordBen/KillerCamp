using UnityEngine;

public class DontDestroyOnLoadScript : MonoBehaviour
{
    public static DontDestroyOnLoadScript Instance;
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
