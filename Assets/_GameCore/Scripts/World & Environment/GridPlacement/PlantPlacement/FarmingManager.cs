// using UnityEngine;

// public class FarmingManager : MonoBehaviour
// {
//     public static FarmingManager Instance { get; private set; }

//     [Header("Selected Seed Data")]
//     public SO_PlantData currentSelectedSeed; // เมล็ดพันธุ์ที่กำลังถืออยู่ (ผูกจากปุ่ม Hotbar มึงวันหลัง)
//     public LayerMask farmPlotLayer;          // Layer ของแปลงผัก

//     void Awake()
//     {
//         if (Instance != null && Instance != this) Destroy(gameObject);
//         else Instance = this;
//     }

//     void Update()
//     {
//         // 🟢 ถ้าไม่ได้ง้างโหมดสร้างตึกอยู่ ให้เปิดระบบจิ้มแปลงผักทำฟาร์มได้เลยมึง
//         if (Input.GetMouseButtonDown(0))
//         {
            
//             TryInteractWithPlot();
//         }
//     }

//     void TryInteractWithPlot()
//     {
//         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//         if (Physics.Raycast(ray, out RaycastHit hit, 100f, farmPlotLayer))
//         {

//             Debug.Log("ลองปลูก");
//             CropPlots plot = hit.collider.GetComponent<CropPlots>();
//             if (plot == null) plot = hit.collider.GetComponentInParent<CropPlots>();

//             if (plot != null)
//             {
//                 // กรณีที่ 1: แปลงว่างอยู่ + ถือเมล็ดพันธุ์ในมือ ➡️ สั่งปลูกผักทันที!
//                 if (plot.currentStage == CropStage.Empty && currentSelectedSeed != null)
//                 {
//                     bool success = plot.PlantSeed(currentSelectedSeed);
//                     if (success)
//                     {
//                         Debug.Log($"🌱 [Farming]: ปลูกเมล็ด {currentSelectedSeed.itemName} สำเร็จ!");

//                         // หักไอเทมเมล็ดพันธุ์ชิ้นนี้ออกจากกระเป๋าจริงของผู้เล่น 1 ชิ้นทันทีมึง!
//                         if (ResourceInventory.Instance != null)
//                         {
//                             ResourceInventory.Instance.ConsumeResource(currentSelectedSeed, 1);
//                         }

//                         // ปลูกเสร็จแล้ว เคลียร์ของออกจากมือเล็ง
//                         currentSelectedSeed = null;
//                     }
//                 }
//                 // กรณีที่ 2: ผักโตเต็มที่หลอดแล้ว ➡️ สั่งเก็บเกี่ยวกวาดปุ๋ยเข้ากระเป๋า!
//                 else if (plot.currentStage == CropStage.ReadyToHarvest)
//                 {
//                     plot.HarvestCrop();
//                 }
//             }
//         }
//     }
// }