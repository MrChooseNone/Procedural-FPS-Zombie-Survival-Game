using UnityEngine;
using Mirror;
using System.Collections;

public class DestroyTimer : NetworkBehaviour
{
    public float timer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(delay());
    }

    IEnumerator delay(){
        yield return new WaitForSeconds(timer);
        NetworkServer.Destroy(gameObject);
    }

    
}
