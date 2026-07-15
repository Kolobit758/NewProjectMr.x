using UnityEngine;
using System.Collections.Generic;

public class OutpostRaidSystem : MonoBehaviour
{
    [Header("Raid Configuration")]
    public float raidCheckInterval = 30f; 
    private float raidTimer = 0f;
    [Range(0f, 100f)] public float raidChance = 25f; 

    void Update()
    {
        // สุ่มบุกตอนกลางคืนตามระเบียบ
        if (MotherTreeController.Instance != null && MotherTreeController.Instance.isNightTime)
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
            ResolveBattle(OutpostVaultManager.Instance.allOutpostsInWorld[0]);
        }
    }

    private void ResolveBattle(OutpostDataSO outpost)
    {
        Debug.LogWarning($"⚔🚨 [EVENT]: กองโจรโจมตี [{outpost.outpostName}] เบื้องหลัง!");

        float banditPower = Random.Range(30f, 80f);
        float playerPower = outpost.GetTotalGarrisonAttack(); // ดึงพลังทหารจริง + ค่าหิวจาก SO

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
            // ค่ายแตก! โจรยึดคืน ล้างบางทหาร
            outpost.garrisonUnits.Clear();
            outpost.isCaptured = false;

            if (OutpostVaultManager.Instance != null)
            {
                OutpostVaultManager.Instance.capturedOutpostIDs.Remove(outpost.outpostID);
            }
            Debug.LogError($"💥 [ค่ายแตก!]: โจรบุกยึด [{outpost.outpostName}] คืนสำเร็จ ส่วยแร่หยุดทำงาน!");
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
}