using UnityEngine;
using Steamworks;
using UnityEngine.UI;
using TMPro;
using System;

public class playerListItem : MonoBehaviour
{
    public string PlayerName;
    public  int ConnectionID;
    public ulong PlayerSteamID;
    private bool AvatarRecived;

    public TextMeshProUGUI PlayerNameText;
    public RawImage PlayerIcon;

    protected Callback<AvatarImageLoaded_t> ImageLoaded;
    private void Start(){
        ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
    }
    public void SetPlayerValues(){
        PlayerNameText.text = PlayerName;
        if(!AvatarRecived) {GetPlayerIcon();}
    }

    void GetPlayerIcon(){
        int ImageID = SteamFriends.GetLargeFriendAvatar((CSteamID)PlayerSteamID);
        if(ImageID == -1){
            return;
        }
        PlayerIcon.texture = GetSteamImageAsTexture(ImageID);
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback)
    {
        if(callback.m_steamID.m_SteamID == PlayerSteamID){
            PlayerIcon.texture = GetSteamImageAsTexture(callback.m_iImage);
        }else{
            return;
        }
    }

    private Texture2D GetSteamImageAsTexture(int iImage)
    {
        Texture2D texture = null;

        bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);
        if (isValid)
        {
            byte[] image = new byte[width * height * 4];

            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));

            if (isValid)
            {
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(image);
                texture.Apply();
            }
        }
        AvatarRecived = true;
        return texture;
    }
}
