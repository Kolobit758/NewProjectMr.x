using UnityEngine;

public class GreyboxBullet : MonoBehaviour
{
    private int damage;
    private Vector3 shooterPosition;
    private LayerMask targetLayers;

    // ฟังก์ชันนี้จะถูกเรียกจากตัว RangedEnemy ตอนที่ปล่อยกระสุนออกมา
    public void SetupBullet(int dmg, Vector3 shooterPos, LayerMask layers)
    {
        damage = dmg;
        shooterPosition = shooterPos;
        targetLayers = layers;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. เช็คว่าชนวัตถุที่อยู่ใน Layer ที่เราตั้งไว้ไหม
        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            // 2. เช็ค Tag ว่าเป็น Player หรือ Unit หรือไม่
            if (other.CompareTag("Player") || other.CompareTag("Unit"))
            {
                // 3. ทำดาเมจเข้า CharacterStats ของเป้าหมายจริง
                if (other.TryGetComponent<CharacterStats>(out var stats))
                {
                    stats.TakeDamage(damage, shooterPosition);
                }
                
                // 4. ชนเสร็จแล้วทำลายลูกกระสุนทิ้งทันที
                Destroy(gameObject); 
            }
        }
    }
}