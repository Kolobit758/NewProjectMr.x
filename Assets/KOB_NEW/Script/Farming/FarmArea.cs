// using System.Collections.Generic;
// using UnityEngine;

// /// <summary>
// /// วางสคริปต์นี้ไว้ที่ตัวแม่ (parent GameObject) ของกลุ่มแปลงผัก (CropPlots) หลายๆ แปลง
// /// ใช้เป็นเป้าหมายตอนผู้เล่นลากคลุมเลือกยูนิตหลายตัว แล้วคลิกขวาชี้เข้าไปที่ "ฟาร์ม" ทั้งก้อน
// /// (ต้องมี Collider ที่ครอบคลุมพื้นที่ฟาร์มไว้ให้ Raycast จาก RTS_movement โดนด้วย)
// ///
// /// ระบบจะเป็นคนแบ่งงานให้แต่ละยูนิตไปคนละแปลงเอง (ดู RTS_movement.AssignUnitsToFarm)
// /// </summary>
// public class FarmArea : MonoBehaviour
// {
//     [Tooltip("ปล่อยว่างไว้ได้ ถ้าไม่ใส่ระบบจะ auto-scan หาแปลงผักที่เป็นลูกของตัวเองให้เองตอน Awake")]
//     public List<CropPlots> plots = new List<CropPlots>();

//     private void Awake()
//     {
//         if (plots == null || plots.Count == 0)
//         {
//             plots = new List<CropPlots>(GetComponentsInChildren<CropPlots>());
//         }
//     }

//     /// <summary>
//     /// คืนรายชื่อแปลงที่ "ต้องการคนช่วย" อยู่จริง ๆ ตอนนี้
//     /// (ขาดปุ๋ย/ขาดน้ำ และไม่มีใครถูกล็อคว่ากำลังจัดการอยู่)
//     /// </summary>
//     public List<CropPlots> GetAvailablePlots()
//     {
//         List<CropPlots> result = new List<CropPlots>();

//         foreach (var plot in plots)
//         {
//             if (plot == null) continue;
//             if (plot.isBeingServiced) continue; // มีคนกำลังไปเบิกของให้แปลงนี้อยู่แล้ว ข้ามไปก่อน

//             if (plot.currentStage == CropStage.NeedsFertilizer || plot.currentStage == CropStage.NeedsWater)
//             {
//                 result.Add(plot);
//             }
//         }

//         return result;
//     }
// }