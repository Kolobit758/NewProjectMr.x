using FischlWorks_FogWar;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder; // ใช้ NavMesh ช่วยคำนวณทางเดินของแสง

public class AncientBossTracker : MonoBehaviour
{
    [Header("Boss Tracking Settings")]
    public Transform bossSpawnPoint;     // จุดที่บอสปลาหมึกอยู่
    public GameObject beaconLightVisual; // แสงที่พุ่งขึ้นฟ้าบนแท่น (เปิด/ปิดตามเนื้อเรื่อง)
    public GameObject trailParticle; // ตัว Particle แสงที่จะวิ่งนำทางผู้เล่น

    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public Transform playerTransform;
    private bool isReadyToTrack = false; // เอาไว้เปิดตอนที่เนื้อเรื่องถึงกำหนด

    public bool isPlayerinArea;

    void Start()
    {
        // หาตัวผู้เล่นในฉาก (หรือจะใช้ Instance ของผู้เล่นดื้อ ๆ ก็ได้)
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;

        isPlayerinArea = false;

        // เริ่มเกมมา อาจจะยังไม่พร้อมให้ตามรอยบอส (ปิดแสงไว้ก่อน)
        // SetTrackerStatus(false);
    }

    public void Update()
    {
        if (isPlayerinArea == true)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("Start track boss");
                TriggerBossTracking();
            }
        }
    }


    // ฟังก์ชันเปิด/ปิดระบบจากสคริปต์คุมเนื้อเรื่องกลาง (GameManager)
    [ContextMenu("SetTriggerStat")]
    public void SetTrueTrackerStat()
    {
        SetTrackerStatus(true);
    }
    public void SetTrackerStatus(bool ready)
    {
        isReadyToTrack = ready;
        if (beaconLightVisual != null)
        {
            beaconLightVisual.SetActive(ready); // เปิดแสงพุ่งขึ้นฟ้าให้ผู้เล่นเห็นแต่ไกล
        }
    }

    private void TriggerBossTracking()
    {
        if (bossSpawnPoint == null || trailParticle == null) return;

        Debug.Log("🔮 โโบราณสถานตอบรับ! กำลังส่งพลังงานนำทางไปหาบอสปลาหมึก...");

        // คำนวณเส้นทางเดินจากแท่นไปหาบอสโดยใช้ NavMesh เพื่อให้แสงเลื้อยไปตามถนน ไม่ทะลุกำแพง
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, bossSpawnPoint.position, NavMesh.AllAreas, path))
        {
            // สั่งให้สคริปต์คุม Particle เคลื่อนที่ไปตามจุด (waypoints) ใน path.corners
            SpawnTrackingParticle(path.corners);
        }
    }

    private void SpawnTrackingParticle(Vector3[] waypoints)
    {
        // ยิงอินสแตนซ์แสงนำทางออกมา แล้วส่งอาเรย์เส้นทางไปให้มันวิ่งตาม
        GameObject p = Instantiate(trailParticle, transform.position, Quaternion.identity);

        // (สร้างสคริปต์ย่อยแปะที่ตัว Particle ให้มันวิ่งย้ายตำแหน่งทีละจุดจนถึงจุดบอส)
        var follower = p.gameObject.AddComponent<ParticlePathFollower>();
        follower.SetupPath(waypoints, bossSpawnPoint.position);
    }

    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hello Player");

        if(other.gameObject.CompareTag("Player")){isPlayerinArea = true;}
        

    }

    public void OnTriggerExit(Collider other)
    {
        Debug.Log("Bye Player");

        if(other.gameObject.CompareTag("Player")){isPlayerinArea = false;}
    }


}