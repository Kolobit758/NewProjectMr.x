using UnityEngine;
using UnityEngine.Rendering; // 🟢 เพิ่มบรรทัดนี้ สำหรับ class Volume
using System;

public enum Season { Spring, Summer, Autumn, Winter }

public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance { get; private set; }

    [Header("Time Settings")]
    public float dayDuration = 300f; // 5 นาทีต่อ 1 วัน
    private float currentTime = 0f;

    [Header("Calendar Settings")]
    public int currentDay = 1;          // วันที่ปัจจุบันในเกม
    public int daysPerSeason = 4;       // 1 ฤดู มีกี่วัน
    public Season currentSeason = Season.Spring; // ฤดูกาลปัจจุบัน

    public bool isNightTime;
    [Header("Lighting Settings")]
    public Light directionalLight;
    public float dayLightRotationX = 50f;
    public float nightLightRotationX = -89.9f; // เลี่ยงมุม -90 พอดี (gimbal lock)
    public float lightRotationY = 0f;          // ← เพิ่มใหม่: ค่า Y คงที่ ไม่อ่านย้อนจาก transform
    public float lightRotationZ = 0f;          // ← เพิ่มใหม่: ค่า Z คงที่ ไม่อ่านย้อนจาก transform

    // Events สำหรับแจ้งเตือนระบบอื่น
    public event Action<bool> OnTimeChanged;
    public event Action<int> OnDayChanged;
    public event Action<Season> OnSeasonChanged;
    [Header("Global Volume Settings")]
    public Volume dayGlobalVolume;
    public Volume nightGlobalVolume;


    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        float previousTime = currentTime;
        currentTime += Time.deltaTime;

        if (Mathf.FloorToInt(currentTime / dayDuration) > Mathf.FloorToInt(previousTime / dayDuration))
        {
            AdvanceDay();
        }

        float timeNormalized = (currentTime % dayDuration) / dayDuration;
        UpdateLightingRotation(timeNormalized);

        bool nightStatus = timeNormalized >= 0.5f;

        if (nightStatus != isNightTime)
        {
            isNightTime = nightStatus;
            UpdateGlobalVolumeBlend(isNightTime); // 🟢 เรียกตอน "เปลี่ยนสถานะ" เท่านั้น ไม่ต้องเช็คทุกเฟรม
            OnTimeChanged?.Invoke(isNightTime);
            Debug.Log(isNightTime ? "🌙 [Time]: Night time started." : "☀️ [Time]: Day time started.");
        }
    }
    private void UpdateLightingRotation(float timeNormalized)
    {
        if (directionalLight == null) return;

        float targetRotationX;
        if (timeNormalized < 0.5f)
        {
            float t = timeNormalized * 2f;
            targetRotationX = Mathf.Lerp(dayLightRotationX, nightLightRotationX, t);
        }
        else
        {
            float t = (timeNormalized - 0.5f) * 2f;
            targetRotationX = Mathf.Lerp(nightLightRotationX, dayLightRotationX + 360f, t);
        }

        // 🟢 ใช้ค่า Y/Z คงที่ที่กำหนดไว้เอง แทนการอ่านย้อนจาก transform
        // (การอ่าน eulerAngles.y/z กลับมาใช้ทุกเฟรม คือสาเหตุที่ทำให้เกิด feedback loop
        // เวลาเจอ gimbal lock ที่ X = -90 พอดี ทำให้ค่าดีดสลับไปเรื่อยๆ)
        directionalLight.transform.rotation = Quaternion.Euler(targetRotationX, lightRotationY, lightRotationZ);
    }

    private void AdvanceDay()
    {
        currentDay++;
        OnDayChanged?.Invoke(currentDay);
        Debug.Log($"🗓️ [Calendar]: New day started! Day {currentDay}");

        CalculateSeason();
    }

    private void CalculateSeason()
    {
        int seasonIndex = ((currentDay - 1) / daysPerSeason) % 4;
        Season newSeason = (Season)seasonIndex;

        if (newSeason != currentSeason)
        {
            currentSeason = newSeason;
            OnSeasonChanged?.Invoke(currentSeason);
            Debug.Log($"🌾 [Calendar]: Season changed to ➡️ <b>{currentSeason}</b>!");

            // เมื่อเปลี่ยนฤดู สั่งให้ร้านค้าสุ่มออเดอร์ใหม่ตามฤดูทันที
            if (ShopOrderManager.Instance != null)
            {
                ShopOrderManager.Instance.GenerateDailyProceduralOrders();
            }
        }
    }


    // 🌟 [ContextMenu] สำหรับคลิกขวาที่ Component เพื่อเทสกลางวัน/กลางคืนด่วนๆ
    [ContextMenu("Force Test / Switch to Day (บังคับเป็นกลางวัน)")]
    public void ForceTestDay()
    {
        currentTime = Mathf.Floor(currentTime / dayDuration) * dayDuration;
        bool oldNightStatus = isNightTime;
        isNightTime = false;

        float t = (currentTime % dayDuration) / dayDuration;
        UpdateLightingRotation(t);
        UpdateGlobalVolumeBlend(isNightTime); // 🟢 แก้ตรงนี้

        if (oldNightStatus != isNightTime) OnTimeChanged?.Invoke(isNightTime);
        Debug.Log("☀️ [ContextMenu]: บังคับเปลี่ยนเป็น 'กลางวัน' สำเร็จ!");
    }

    [ContextMenu("Force Test / Switch to Night (บังคับเป็นกลางคืน / เรียก Wave)")]
    public void ForceTestNight()
    {
        currentTime = (Mathf.Floor(currentTime / dayDuration) * dayDuration) + (dayDuration * 0.75f);
        bool oldNightStatus = isNightTime;
        isNightTime = true;

        float t = (currentTime % dayDuration) / dayDuration;
        UpdateLightingRotation(t);
        UpdateGlobalVolumeBlend(isNightTime); // 🟢 แก้ตรงนี้

        if (oldNightStatus != isNightTime) OnTimeChanged?.Invoke(isNightTime);
        Debug.Log("🌙 [ContextMenu]: บังคับเปลี่ยนเป็น 'กลางคืน' สำเร็จ!");
    }

    // 🌟 [ContextMenu] เพิ่มปุ่มสำหรับคลิกขวาเทสข้ามวันใหม่ด่วนๆ
    [ContextMenu("Force Test / Next Day (ข้ามไปวันใหม่ + สุ่มออเดอร์ร้านค้า)")]
    public void ForceTestNextDay()
    {
        AdvanceDay();

        // บังคับรีเซ็ตเวลาให้อยู่ช่วงเช้าของวันใหม่พอดี
        currentTime = (currentDay - 1) * dayDuration;

        Debug.Log($"🗓️ [ContextMenu]: บังคับข้ามมาที่ Day {currentDay} สำเร็จ!");
    }
    private void UpdateGlobalVolumeBlend(bool isNight)
    {
        if (dayGlobalVolume != null)
            dayGlobalVolume.gameObject.SetActive(!isNight);

        if (nightGlobalVolume != null)
            nightGlobalVolume.gameObject.SetActive(isNight);
    }
}