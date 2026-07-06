using UnityEngine;

// คลาสหลักสำหรับบัฟทุกตัว
public abstract class BaseBuffData : ScriptableObject
{
    public string buffName;
    public float duration; // ระยะเวลาของบัฟเมื่อเดินออกจากวง
    public Sprite buffIcon;

    // ฟังก์ชันที่จะถูกเรียกเมื่อ Unit ได้รับบัฟนี้
    public abstract void ApplyBuff(GameObject target);
    
    // ฟังก์ชันเมื่อบัฟหมดเวลา
    public abstract void RemoveBuff(GameObject target);
}