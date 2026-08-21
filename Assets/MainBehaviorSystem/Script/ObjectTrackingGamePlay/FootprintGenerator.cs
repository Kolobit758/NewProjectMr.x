using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class FootprintGenerator : MonoBehaviour
{
    public static FootprintGenerator Instance { get; private set; }

    [Header("Prefabs & Layers")]
    public GameObject footprintNodePrefab; 
    public LayerMask groundLayer;

    [Header("Manual Setup (Inspector)")]
    public Transform manualStartPoint;
    public Transform manualEndPoint;

    [Header("Generator Settings")]
    public int footprintCount = 10;
    public float curveIntensity = 5f;

    // 🟢 โครงสร้างข้อมูลขนาดเล็กสำหรับเก็บค่าพิกัดและมุมเอียงรันไทม์
    [System.Serializable]
    public struct TrailPointData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 🟢 ฟังก์ชันคำนวณเส้นทางโค้ง Bezier คืนค่ากลับไปเฉพาะ "ข้อมูลพิกัดตำแหน่ง" ใน RAM เท่านั้น (ยังไม่เสกวัตถุ)
    /// </summary>
    public List<TrailPointData> CalculateSplinePositions(Vector3 startPoint, Vector3 endPoint)
    {
        List<TrailPointData> pointList = new List<TrailPointData>();
        List<Vector3> bezierPoints = new List<Vector3>();

        // 1. คำนวณจุดควบคุมความโค้งตรงกลาง (Control Point สำหรับดัดทางโค้ง)
        Vector3 midPoint = Vector3.Lerp(startPoint, endPoint, 0.5f);
        Vector3 perpendicularDir = Vector3.Cross((endPoint - startPoint).normalized, Vector3.up).normalized;
        float randomOffset = Random.Range(-curveIntensity, curveIntensity);
        Vector3 controlPoint = midPoint + (perpendicularDir * randomOffset);

        // 2. คำนวณพิกัดแต่ละโหนดและทิ่มลงแนบพื้นผิว NavMesh / Terrain
        for (int i = 0; i < footprintCount; i++)
        {
            float t = (float)i / (footprintCount - 1);
            
            // สมการกำลังสอง Quadratic Bezier Curve
            Vector3 rawBezierPos = Mathf.Pow(1f - t, 2f) * startPoint + 
                                   2f * (1f - t) * t * controlPoint + 
                                   Mathf.Pow(t, 2f) * endPoint;

            Vector3 finalGroundPos = rawBezierPos;
            
            // ตรวจสอบและดูดติดพื้นผิวทางเดินจริง
            if (NavMesh.SamplePosition(rawBezierPos, out NavMeshHit navHit, 4f, NavMesh.AllAreas))
            {
                finalGroundPos = navHit.position;
            }
            else if (Physics.Raycast(rawBezierPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 15f, groundLayer))
            {
                finalGroundPos = hit.point;
            }

            bezierPoints.Add(finalGroundPos);
        }

        // 3. บันทึกข้อมูลตำแหน่งพร้อมคำนวณมุมหันหน้า (Rotation) ของรอยเท้าไปตามแนวโค้ง
        for (int i = 0; i < bezierPoints.Count; i++)
        {
            TrailPointData data = new TrailPointData();
            data.position = bezierPoints[i];

            if (i < bezierPoints.Count - 1)
            {
                Vector3 forwardDir = (bezierPoints[i + 1] - bezierPoints[i]).normalized;
                data.rotation = forwardDir != Vector3.zero ? Quaternion.LookRotation(forwardDir) : Quaternion.identity;
            }
            else
            {
                // จุดสุดท้ายหันตรงเข้าหาเป้าหมายปลายทาง
                Vector3 finalDir = (endPoint - startPoint).normalized;
                data.rotation = finalDir != Vector3.zero ? Quaternion.LookRotation(finalDir) : Quaternion.identity;
            }

            pointList.Add(data);
        }

        return pointList;
    }

    // 🛠️ ปุ่มกดทดสอบระบบผ่านหน้าต่าง Inspector (เรียกใช้ขณะกดเล่นเกมอยู่)
    [ContextMenu("Test Calculate & Dynamic Spawn")]
    public void TestCalculateAndSpawn()
    {
        if (manualStartPoint == null || manualEndPoint == null || footprintNodePrefab == null)
        {
            Debug.LogError("[Generator] ❌ ไม่สามารถทดสอบได้ กรุณาใส่จุดอ้างอิงให้ครบถ้วนใน Inspector!");
            return;
        }

        List<TrailPointData> testPoints = CalculateSplinePositions(manualStartPoint.position, manualEndPoint.position);
        
        // ทดสอบจำลองการเสกโชว์พิกัดทั้งหมดขึ้นมาดูความโค้ง
        foreach (var pt in testPoints)
        {
            GameObject testGo = Instantiate(footprintNodePrefab, pt.position, pt.rotation);
            FootprintNode nodeScript = testGo.GetComponent<FootprintNode>();
            if (nodeScript != null) nodeScript.RevealNode();
        }
        
        Debug.Log($"[Generator] 🐾 ทดสอบคำนวณและกางเส้นทางสำเร็จ! จำนวนรอยเท้า: {testPoints.Count} จุด");
    }
}