using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutPosManager : MonoBehaviour
{
    [Header("Capture")]
    public bool isPlayerOccupy;
    public float captureTime = 5f;
    public GameObject outPosArea;

    // 🟢 NEW: ตัวแปรสำหรับเปลี่ยน Material ตามสถานะการยึดครอง
    [Header("Visuals")]
    public Material CapturedMat;
    public Material NonCapturedMat;

    // 🟢 NEW: เพิ่ม outpostId เพื่อบ่งบอกว่า Outpost ชิ้นนี้คืออะไร (เอาไว้เซฟกับ Load)
    public string outpostId = "outpost_default";

    [Header("Resource Production")]
    // 🟢 แก้ไขจุดที่ 1: เปลี่ยนมารองรับ SO_ItemData (ตัวใหม่ที่เก็บได้ทั้งแร่, อาวุธ, เกราะ, บ้าน)
    public SO_ItemData resourceToProduce;
    public int amountPerTick = 5; // ขุดได้ทีละกี่ชิ้น (เช่น หมุนส่งส่วย +5 ชิ้นทุกๆ 1 นาที)

    [Header("Enemy")]
    public List<GameObject> enemies = new();

    private int playerInside = 0;
    private Coroutine captureCoroutine;

    // 🟢 NEW: เช็คสถานะเริ่มต้นเมื่อเปิดเกม/โหลดซีน
    private void Start()
    {
        UpdateOutpostVisual();

        if (enemies != null)
        {
            foreach (GameObject enemy in enemies)
            {
                if (enemy == null) continue;

                // 🟢 เปลี่ยนจาก GetComponent เป็น GetComponentInChildren
                // คราวนี้ต่อให้อยู่ในวัตถุลูกขั่นไหน มันก็จะมุดไปหาจนเจอครับ
                EnemyBase enemyScript = enemy.GetComponentInChildren<EnemyBase>();

                if (enemyScript != null)
                {
                    enemyScript.myOutPos = this;
                    Debug.Log($"[Outpost] 🟢 เชื่อมต่อกับ {enemy.name} (ตรวจพบสคริปต์ที่วัตถุลูก) สำเร็จ!");
                }
                else
                {
                    Debug.LogError($"[Outpost] 🔴 หา EnemyBase บน {enemy.name} ไม่เจอ ลองเช็คดูว่าใส่สคริปต์ Melee หรือ Ranged ไว้หรือยังนะคร้าบ");
                }
            }
        }
    }

    #region Resource

    [ContextMenu("Test Claim To Inventory Directly")]
    public void ReturnResource()
    {
        if (!isPlayerOccupy) return;

        // ถ้าวันไหนอยากให้กดคุยกับสิ่งก่อสร้างนี้ในฉากแล้วได้ของยัดเข้าตัวทันที ก็ใช้ตัวนี้ได้ครับ
        if (ResourceInventory.Instance != null && resourceToProduce != null)
        {
            ResourceInventory.Instance.AddResource(resourceToProduce, amountPerTick);
        }
    }

    #endregion

    #region Capture

    void TryStartCapture()
    {
        if (isPlayerOccupy) return;
        if (captureCoroutine != null) return;
        if (playerInside <= 0) return;
        if (enemies.Count > 0) return;

        captureCoroutine = StartCoroutine(CaptureRoutine());
    }

    IEnumerator CaptureRoutine()
    {
        float timer = 0;
        while (timer < captureTime)
        {
            if (playerInside <= 0 || enemies.Count > 0)
            {
                captureCoroutine = null;
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        CaptureComplete();
        captureCoroutine = null;
    }

    void CaptureComplete()
    {
        isPlayerOccupy = true;
        Debug.Log($"[Outpost] ยึดฐานสำเร็จ! กำลังเชื่อมต่อระบบส่งส่วยทรัพยากร {resourceToProduce.itemName} เข้าสู่คลังกลาง...");

        // 🟢 NEW: อัปเดต Material ทันทีเมื่อยึดสำเร็จ
        UpdateOutpostVisual();

        // 🟢 แก้ไขจุดที่ 2: ยึดเสร็จปุ๊บ ยิงพิกัดส่งข้อมูลไปบอกคลังกลาง OutpostVaultManager ทันที!
        if (OutpostVaultManager.Instance != null && resourceToProduce != null)
        {
            // สร้างยอดขุดแร่รายนาทีเข้าระบบ
            OutpostVaultManager.OutpostProduction newProd = new OutpostVaultManager.OutpostProduction();
            newProd.item = resourceToProduce;
            newProd.amountPerTick = amountPerTick;

            // ส่งข้อมูลเข้าคลังสะสมกลาง คลังฐานจะเริ่มผลิตของให้เราอัตโนมัติเบื้องหลังทุกๆ 1 นาทีทันที!
            OutpostVaultManager.Instance.activeOutpostRates.Add(newProd);

            // 🟢 NEW: เพิ่ม outpostId ลงใน capturedOutpostIDs เพื่อบันทึกว่า Outpost นี้ยึดแล้ว
            OutpostVaultManager.Instance.capturedOutpostIDs.Add(outpostId);
            Debug.Log($"[Outpost] ✅ บันทึก outpostId: {outpostId} ลงรายชื่อที่ยึด");
        }
        else
        {
            Debug.LogError("ไม่พบ OutpostVaultManager ในฉาก! อย่าลืมเสกสร้าง Object นี้ไว้เบื้องหลังด้วยครับ");
        }

        // 🟢 NEW: เซฟเกมเมื่อยึด Outpost สำเร็จ
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame(true);
            Debug.Log("[Save] 💾 บันทึกข้อมูลการยึด Outpost ลงเครื่องสำเร็จ!");
        }

        // TODO : เปลี่ยนธง, เล่นเอฟเฟกต์ ฯลฯ
    }

    #endregion

    #region Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Unit"))
        {
            playerInside++;
            TryStartCapture();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Unit"))
        {
            playerInside = Mathf.Max(0, playerInside - 1);
        }
    }
    #endregion

    #region Enemy
    public void AddEnemy(GameObject enemy)
    {
        if (!enemies.Contains(enemy)) enemies.Add(enemy);
    }

    public void RemoveEnemy(GameObject enemy)
    {
        if (enemies.Remove(enemy))
        {
            TryStartCapture();
        }
    }
    #endregion

    // 🟢 NEW: ฟังก์ชันศูนย์กลางสำหรับอัปเดตสี/Material ตามสถานะปัจจุบัน
    public void UpdateOutpostVisual()
    {
        if (outPosArea == null) return;

        // ดึง MeshRenderer จาก outPosArea มาเปลี่ยน Material
        MeshRenderer renderer = outPosArea.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (isPlayerOccupy)
            {
                if (CapturedMat != null) renderer.material = CapturedMat;
            }
            else
            {
                if (NonCapturedMat != null) renderer.material = NonCapturedMat;
            }
        }
        else
        {
            Debug.LogWarning($"[Outpost] ไม่พบ MeshRenderer บน {outPosArea.name} ทำให้ไม่สามารถเปลี่ยน Material ได้");
        }
    }

    // 🟢 ปรับปรุงฟังก์ชันเดิมตามที่ต้องการ
    public void CaptureOutPos()
    {
        // สามารถเรียกฟังก์ชันนี้จากภายนอกเพื่อสลับสถานะแบบ Manual ได้เลย
        UpdateOutpostVisual();
    }

    public void OnEnemyDie()
    {
        enemies.RemoveAll(enemy => enemy == null);

        TryStartCapture();
    }
}