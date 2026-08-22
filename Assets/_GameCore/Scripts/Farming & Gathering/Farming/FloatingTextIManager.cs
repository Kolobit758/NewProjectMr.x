using UnityEngine;
using TMPro;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }
    
    [Header("Prefab ที่มี Script FloatingTextItem และ TextMeshProUGUI")]
    public GameObject floatingTextPrefab; 

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ShowText(Vector3 worldPos, string text, Color color)
    {
        if (floatingTextPrefab == null) return;

        // สร้าง Object จาก Prefab ในตำแหน่งโลก (World Position) ของแปลงผัก
        GameObject obj = Instantiate(floatingTextPrefab, worldPos, Quaternion.identity);
        
        // ดึง Script ที่จัดการข้อความแล้วสั่ง Initialize ข้อความและสี
        FloatingTextItem item = obj.GetComponent<FloatingTextItem>();
        if (item != null)
        {
            item.Initialize(text, color);
        }
    }
}