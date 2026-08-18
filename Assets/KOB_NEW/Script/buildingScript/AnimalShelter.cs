using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;

public class AnimalShelter : MonoBehaviour, ITaskable
{
    [Header("Shelter Settings")]
    public string shelterName = "โรงเก็บสัตว์";
    public int animalCapacityBonus = 4; // สร้าง 1 หลัง เพิ่มความจุ 4 ตัว

    [Header("Assigned Units")]
    // 🟢 เปลี่ยนมารองรับ UnitBase (เพราะหลังจาก Tame แล้ว มันแปลงร่างเป็น UnitBase เรียบร้อย)
    public List<UnitBase> shelteredUnits = new List<UnitBase>();

    [Header("Sleep Positions")]
    public Transform[] sleepPoints;
    public Transform wakeUpPoint;


    public Canvas canvas;
    private bool isCurrentlyNight = false; // 🟢 เก็บสถานะกลางคืน/กลางวันปัจจุบัน

    // 🌐 [Static Manager] รวมความจุโรงสัตว์ทั้งหมดในฉาก
    public static List<AnimalShelter> allShelters = new List<AnimalShelter>();


    void Awake()
    {
        if (!allShelters.Contains(this)) allShelters.Add(this);
    }

    void OnDestroy()
    {
        if (allShelters.Contains(this)) allShelters.Remove(this);

        if (DayNightManager.Instance != null)
        {
            // DayNightManager.Instance.OnTimeChanged -= HandleTimeChanged;
        }
    }

    void Start()
    {
        if (DayNightManager.Instance != null)
        {
            // DayNightManager.Instance.OnTimeChanged += HandleTimeChanged;
            isCurrentlyNight = DayNightManager.Instance.isNightTime; // ปรับชื่อ property ตามจริงที่ DayNightManager มี
        }

        canvas.worldCamera = FindAnyObjectByType<Camera>();

    }

    // 🧮 คำนวณความจุรวมทั้งหมดจากโรงสัตว์ทุกหลังในฉาก (เช่น มี 2 หลัง = ความจุรวม 8 ตัว)
    public static int GetTotalCapacity()
    {
        int total = 0;
        foreach (var shelter in allShelters)
        {
            if (shelter != null) total += shelter.animalCapacityBonus;
        }
        return total + SpawnAllUnit.Instance.starterCapacity;
    }

    // � นับจำนวนยูนิตที่ถูก spawn จริง ๆ ทั่วทั้งฉาก ไม่ใช่แค่ที่อยู่ในคอก
    public static int GetCurrentUnitCount()
    {
        if (RTS_movement.instance == null || RTS_movement.instance.allUnits == null)
        {
            int fallback = 0;
            foreach (var shelter in allShelters)
            {
                if (shelter != null) fallback += shelter.shelteredUnits.Count;
            }
            return fallback;
        }

        int currentTotalAnimals = 0;
        foreach (var unit in RTS_movement.instance.allUnits)
        {
            if (unit != null && unit.gameObject != null) currentTotalAnimals++;
        }
        return currentTotalAnimals;
    }


    public static string GetPopulationText()
    {
        return $"{GetCurrentUnitCount()}/{GetTotalCapacity()}";
    }

    // 📥 หาโรงสัตว์ที่ยังว่างอยู่เพื่อยัดสัตว์เข้าคอก
    public static AnimalShelter GetAvailableShelter()
    {
        foreach (var shelter in allShelters)
        {
            if (shelter != null && shelter.shelteredUnits.Count < shelter.animalCapacityBonus)
            {
                return shelter;
            }
        }
        return null; // เต็มทุกหลัง!
    }

    public void RegisterUnit(UnitBase unit)
    {
        if (!shelteredUnits.Contains(unit))
        {
            shelteredUnits.Add(unit);
            Debug.Log($"🏠 [Animal Shelter]: ยูนิตสัตว์ {unit.name} เข้าคอกเรียบร้อย ({shelteredUnits.Count}/{animalCapacityBonus})");
        }
    }



    // 🟢 Coroutine คอยเช็คว่าเดินถึงเตียงหรือยัง พึงถึงแล้วค่อยสั่งหยุด
    private IEnumerator WaitAndStopWhenReached(UnitBase unit, NavMeshAgent agent, Vector3 targetPos)
    {
        yield return new WaitForEndOfFrame();
        while (agent.pathPending) yield return null;

        while (agent.enabled && Vector3.Distance(unit.transform.position, targetPos) > agent.stoppingDistance + 0.2f)
        {
            yield return null;
        }

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.enabled = false; // 🔒 ปิด NavMeshAgent จริงๆ คุมไม่ได้จนกว่าจะปลดล็อกตอนเช้า
            Debug.Log($"🛌 [Shelter]: {unit.name} ถึงเตียงและหลับสนิทแล้ว (คุมไม่ได้จนกว่าจะตื่น)");
        }
    }

    // 🔍 เช็คว่าตอนนี้จำนวนยูนิตทั้งหมด (รวมตัวที่อยู่ในฉาก + ตัวที่กำลังผลิต/รอในคิวของทุกตึก) ถึงความจุแล้วหรือยัง
    public static bool IsTotalCapacityFull()
    {
        return GetTotalPendingUnitCount() >= GetTotalCapacity();
    }

    // 📊 นับจำนวนยูนิตจริงในฉาก + จำนวนที่กำลังผลิต/รอคิวอยู่ในตึกสร้างทั้งหมด
    public static int GetTotalPendingUnitCount()
    {
        int totalCount = GetCurrentUnitCount();

        // วิ่งไปเช็คตึกผลิตทุกตึกในฉากเพื่อบวกยูนิตที่กำลัง process หรืออยู่ในคิวเพิ่ม
        UnitProducerBuilding[] allProducers = Object.FindObjectsByType<UnitProducerBuilding>(FindObjectsSortMode.None);
        foreach (var producer in allProducers)
        {
            if (producer != null)
            {
                totalCount += producer.GetPendingCount();
            }
        }

        return totalCount;
    }





    public void OnUnitInteract(UnitBase unit) { }
    public void OnUnitExit(UnitBase unit) { }
    public Vector3 GetInteractionPoint() => transform.position + transform.forward * 2f;
}