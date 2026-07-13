using UnityEngine;

public enum GraftType
{
    Normal,     // กิ่งไม้ธรรมดา
    GonBuri,    // ต้นก้นบุรี (แตกรากแผ่นพื้นใหม่)
    Rose,       // ต้นกุหลาบ (ล่อสัตว์)
    AntiWarm,    // ต้นทนน้ำอุ่น
    BatteryTree
}

public class GraftData : MonoBehaviour
{
    // สคริปต์นี้เอาไว้ใส่ข้อมูลหรือโมเดลจำลองของแต่ละสายพันธุ์
    public GraftType type = GraftType.Normal;
}