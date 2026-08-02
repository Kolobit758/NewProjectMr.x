// using UnityEngine;

// public class BatEntity : MonoBehaviour
// {
//     public float speed = 3f;
//     private CropPlots targetPlot;
//     private bool isEating = false;
//     private float eatTimer = 0f;
//     public float timeToEatCrop = 3f; // เวลาที่ใช้ในการแทะผักจนเน่าหาย (วินาที)

//     // 🎯 สั่งตั้งเป้าหมายแปลงผักจากตัว Spawner
//     public void InitializeTarget(CropPlots plot)
//     {
//         targetPlot = plot;
//     }

//     void Update()
//     {
//         if (targetPlot == null)
//         {
//             // ถ้าแปลงผักโดนเคลียร์หรือเน่าไปแล้ว ให้ค้างคาวบินหนีหายไปบนฟ้า
//             transform.Translate(Vector3.up * speed * Time.deltaTime);
//             Destroy(gameObject, 3f);
//             return;
//         }

//         // 🚶 ขยับบินดิ่งเข้าหาพิกัดแปลงผักเป้าหมาย
//         float distance = Vector3.Distance(transform.position, targetPlot.transform.position);

//         if (distance > 0.5f && !isEating)
//         {
//             Vector3 direction = (targetPlot.transform.position - transform.position).normalized;
//             transform.position += direction * speed * Time.deltaTime;
//         }
//         else
//         {
//             // 🍴 บินมาถึงแปลงผักแล้ว เริ่มลูปรุมแทะแดกผักมึงกอบ!
//             StartEating();
//         }
//     }

//     private void StartEating()
//     {
//         isEating = true;

//         if (targetPlot != null && (targetPlot.currentStage == CropStage.ReadyToHarvest || targetPlot.currentStage == CropStage.Growing))
//         {
//             eatTimer += Time.deltaTime;
//             if (eatTimer >= timeToEatCrop)
//             {
//                 eatTimer = 0f;

//                 // 🔥 สั่งหักเลือดแปลงผักโดยตรง (ไม่เข้ากระเป๋าผู้เล่นชัวร์มึง!)
//                 targetPlot.TakeDamageFromBat();

//                 // แดกเสร็จ 1 รอบ สั่งทำลายตัวเองทันที (ตัวต่อไปค่อยมาตอมต่อให้ครบโควตา)
//                 Destroy(gameObject);
//             }
//         }
//         else
//         {
//             targetPlot = null;
//         }
//     }
// }