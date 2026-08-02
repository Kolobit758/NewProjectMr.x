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

            // 🔒 ถ้ายูนิตมี gatBase (Tree/Water node) ให้ gatBase จัดการ ResetUnitState เองผ่าน OnUnitSentResourced
            // ถ้าไม่มี (เช่น Fetch flow) ค่อย ResetUnitState เอง — ป้องกัน Double-call ที่ทำให้ auto-repeat เสียหาย
            if (unit.gatBase != null)
            {
                unit.DropItemAtVault(); // gatBase จะ reset ให้เอง
            }
            else
            {
                unit.DropItemAtVault(); // trigger callback ถ้ามี
                unit.ResetUnitState();  // reset เองเพราะไม่มีใครทำให้
            }
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