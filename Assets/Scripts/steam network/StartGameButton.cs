using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class StartGameButton : MonoBehaviour
{
    private Button startGameButton;
    private CustomNetworkManager networkManager;

    void Start()
    {
        startGameButton = GetComponent<Button>();
        startGameButton.onClick.AddListener(OnStartGameClicked);
    }

    void OnStartGameClicked()
    {
        if (networkManager == null)
        {
            networkManager = (CustomNetworkManager)NetworkManager.singleton;
        }

        if (networkManager != null && NetworkServer.active) // Only the host should start the game
        {
            networkManager.StartGame();
        }
        else
        {
            Debug.LogWarning("Only the host can start the game!");
        }
    }
}
