using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Plants/Plant_Product")]
public class SO_PlantProduct : SO_ItemData
{
    public string productName;
    [TextArea] public string productDescription;
    public GameObject productPrefab;

    [Header("AOE Settings")]
    public float aoeRadius = 5f;
    public float zoneDuration = 8f;

    [Header("Buffs Provided")]
    public List<BaseBuffData> buffsToApply;

    [Header("Projectile Throw Settings")]
    public float throwArcHeight = 3f;
    public float throwDuration = 0.8f;
    public GameObject aoeZonePrefab;



    // public override bool UseItem(GameObject user)
    // {
    //     // 🔍 ค้นหาคอนโทรลเลอร์ปายาพืชที่ตัวผู้เล่น
    //     PlayerThrowBuffController throwController = user.GetComponent<PlayerThrowBuffController>();

    //     if (throwController != null)
    //     {
    //         // 🔥 ส่งข้อมูลตัวเอง (SO) ไปลงทะเบียน และสั่งให้เริ่มเข้าสู่โหมดเล็งเตรียมโยนทันที!
    //         throwController.SetupPlantProduct(this);
    //         return true; // คืนค่า true บอกกระเป๋าว่าเปิดใช้งานฟังก์ชันสำเร็จ
    //     }

    //     return false;
    // }
    // เพิ่มเติมใน SO_ItemData.cs ของคุณ
    public override bool UseItem(GameObject user)
    {
        // ถ้าเป็นประเภทพืช ผลผลิต หรือเมล็ด ให้ใช้ระบบขว้างเพื่อเทมสัตว์
        if (itemType == ItemType.Plant_Product || itemType == ItemType.Seed)
        {
            if (PlayerThrowManager.Instance != null)
            {
                PlayerThrowManager.Instance.StartThrowMode(this);
                Debug.Log($"[Item] เตรียมขว้าง {itemName} เพื่อล่อสัตว์!");
                return true;
            }
        }

        // ลอจิกเดิมของคุณ...
        return false;
    }

}