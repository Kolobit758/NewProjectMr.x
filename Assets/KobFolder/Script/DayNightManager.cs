using UnityEngine;
using System;

public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance { get; private set; }

    [Header("Time Settings")]
    public float dayDuration = 300f; // สมมติ 5 นาทีต่อวัน
    private float currentTime = 0f;

    // public bool isNightTime { get; private set; }
    public bool isNightTime;

    // Event ให้ระบบอื่นมาดึงไปใช้ (ช่วยลดการเช็คใน Update)
    public event Action<bool> OnTimeChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    

    void Update()
    {
        currentTime += Time.deltaTime;
        
        // เช็คว่าเกินครึ่งของ dayDuration หรือยังเพื่อสลับกลางวัน/กลางคืน
        bool nightStatus = (currentTime % dayDuration) > (dayDuration / 2);

        if (nightStatus != isNightTime)
        {
            isNightTime = nightStatus;
            // ส่งสัญญาณบอกทุกระบบที่รอฟังอยู่
            OnTimeChanged?.Invoke(isNightTime);
            Debug.Log(isNightTime ? "🌙 เข้าสู่เวลากลางคืน!" : "☀️ เข้าสู่เวลากลางวัน!");
        }
    }
}