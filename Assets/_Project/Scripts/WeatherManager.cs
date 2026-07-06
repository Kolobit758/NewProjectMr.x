using TMPro;
using UnityEngine;

public enum WeatherType
{
    Normal,
    Heat
}

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Weather")]
    public WeatherType currentWeather = WeatherType.Normal;

    [Header("UI")]
    public TextMeshProUGUI weatherText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ToggleWeather();
        }
    }

    private void ToggleWeather()
    {
        if (currentWeather == WeatherType.Normal)
        {
            currentWeather = WeatherType.Heat;
        }
        else
        {
            currentWeather = WeatherType.Normal;
        }

        Debug.Log("Weather changed to: " + currentWeather);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (weatherText != null)
        {
            weatherText.text = "Weather: " + currentWeather;
        }
    }

    public bool IsHeat()
    {
        return currentWeather == WeatherType.Heat;
    }
}
