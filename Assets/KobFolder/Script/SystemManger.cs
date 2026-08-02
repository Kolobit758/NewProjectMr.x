using UnityEngine;

public class SystemManager : MonoBehaviour
{
    private static bool exists = false;

    void Awake()
    {
        if (exists) 
        { 
            Destroy(gameObject); // ถ้ามีตัวแม่โผล่มาอีก ให้ลบทิ้งทันที
            return; 
        }
        exists = true;
        
        // 🟢 สั่งให้ตัวแม่ (Prefab ทั้งก้อน) ไม่โดนทำลาย!
        DontDestroyOnLoad(gameObject); 
        Debug.Log("🛡️ KobSystemPrefab ย้ายเข้าโซนอมตะแล้วมึงกอบ!");
    }
}