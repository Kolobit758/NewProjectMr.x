using UnityEngine;
using System.Collections.Generic;

public class OutpostRaidSystem : MonoBehaviour
{
    [Header("Raid Configuration")]
    public float raidCheckInterval = 30f;
    private float raidTimer = 0f;
    [Range(0f, 100f)] public float raidChance = 25f;
    [Header("Bandit Variety")]
    public List<GameObject> banditPrefabs = new List<GameObject>(); // ลากโจรหลายแบบมาใส่ตรงนี้
    // ใน OutpostRaidSystem.cs
    private void Start()
    {
        // สมัครสมาชิกเพื่อฟัง Event กลางวันกลางคืน (วิธีนี้โค้ดจะวิ่งแค่ตอนเวลาเปลี่ยน ไม่ต้องเช็คใน Update ตลอดเวลา)
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnTimeChanged += HandleTimeChanged;
        }
    }

    private void HandleTimeChanged(bool isNight)
    {
        if (isNight)
        {
            Debug.Log("🌙 กลางคืนแล้ว! โจรเริ่มสุ่มบุกค่าย...");
        }
    }


    void Update()
    {
        // สุ่มบุกตอนกลางคืนตามระเบียบ
        if (DayNightManager.Instance != null && DayNightManager.Instance.isNightTime)
        {
            raidTimer += Time.deltaTime;
            if (raidTimer >= raidCheckInterval)
            {
                raidTimer = 0f;
                TryTriggerRandomRaid();
            }
        }
    }

    private void TryTriggerRandomRaid()
    {
        if (OutpostVaultManager.Instance == null) return;

        List<OutpostDataSO> captured = OutpostVaultManager.Instance.allOutpostsInWorld.FindAll(o => o.isCaptured && o.garrisonUnits.Count > 0);
        if (captured.Count == 0) return;

        if (Random.Range(0f, 100f) <= raidChance)
        {
            int randomIndex = Random.Range(0, captured.Count);
            ResolveBattle(captured[randomIndex]);
        }
    }

    [ContextMenu("Force Bandit Attack North Outpost")]
    public void ForceAttack()
    {
        if (OutpostVaultManager.Instance != null && OutpostVaultManager.Instance.allOutpostsInWorld.Count > 0)
        {
            ResolveBattle(OutpostVaultManager.Instance.allOutpostsInWorld[1]);
        }
    }

    private void ResolveBattle(OutpostDataSO outpost)
    {
        // 🟢 สุ่มเลือกโจรแบบที่จะบุกรอบนี้ออกมา 1 แบบก่อน
        GameObject chosenBanditPrefab = banditPrefabs[Random.Range(0, banditPrefabs.Count)];

        // ดึงพลังโจมตีจากตัวที่สุ่มได้ (หรือใช้ค่าสุ่มถ้าไม่มี Stat ในตัว Prefab)
        float banditPower = Random.Range(30f, 80f);
        float playerPower = outpost.GetTotalGarrisonAttack();

        playerPower *= Random.Range(0.8f, 1.2f);
        banditPower *= Random.Range(0.8f, 1.2f);

        if (playerPower >= banditPower)
        {
            if (outpost.garrisonUnits.Count > 0)
            {
                int dmg = Random.Range(10, 25);
                outpost.garrisonUnits[0].currentHP -= dmg;
                Debug.Log($"🔹 [ชนะศึก]: ค่ายรอด! {outpost.garrisonUnits[0].customName} เลือดหักลบไป {dmg}");

                if (outpost.garrisonUnits[0].currentHP <= 0)
                {
                    Debug.LogError($"💀 ยูนิต {outpost.garrisonUnits[0].customName} ตายในหน้าที่!");
                    outpost.garrisonUnits.RemoveAt(0);
                }
            }
        }
        else
        {
            // 💥 ค่ายแตก!
            outpost.garrisonUnits.Clear();
            outpost.isCaptured = false;

            OutPosManager outPosManager = FindOutpostManagerInScene(outpost.outpostID);
            if (outPosManager != null)
            {
                outPosManager.isPlayerOccupy = false;
                // 🟢 ส่ง chosenBanditPrefab เข้าไปที่ฟังก์ชันเสกโจร
                SpawnBanditsAtOutpost(outpost, outPosManager);
            }
            else
            {
                outpost.isCaptured = false;
            }

            if (OutpostVaultManager.Instance != null)
            {
                OutpostVaultManager.Instance.capturedOutpostIDs.Remove(outpost.outpostID);
            }
            Debug.LogError($"💥 [ค่ายแตก!]: โจรยึด [{outpost.outpostName}] คืนสำเร็จ!");
        }

        // 💾 บรรทัดไม้ตาย: คำนวณรบเสร็จ ทหารตายหรือค่ายแตก สั่งบันทึกไฟล์เซฟทับทันที!
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame(true);
        }

        // สั่งอัปเดตหน้าตา Visual และรีสปอว์นทหารที่เหลืออยู่ในฉากทันที
        OutPosManager sceneManager = FindOutpostManagerInScene(outpost.outpostID);
        if (sceneManager != null) sceneManager.UpdateOutpostVisual();

        OutpostGarrisonVisualizer visualizer = FindAnyObjectByType<OutpostGarrisonVisualizer>();
        if (visualizer != null && visualizer.outpostData.outpostID == outpost.outpostID) visualizer.SpawnGarrisonUnitsInScene();
    }

    private OutPosManager FindOutpostManagerInScene(string id)
    {
        OutPosManager[] managers = FindObjectsByType<OutPosManager>(FindObjectsSortMode.None);
        return System.Array.Find(managers, m => m.outpostData != null && m.outpostData.outpostID == id);
    }

    #region After thief capture outpos

    private void SpawnBanditsAtOutpost(OutpostDataSO outpost, OutPosManager sceneManager)
    {
        // โยนงานการเสกไปให้ Manager จัดการตาม Config ที่ตั้งไว้ในตัวค่ายนั้นๆ
        sceneManager.SpawnBanditsByConfig();

        Debug.Log($"💀 [EVENT]: กองโจรบุกค่าย [{outpost.outpostName}] ตามแผน Tactical!");
    }
    #endregion
}