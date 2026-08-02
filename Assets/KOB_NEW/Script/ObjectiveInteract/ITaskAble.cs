using UnityEngine;

public interface ITaskable
{
    // ยูนิตเรียกฟังก์ชันนี้เมื่อต้องทำงานกับ Object นั้น
    void OnUnitInteract(UnitBase unit); 

    void OnUnitExit(UnitBase unit);
    
    // ฟังก์ชันบอกยูนิตว่าควรยืนตรงไหนตอนทำงาน
    Vector3 GetInteractionPoint();
}