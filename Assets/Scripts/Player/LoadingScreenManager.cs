using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
    public Image LoadingScreen;
    public FirstPersonController firstPersonController;
    public bool isLoading = false;

    public void ShowLoadingScreen(){
        isLoading = true;
        LoadingScreen.enabled = true;
        firstPersonController.enabled = false;
    }
    public void HideLoadingScreen(){
        isLoading = false;
        LoadingScreen.enabled = false;
        firstPersonController.enabled = true;
    }
}