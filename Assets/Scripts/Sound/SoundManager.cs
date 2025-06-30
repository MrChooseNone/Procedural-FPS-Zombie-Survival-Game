using UnityEngine;
using Mirror;

public class NetworkAudioManager : NetworkBehaviour
{
    [Header("Audio Clips")]
    public AudioClip[] ambientLoopSounds; 
    public AudioClip screamClip;

    public AudioSource _audioSource;
    public AudioSource _audioSourceScream;

    #region Unity Callbacks
    public override void OnStartClient()
    {
        // Every client (including host) grabs the AudioSource and starts ambient
        _audioSource = GetComponent<AudioSource>();
        if (ambientLoopSounds[0] != null)
        {
            _audioSource.clip = ambientLoopSounds [0];
            _audioSource.loop = true;
            _audioSource.Play();
        }
    }
    #endregion

    #region Server Triggers
    // Call this from any server-side code when the horde is detected:
    [Server]
    public void TriggerHordeScream()
    {
        // Tell every client to play the scream
        RpcPlayScream();
    }
    #endregion

    #region ClientRpcs
    [ClientRpc]
    void RpcPlayScream()
    {
        // Plays on top of whatever is already playing
        
        _audioSourceScream.PlayOneShot(screamClip);
    }
    #endregion

    [Server]
    public void TriggerAmbientChange(int index)
    {
        // Tell every client to play the scream
        RpcPlayAmbient(index);
    }



    [ClientRpc]
    void RpcPlayAmbient(int index)
    {
        if (_audioSource.clip != ambientLoopSounds[index])
        {
            _audioSource.clip = ambientLoopSounds[index];
            _audioSource.loop = true;
            _audioSource.Play();
        }
    }
}