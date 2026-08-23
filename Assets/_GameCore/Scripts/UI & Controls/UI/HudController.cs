using UnityEngine;
using TMPro; 

public class HudController : MonoBehaviour
{
    public TextMeshProUGUI dayText;          // ลาก Text วันที่มาใส่
    public TextMeshProUGUI seasonText;       // ลาก Text ฤดูกาลมาใส่
    public TextMeshProUGUI timeText;         // ลาก Text เวลา (แสดงผลเป็น HH:mm AM/PM)
    public TextMeshProUGUI unitPopulationText; // เช่น 5 / 16

    void Start()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayChanged += UpdateDayUI;
            DayNightManager.Instance.OnSeasonChanged += UpdateSeasonUI;
            
            UpdateDayUI(DayNightManager.Instance.currentDay);
            UpdateSeasonUI(DayNightManager.Instance.currentSeason);
        }

        UpdateUnitPopulationText();
    }

    void OnDestroy()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayChanged -= UpdateDayUI;
            DayNightManager.Instance.OnSeasonChanged -= UpdateSeasonUI;
        }
    }

    void Update()
    {
        if (DayNightManager.Instance == null) return;

        // 🟢 แปลงเวลาเป็นระบบ AM / PM (ภาษาอังกฤษ) ทุกเฟรม
        var gameTime = DayNightManager.Instance.GetGameTime();
        if (timeText != null)
        {
            string ampm = gameTime.hour >= 12 ? "PM" : "AM";
            int displayHour = gameTime.hour % 12;
            if (displayHour == 0) displayHour = 12;

            timeText.text = $"{displayHour:00}:{gameTime.minute:00} {ampm}";
        }

        UpdateUnitPopulationText();
    }

    private void UpdateUnitPopulationText()
    {
        if (unitPopulationText == null) return;

        int current = AnimalShelter.GetCurrentUnitCount();
        int total = AnimalShelter.GetTotalCapacity();
        unitPopulationText.text = $"Unit Cap: {current} / {total}";
    }

    void UpdateDayUI(int day)
    {
        if (dayText != null)
        {
            dayText.text = $"Day {day} / 30";
        }
    }

    void UpdateSeasonUI(Season season)
    {
        if (seasonText != null)
        {
            // แสดงผลชื่อ Season เป็นภาษาอังกฤษตามสากล
            seasonText.text = season.ToString(); // Spring, Summer, Autumn, Winter
        }
    }
}