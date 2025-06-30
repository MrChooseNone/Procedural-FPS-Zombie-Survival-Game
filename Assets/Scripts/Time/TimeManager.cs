using UnityEngine;
using Mirror;
using System.Collections;
using TMPro;

public class NetworkTimeManager : NetworkBehaviour
{
    [Header("Skybox settings")]
    public Material skybox;
    public GameObject sunLight;

    [Header("Time Settings")]
    [Tooltip("1 real second = this many in-game minutes")]
    public float timeScale = 60f;
    public int dayStartHour = 6;
    public int nightStartHour = 18;

    [Header("UI")]
    public TextMeshProUGUI timeText;

    [Header("Time Tracking (sync’d)")]
    [SyncVar(hook=nameof(OnTimeChanged))] 
    int currentHour;
    [SyncVar(hook=nameof(OnTimeChanged))] 
    int currentMinute;
    [SyncVar(hook=nameof(OnDayCountChanged))] 
    int daysPassed;

    [Header("Horde")]
    public ZombieSpawner zombieSpawner;
    [SyncVar] 
    bool hordeSpawned = false;

    private float _timeCounter; 
    
    [SerializeField] NetworkAudioManager audioManager = null;

    #region Server Logic
    public override void OnStartServer()
    {
        // Initialize
        currentHour = dayStartHour;
        currentMinute = 0;
        daysPassed = 0;
        hordeSpawned = false;
        _timeCounter = 0f;

        StartCoroutine(DayNightLoop());
    }

    [Server]
    IEnumerator DayNightLoop()
    {
        while (true)
        {
            // advance internal clock
            _timeCounter += Time.deltaTime * timeScale;
            if (_timeCounter >= 60f)
            {
                _timeCounter -= 60f;
                AdvanceMinute();
            }

            yield return null;
        }
    }

    [Server]
    void AdvanceMinute()
    {
        currentMinute++;
        if (currentMinute >= 60)
        {
            currentMinute = 0;
            currentHour++;

            // new day
            if (currentHour >= 24)
            {
                currentHour = 0;
                daysPassed++;
                hordeSpawned = false;
            }

            // day/night events
            if (currentHour == dayStartHour)
            {
                RpcOnDay();
                audioManager.TriggerAmbientChange(0);
            }
            else if (currentHour == nightStartHour)
            {
                RpcOnNight();
                audioManager.TriggerAmbientChange(1);
            }

            // possible horde spawn at start of night
            if (currentHour == nightStartHour && !hordeSpawned)
                TrySpawnHorde();
                audioManager.TriggerHordeScream();
        }
    }

    [Server]
    void TrySpawnHorde()
    {
        hordeSpawned = true;
        int hordeSize = (daysPassed + 1) * 2;
        zombieSpawner.SpawnHorde(hordeSize);
    }

    [ClientRpc]
    void RpcOnDay()
    {
        // clients can hook into day‐start if needed
        Debug.Log("Day has dawned!");
       
    }

    [ClientRpc]
    void RpcOnNight()
    {
        Debug.Log("Night falls—and the horde stirs!");
    }
    #endregion

    #region Client Logic
    void OnTimeChanged(int oldVal, int newVal)
    {
        // both hour & minute use this hook to refresh UI immediately
        UpdateUI();
    }

    void OnDayCountChanged(int oldVal, int newVal)
    {
        UpdateUI();
    }

    public override void OnStartClient()
    {
        // initial visuals
        UpdateUI();
        UpdateSkybox();
    }

    void Update()
    {
        if (!isClient) return;

        // continuously adjust skybox/sun based on synced time
        UpdateSkybox();
    }
    #endregion

    #region UI & Skybox
    void UpdateUI()
    {
        if (timeText == null) return;
        timeText.text = $"Day {daysPassed + 1} – {currentHour:00}:{currentMinute:00}";
    }

    void UpdateSkybox()
    {
        float norm = GetNormalizedTimeOfDay();
        float t     = Mathf.Lerp(-90f, 270f, norm);
        sunLight.transform.rotation = Quaternion.Euler(t, 170f, 0f);

        // optional: if your shader uses a transition curve
        if (skybox.HasProperty("_CubemapTransition"))
        {
            float ct = Mathf.Clamp01((currentHour * 60f + currentMinute) / (24f * 60f));
            skybox.SetFloat("_CubemapTransition", ct);
        }
    }
    #endregion

    #region Helpers
    public float GetNormalizedTimeOfDay()
    {
        return (currentHour * 60f + currentMinute) / (24f * 60f);
    }
    #endregion
}
