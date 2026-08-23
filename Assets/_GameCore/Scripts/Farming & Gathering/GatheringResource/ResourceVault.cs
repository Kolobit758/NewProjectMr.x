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
            ResourceInventory.Instance.AddResource(unit.carriedItem, unit.carriedAmount, showFeedback: false);

            // 🟢 แสดงตัวเลขทรัพยากรที่ได้รับลอยขึ้นเหนือโกดัง Vault
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.ShowResourceGain(unit.carriedItem.itemName, unit.carriedAmount, transform.position + Vector3.up * 2.5f);
            }

            if (unit.gatBase != null)
            {
                unit.DropItemAtVault();
            }
            else
            {
                unit.DropItemAtVault();
                unit.ResetUnitState();
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