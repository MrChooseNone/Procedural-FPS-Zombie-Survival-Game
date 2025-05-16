using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Mirror;
using System.Collections;

public class InputCity : NetworkBehaviour
{
    public TMP_InputField Street;
    public TMP_InputField City;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        while (!NetworkServer.active)
            yield return null;
            
        if(!isServer){
            Street.enabled = false;
            City.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        CityData.street = Street.text;
        CityData.city = City.text;
    }
}
