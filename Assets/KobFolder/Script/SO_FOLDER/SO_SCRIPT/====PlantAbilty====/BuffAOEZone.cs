using System.Collections.Generic;
using UnityEngine;

public class BuffAOEZone : MonoBehaviour
{
    public SO_PlantProduct productData;
    // ใช้เก็บรายชื่อยูนิตทั้งหมดที่กำลังเหยียบอยู่บนพื้น AOE นี้ในเฟรมปัจจุบัน
    private HashSet<UnitBuffManager> unitsInsideInCurrentFrame = new HashSet<UnitBuffManager>();
    private HashSet<UnitBuffManager> unitsInsideInLastFrame = new HashSet<UnitBuffManager>();

    public void Setup(SO_PlantProduct data)
    {
        productData = data;
        // ทำลายพื้นที่ AOE ทิ้งเมื่อหมดเวลาอายุของโซน
        Destroy(gameObject, data.zoneDuration);
        
        // ปรับขนาดของ Collider/Visual ตามรัศมี AOE ที่กำหนดใน ScriptableObject
        transform.localScale = new Vector3(data.aoeRadius * 2, 0.1f, data.aoeRadius * 2);
    }


    // ทำงานทุกเฟรมเพื่อตรวจสอบยูนิตที่เหยียบอยู่
    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<UnitBuffManager>(out var unit))
        {   
        
            unitsInsideInCurrentFrame.Add(unit);
            
            // ถ้ายูนิตเพิ่งเดินเข้ามาเหยียบเฟรมแรก (หรือยังเหยียบอยู่) ให้รีเฟรชสถานะว่า "อยู่บนพื้น"
            foreach (var buff in productData.buffsToApply)
            {
                unit.AddOrRefreshBuff(buff, isInsideZone: true);
            }
        }
    }

    private void LateUpdate()
    {
        // ตรวจสอบว่ามียูนิตไหนที่เฟรมที่แล้วเคยอยู่ แต่เฟรมนี้ไม่ได้อยู่แล้ว (แปลว่าเดินออกจากพื้น AOE ไปแล้ว)
        foreach (var unit in unitsInsideInLastFrame)
        {
            if (unit != null && !unitsInsideInCurrentFrame.Contains(unit))
            {
                // แจ้งเตือนยูนิตนั้นว่า "คุณเดินออกจากพื้นแล้วนะ เริ่มนับถอยหลังเวลาบัฟได้"
                foreach (var buff in productData.buffsToApply)
                {
                    unit.NotifyLeftZone(buff);
                }
            }
        }

        // สลับข้อมูลเพื่อเตรียมเช็คในเฟรมถัดไป
        unitsInsideInLastFrame.Clear();
        foreach (var unit in unitsInsideInCurrentFrame)
        {
            unitsInsideInLastFrame.Add(unit);
        }
        unitsInsideInCurrentFrame.Clear();
    }

    // เผื่อในกรณีที่วง AOE หมดอายุขัยและโดน Destroy ไปดื้อๆ ขณะที่ยูนิตยังยืนเหยียบอยู่
    private void OnDestroy()
    {
        foreach (var unit in unitsInsideInLastFrame)
        {
            if (unit != null)
            {
                foreach (var buff in productData.buffsToApply)
                {
                    unit.NotifyLeftZone(buff);
                }
            }
        }
    }
}