using UnityEngine;
using TMPro; // ใช้ TextMeshPro (ถ้าใช้ UI Text ธรรมดาให้เปลี่ยนเป็น using UnityEngine.UI;)

public class HudController : MonoBehaviour
{
    public TextMeshProUGUI dayText;      // ลาก Text วันที่มาใส่
    public TextMeshProUGUI seasonText;   // ลาก Text ฤดูกาลมาใส่
    public TextMeshProUGUI timeText;     // ลาก Text เวลา (เช่น นาฬิกา) มาใส่
    public TextMeshProUGUI unitPopulationText; // เช่น 5/16

    void Start()
    {
        // สมัครรับ Event จาก DayNightManager เพื่อให้อัปเดต UI ทันทีที่มีการเปลี่ยนแปลง
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayChanged += UpdateDayUI;
            DayNightManager.Instance.OnSeasonChanged += UpdateSeasonUI;
            
            // อัปเดตค่าเริ่มต้นทันที
            UpdateDayUI(DayNightManager.Instance.currentDay);
            UpdateSeasonUI(DayNightManager.Instance.currentSeason);
        }

        UpdateUnitPopulationText();
    }

    void OnDestroy()
    {
        // ยกเลิก Event เมื่อปิดฉากเพื่อป้องกัน Memory Leak
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayChanged -= UpdateDayUI;
            DayNightManager.Instance.OnSeasonChanged -= UpdateSeasonUI;
        }
    }

    void Update()
    {
        if (DayNightManager.Instance == null) return;

        // อัปเดตเวลาแบบเรียลไทม์ทุกเฟรม
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
        unitPopulationText.text = $"unit cap : {current} / {total}";
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
            seasonText.text = $"{season}"; // สามารถปรับแต่งข้อความภาษาไทยได้ เช่น เปลี่ยน Season เป็นชื่อไทย
        }
    }
}