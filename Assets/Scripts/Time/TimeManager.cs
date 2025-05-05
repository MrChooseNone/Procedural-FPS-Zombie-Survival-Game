using TMPro;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI; // Only if using UI Text

public class TimeManager : MonoBehaviour
{
    [Header("Skybox settings")]
    public Material skybox;
    public GameObject sunLight;

    [Header("Time Settings")]
    public float timeScale = 60f; // 1 real second = 1 in-game minute
    public int dayStartHour = 6;
    public int nightStartHour = 18;
    // public float fullDayLengthInMinutes = 24f; // in-game hours (24h format)

    [Header("UI")]
    public TextMeshProUGUI timeText; // drag your UI Text here in Inspector

    [Header("Time Tracking")]
    public int currentHour;
    public int currentMinute;
    public int daysPassed;

    public bool isDay;
    public bool isNight;

    private float timeCounter;

    public delegate void DayNightEvent();
    public event DayNightEvent OnDay;
    public event DayNightEvent OnNight;

    private bool wasDayLastFrame;
    public AnimationCurve cubemapTransitionCurve;
    public ZombieSpawner zombieSpawner;
    public bool hordeSpawned = false;


    void Start()
    {
        currentHour = dayStartHour;
        currentMinute = 0;
        wasDayLastFrame = true;
        isDay = true;
        isNight = false;
    }

    void Update()
    {
        UpdateTime();
        UpdateUI();
        HandleDayNightSwitch();
        UpdateSkybox();
        CheckHordeSpawn();
    }

    void UpdateSkybox(){
        float transitionValue = cubemapTransitionCurve.Evaluate(GetNormalizedTimeOfDay());
        skybox.SetFloat("_CubemapTransition", transitionValue);
        // Simulate sun rotation over a full 24h cycle
        float t = GetNormalizedTimeOfDay(); // 0 to 1
        float sunAngle = Mathf.Lerp(-90f, 270f, t); // Rises in east, sets in west
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
    }

    void UpdateTime()
    {
        timeCounter += Time.fixedDeltaTime * timeScale;

        while (timeCounter >= 60f)
        {
            currentMinute++;
            timeCounter -= 60f;

            if (currentMinute >= 60)
            {
                currentMinute = 0;
                currentHour++;

                if (currentHour >= 24)
                {
                    currentHour = 0;
                    daysPassed++;
                    Debug.Log("Day passed: " + daysPassed);

                    // Call this to maybe spawn a horde
                    
                }
            }
        }
    }

    void UpdateUI()
    {
        if (timeText != null)
        {
            string hourStr = currentHour.ToString("00");
            string minStr = currentMinute.ToString("00");
            timeText.text = $"Day {daysPassed + 1} - {hourStr}:{minStr}";
        }
    }

    void HandleDayNightSwitch()
    {
        isDay = currentHour >= dayStartHour && currentHour < nightStartHour;
        isNight = !isDay;

        if (isDay != wasDayLastFrame)
        {
            if (isDay)
                OnDay?.Invoke();
            else
                OnNight?.Invoke();

            wasDayLastFrame = isDay;
        }
    }

    void CheckHordeSpawn()
    {
        // Example: spawn a horde every 3rd day
        if ((daysPassed + 1) % 3 == 0 && !hordeSpawned)
        {
            Debug.Log("Horde incoming tonight!");
            
            int hordeSize = daysPassed * 2; // scale with day
            zombieSpawner.SpawnHorde(hordeSize);
            hordeSpawned = true;
            float resetTime = 0.02f*timeScale*1440f; // 0.02 is fixed time, timescale is the variable, 50 gets 24 minutes per day, 1440 is the amount of minutes per day
            Invoke("ResetHord", resetTime);
            Debug.Log("Horde incoming! Size: " + hordeSize);
        }
    }
    void ResetHord(){
        hordeSpawned = false;
    }

    public float GetNormalizedTimeOfDay()
    {
        return (currentHour * 60f + currentMinute) / (24f * 60f);
    }
}
