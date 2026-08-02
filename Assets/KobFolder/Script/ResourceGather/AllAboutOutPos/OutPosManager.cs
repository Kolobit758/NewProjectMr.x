using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BanditSquadConfig
{
    public int tankCount = 1;
    public int meleeCount = 2;
    public int rangedCount = 1;
}

public class OutPosManager : MonoBehaviour
{
    [Header("Squad Setup")]
    public BanditSquadConfig squadConfig; // กำหนดจำนวนใน Inspector ได้เลย!
    public List<GameObject> tankPrefabs;
    public List<GameObject> meleePrefabs;
    public List<GameObject> rangedPrefabs;
    [Header("Data Link")]
    // 🟢 NEW: ลากก้อน SO ประจำค่ายนี้มาแปะตรงนี้ (มันจะทำหน้าที่ถือข้อมูลแทนตัวแปรลอย ๆ)
    public OutpostDataSO outpostData;

    [Header("Capture")]
    // 🟢 ปรับปรุง: เปลี่ยนโปรเพอร์ตี้ให้ไปอ่าน/เขียนที่ SO โดยตรง เพื่อให้เปลี่ยนซีนแล้วค่าไม่หาย!
    public bool isPlayerOccupy
    {
        get => outpostData != null ? outpostData.isCaptured : false;
        set
        {
            if (outpostData != null)
            {
                outpostData.isCaptured = value;
                // 🟢 เพิ่มบรรทัดนี้เข้าไป! ให้มันเรียกอัปเดตหน้าตาตัวเองทันทีที่โดนเปลี่ยนค่า
                UpdateOutpostVisual();

                // ถ้ามึงอยากให้มันเคลียร์ศัตรู หรือทำอะไรตอนถูกยึดก็สั่งตรงนี้ได้เลย
                if (value == true) SetUpGame();
            }
        }
    }

    public float captureTime = 5f;
    public GameObject outPosArea;

    [Header("Visuals")]
    public Material CapturedMat;
    public Material NonCapturedMat;

    // 🟢 ปรับปรุง: ดึงค่า id มาจาก SO ตรงๆ ไม่ต้องพิมพ์แยกให้หลงลูป
    public string outpostId => outpostData != null ? outpostData.outpostID : "outpost_default";

    [Header("Resource Production")]
    // 🟢 ปรับปรุง: ดึงข้อมูลแร่ที่จะผลิตจาก SO ส่วนกลาง
    public SO_ItemData resourceToProduce => outpostData != null ? outpostData.resourceToProduce : null;
    public int amountPerTick => outpostData != null ? outpostData.amountPerTick : 5;

    [Header("Enemy")]
    public List<GameObject> enemies = new();

    private int playerInside = 0;
    private Coroutine captureCoroutine;
    [Header("UI Interaction")]
    public string targetOutpostIdForUI = "North_01"; // เอาไว้แมตช์ระบุว่าค่ายนี้คืออันไหน

    private void Start()
    {
        UpdateOutpostVisual();

        if (enemies != null)
        {
            foreach (GameObject enemy in enemies)
            {
                if (enemy == null) continue;
                EnemyBase enemyScript = enemy.GetComponentInChildren<EnemyBase>();
                if (enemyScript != null)
                {
                    enemyScript.myOutPos = this;
                    Debug.Log($"[Outpost] 🟢 เชื่อมต่อกับ {enemy.name} สำเร็จ!");
                }
            }
        }

        SetUpGame();
    }

    public void SetUpGame()
    {
        SpawnBanditsByConfig();

        if (isPlayerOccupy)
        {
            foreach (GameObject enemy in enemies)
            {
                Destroy(enemy);
            }

            enemies.RemoveAll(e => e == null);

        }


    }

    #region Resource
    [ContextMenu("Test Claim To Inventory Directly")]
    public void ReturnResource()
    {
        if (!isPlayerOccupy) return;

        if (ResourceInventory.Instance != null && resourceToProduce != null)
        {
            ResourceInventory.Instance.AddResource(resourceToProduce, amountPerTick);
        }
    }
    #endregion

    #region Capture
    void TryStartCapture()
    {
        // 🟢 ทำความสะอาดลิสต์ทุกครั้งก่อนจะประมวลผลเริ่มนับเวลายึดฐาน
        enemies.RemoveAll(enemy => enemy == null);

        if (isPlayerOccupy) return;
        if (captureCoroutine != null) return;
        if (playerInside <= 0) return;
        if (enemies.Count > 0) return; // ถ้าลิสต์คลีนแล้วเหลือ 0 จริง จะวิ่งทะลุผ่านตรงนี้ไปเริ่มลูป!

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
        if (outpostData == null) return;

        // 🟢 1. สลักสถานะยึดครองลงก้อน SO และตั้งค่าความหิวเริ่มต้น
        isPlayerOccupy = true;
        outpostData.hungerLevel = 100f;

        Debug.Log($"[Outpost] ยึดฐานสำเร็จ! [{outpostData.outpostName}] กำลังเชื่อมต่อระบบส่งส่วย...");

        UpdateOutpostVisual();

        // 🟢 2. ยิงยอดขุดแร่เข้า OutpostVaultManager 
        if (OutpostVaultManager.Instance != null && resourceToProduce != null)
        {
            OutpostVaultManager.OutpostProduction newProd = new OutpostVaultManager.OutpostProduction();
            newProd.item = resourceToProduce;
            newProd.amountPerTick = amountPerTick;

            OutpostVaultManager.Instance.activeOutpostRates.Add(newProd);

            if (!OutpostVaultManager.Instance.capturedOutpostIDs.Contains(outpostId))
            {
                OutpostVaultManager.Instance.capturedOutpostIDs.Add(outpostId);
            }
        }

        // 💾 3. สั่งเซฟเกมลงเครื่องผ่าน SaveLoadManager ตัวเดิมของมึง
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame(true);
            Debug.Log("[Save] 💾 บันทึกข้อมูลการยึด Outpost ลงเครื่องสำเร็จ!");
        }
    }
    #endregion

    #region Trigger & Enemy (คงเดิมไว้ 100%)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Unit"))
        {
            playerInside++;
            TryStartCapture();

            if (isPlayerOccupy)
            {
                OpenOutpostGarrisonUI();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Unit"))
        {
            playerInside = Mathf.Max(0, playerInside - 1);
        }
    }

    public void AddEnemy(GameObject enemy)
    {
        if (!enemies.Contains(enemy)) enemies.Add(enemy);
    }


    public void OnEnemyDie()
    {
        enemies.RemoveAll(enemy => enemy == null);
        TryStartCapture();
    }
    #endregion

    public void UpdateOutpostVisual()
    {
        if (outPosArea == null) return;

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
    }

    public void CaptureOutPos()
    {
        UpdateOutpostVisual();
    }


    public void OpenOutpostGarrisonUI()
    {
        if (!isPlayerOccupy)
        {
            Debug.LogWarning("❌ ค่ายนี้ยังเป็นของโจรอยู่มึงกอบ! ยึดให้เสร็จก่อนค่อยส่งทหารมาเฝ้า!");
            return;
        }

        if (OutpostUIController.Instance != null)
        {
            // ส่งข้อมูลค่ายปัจจุบันไปให้ UI เปิดแสดงผล
            OutpostUIController.Instance.OpenPanel(outpostData);
        }
        else
        {
            Debug.LogError("🔴 หา OutpostUIController ไม่เจอในฉาก! อย่าลืมสร้าง UI Canvas มารองรับนะมึง");
        }
    }

    #region Tactaic Enemy
    public void SpawnBanditsByConfig()
    {
        SpawnSpecificBandits(EnemyRole.Tank, squadConfig.tankCount, tankPrefabs);
        SpawnSpecificBandits(EnemyRole.Melee, squadConfig.meleeCount, meleePrefabs);
        SpawnSpecificBandits(EnemyRole.Ranged, squadConfig.rangedCount, rangedPrefabs);

        Debug.Log("Creat Bandit");
        // จัดแถวทันทีหลังเสกเสร็จ
        UpdateSquadFormation();
    }
    private void SpawnSpecificBandits(EnemyRole role, int count, List<GameObject> prefabs)
    {
        if (prefabs == null || prefabs.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            Vector3 randomPos = transform.position + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));

            GameObject bandit = Instantiate(prefab, randomPos, Quaternion.identity);
            EnemyBase enemyScript = bandit.GetComponentInChildren<EnemyBase>();

            if (enemyScript != null)
            {
                enemyScript.role = role; // บังคับให้เป็น Role นี้เลย
                enemyScript.myOutPos = this;
                AddEnemy(bandit);
            }
        }
    }
    // เพิ่มฟังก์ชันนี้ใน OutPosManager
    public void UpdateSquadFormation()
    {
        enemies.RemoveAll(e => e == null);
        if (enemies.Count == 0) return;

        List<EnemyBase> tanks = new();
        List<EnemyBase> melees = new();
        List<EnemyBase> ranged = new();

        foreach (var e in enemies)
        {
            var script = e.GetComponentInChildren<EnemyBase>();
            if (script == null) continue;
            if (script.role == EnemyRole.Tank) tanks.Add(script);
            else if (script.role == EnemyRole.Ranged) ranged.Add(script);
            else melees.Add(script);
        }

        // Tank: อยู่หน้าผู้เล่น
        foreach (var t in tanks) t.SetTacticalPosition(new Vector3(0, 0, 1.5f));

        // ตรงส่วน Melee
        for (int i = 0; i < melees.Count; i++)
        {
            // เพิ่ม Time.time เข้าไปเพื่อให้ค่ามันแกว่งตามเวลา
            float time = Time.time * 0.5f;
            float angle = (i * (360f / Mathf.Max(1, melees.Count))) * Mathf.Deg2Rad + time;
            melees[i].SetTacticalPosition(new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 2.5f);
        }
        

        // Ranged: ยืนห่างออกไป
        foreach (var r in ranged) r.SetTacticalPosition(new Vector3(0, 0, -7f));
    }

    // 🟢 แก้ไขฟังก์ชัน RemoveEnemy ให้เรียก UpdateSquadFormation
    public void RemoveEnemy(GameObject enemy)
    {
        if (enemies.Contains(enemy)) enemies.Remove(enemy);
        enemies.RemoveAll(enemy => enemy == null);


        if (enemies.Count <= 0) TryStartCapture();
    }
    #endregion

}