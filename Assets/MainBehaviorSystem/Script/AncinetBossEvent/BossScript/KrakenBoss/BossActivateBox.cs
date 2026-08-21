using UnityEngine;

public class BossActivateBox : MonoBehaviour
{
    public KrakenBossAI3D krakenBossAI3D;

    public void OnTriggerEnter(Collider other)
    {
        // 1. ดักจับตัวผู้เล่นเพื่อเปิดใช้งานบอส
        if (other.gameObject.CompareTag("Player"))
        {
            if (krakenBossAI3D != null) krakenBossAI3D.ActivateBoss();
        }

        // 2. 🟢 ดักจับทหาร (Tag: Unit) หรือผู้เล่นที่เข้ามาในพื้นที่ -> แอดรายชื่อลงลิสต์สมองบอส!
        if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Unit"))
        {
            if (other.TryGetComponent<CharacterStats>(out CharacterStats stats))
            {
                if (krakenBossAI3D != null && !krakenBossAI3D.activeTargetsInArena.Contains(stats))
                {
                    krakenBossAI3D.activeTargetsInArena.Add(stats);
                    Debug.Log($"🛡️ [Arena Register]: {other.gameObject.name} ลงทะเบียนเข้าสู่สมรภูมิ!");
                }
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        // 1. ดักจับตัวผู้เล่นหนีทัพออกจากลานรบ
        if (other.gameObject.CompareTag("Player"))
        {
            if (krakenBossAI3D != null) krakenBossAI3D.ForceBossRetreat();
        }

        // 2. 🔴 ดักจับยูนิตที่เดินออกจากพื้นที่ลานประลอง หรือทหารตาย -> ลบรายชื่อออกจากสารบบบอส
        if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Unit"))
        {
            if (other.TryGetComponent<CharacterStats>(out CharacterStats stats))
            {
                if (krakenBossAI3D != null && krakenBossAI3D.activeTargetsInArena.Contains(stats))
                {
                    krakenBossAI3D.activeTargetsInArena.Remove(stats);
                    Debug.Log($"❌ [Arena Unregister]: {other.gameObject.name} ออกจากสมรภูมิ!");
                }
            }
        }
    }
}