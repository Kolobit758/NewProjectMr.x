using UnityEngine;


[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Building")]
public class SO_Building : SO_ItemData
{
    [Header("Building System Connection")]
    public GameObject buildingGhostPrefab; // โมเดลโปร่งแสงสีเขียวตอนกำลังเล็งวาง
    public GameObject Realprefab;

    public override bool UseItem(GameObject user)
    {
        // 🏗️ ปลุกระบบกางบล็อกตารางผ่าน Singleton Instance
        if (GridPlacementManager.Instance != null && buildingGhostPrefab != null)
        {
            GridPlacementManager.Instance.StartPlacementMode(buildingGhostPrefab, this);
            Debug.Log($"[Grid System] 🏗️ เปิดพิมพ์เขียว {itemName} เล็งตำแหน่งสร้างบน Grid แล้ว!");
            return true;
        }

        return false;
    }
}