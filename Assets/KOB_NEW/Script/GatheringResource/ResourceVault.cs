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
            unit.currentBehavior = UnitBehavior.Idle;
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