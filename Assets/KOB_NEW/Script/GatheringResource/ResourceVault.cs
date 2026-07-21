using UnityEngine;

public class ResourceVault : MonoBehaviour, ITaskable
{
    public string vaultName = "โรงเก็บไม้และหิน";
    // ใน ResourceVault.cs
    public void OnUnitInteract(UnitBase unit)
    {
        Debug.Log($"Inventory : {ResourceInventory.Instance}");
        Debug.Log($"Unit : {unit}");
        Debug.Log($"Carry : {unit?.carriedItem}");

        if (unit != null && unit.isCarrying && unit.carriedItem != null)
        {
            ResourceInventory.Instance.AddResource(unit.carriedItem, unit.carriedAmount);

            unit.DropItemAtVault();

            // 🟢 แก้: อย่ายัด currentState ตรงๆ (unit.currentState = UnitBehavior.Idle)
            // เพราะไม่เคลียร์ carriedItem/carriedAmount/currentOrder ให้ ทำให้ข้อมูลค้างไม่ตรงกับ state
            // ResetUnitState() เป็นจุดเดียวที่ทำความสะอาดครบทุก field พร้อมกัน
            unit.ResetUnitState();
        }
    }

    public void OnUnitExit(UnitBase unit)
    {

    }

    public Vector3 GetInteractionPoint()
    {
        // จุดที่ยูนิตต้องเดินมาหยุดข้างหน้าตึก
        return transform.position + transform.forward * 2f;
    }
}