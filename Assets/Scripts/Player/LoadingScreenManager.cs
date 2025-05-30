
using UnityEngine;


public class LoadingScreenManager : MonoBehaviour
{
    public GameObject LoadingScreen;
    public FirstPersonController firstPersonController;
    public HealthSystem health;
    public bool isLoading = false;
    void Update()
    {
        if (isLoading)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
    }

    public void ShowLoadingScreen()
    {
        isLoading = true;
        LoadingScreen.SetActive(true);
        firstPersonController.enabled = false;
        health.enabled = false;
    }
    public void HideLoadingScreen()
    {
        isLoading = false;
        LoadingScreen.SetActive(false);
        firstPersonController.enabled = true;
        health.enabled = false;
    }
}