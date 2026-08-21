using System.Collections.Generic;
using UnityEngine;

public class UnitBuffManager : MonoBehaviour
{
    private class ActiveBuff
    {
        public BaseBuffData data;
        public float remainingTime;
        public bool isInsideZone;

        public ActiveBuff(BaseBuffData data, bool isInsideZone)
        {
            this.data = data;
            this.isInsideZone = isInsideZone;
            // ดักจับ: ป้องกันกรณี duration ใน SO เป็น 0 หรือติดลบ ให้บัฟอยู่ขั้นต่ำ 0.1 วินาที
            this.remainingTime = Mathf.Max(0.1f, data.duration); 
        }
    }

    private Dictionary<BaseBuffData, ActiveBuff> activeBuffs = new Dictionary<BaseBuffData, ActiveBuff>();
    private List<BaseBuffData> buffsToRemove = new List<BaseBuffData>();

    public void AddOrRefreshBuff(BaseBuffData buffData, bool isInsideZone)
    {
        if (buffData == null) return;

        if (activeBuffs.TryGetValue(buffData, out var activeBuff))
        {
            activeBuff.isInsideZone = isInsideZone;
            activeBuff.remainingTime = Mathf.Max(0.1f, buffData.duration); // รีเฟรชเวลา
            Debug.Log($"[Buff Manager] 🔄 รีเฟรชบัฟเดิม: {buffData.name} | แช่ในโซน: {isInsideZone}");
        }
        else
        {
            ActiveBuff newBuff = new ActiveBuff(buffData, isInsideZone);
            activeBuffs.Add(buffData, newBuff);
            buffData.ApplyBuff(gameObject);
            Debug.Log($"[Buff Manager] 🟢 ได้รับบัฟใหม่: {buffData.name} | เวลา: {newBuff.remainingTime} วินาที | แช่ในโซน: {isInsideZone}");
        }
    }

    public void NotifyLeftZone(BaseBuffData buffData)
    {
        if (buffData == null) return;

        if (activeBuffs.TryGetValue(buffData, out var activeBuff))
        {
            activeBuff.isInsideZone = false;
            activeBuff.remainingTime = Mathf.Max(0.1f, buffData.duration); // เริ่มนับถอยหลังจริงเมื่อเดินออก
            Debug.Log($"[Buff Manager] 🏃 เดินออกจากโซนบัฟ: {buffData.name} | เริ่มนับถอยหลังจริง!");
        }
    }

    private void Update()
    {
        if (activeBuffs.Count == 0) return;

        buffsToRemove.Clear();

        foreach (var kvp in activeBuffs)
        {
            var buff = kvp.Value;
            
            // 🚨 จุดต้องสงสัยที่ 1: ถ้ายืนแช่ในโซน เวลาจะไม่ลด
            if (buff.isInsideZone) continue;

            buff.remainingTime -= Time.deltaTime;
            
            if (buff.remainingTime <= 0)
            {
                buffsToRemove.Add(kvp.Key);
            }
        }

        // ลบบัฟที่หมดเวลา
        foreach (var buffData in buffsToRemove)
        {
            if (buffData != null)
            {
                buffData.RemoveBuff(gameObject);
                activeBuffs.Remove(buffData);
                Debug.Log($"[Buff Manager] 🔴 บัฟหมดเวลา ทำการลบออกสำเร็จ: {buffData.name}");
            }
        }
    }

    private void OnDisable()
    {
        // เซฟตี้ด่านสุดท้าย: ป้องกันตัวละครตาย/หายไปแล้วค่าสเตตัสค้าง
        foreach (var kvp in activeBuffs)
        {
            if (kvp.Key != null) kvp.Key.RemoveBuff(gameObject);
        }
        activeBuffs.Clear();
    }
}