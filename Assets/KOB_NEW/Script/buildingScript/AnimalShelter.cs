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
        UpdateCanvasVisibility();
    }

    // 🧮 คำนวณความจุรวมทั้งหมดจากโรงสัตว์ทุกหลังในฉาก (เช่น มี 2 หลัง = ความจุรวม 8 ตัว)
    public static int GetTotalCapacity()
    {
        int total = 0;
        foreach (var shelter in allShelters)
        {
            if (shelter != null) total += shelter.animalCapacityBonus;
        }
        return total;
    }

    // 🔍 เช็คว่าตอนนี้จำนวนสัตว์ทั้งหมดในคอกเต็มหรือยัง
    public static bool IsTotalCapacityFull()
    {
        int currentTotalAnimals = 0;
        foreach (var shelter in allShelters)
        {
            if (shelter != null) currentTotalAnimals += shelter.shelteredUnits.Count;
        }
        return currentTotalAnimals >= GetTotalCapacity();
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

    // เมื่อเปลี่ยนเวลาเป็นกลางวัน ให้เช็คปลุกยูนิตที่นอนกลางวันเสร็จ หรือจัดการยูนิตที่อดนอน
    // private void HandleTimeChanged(bool isNight)
    // {
    //     isCurrentlyNight = isNight; // 🟢 บันทึกสถานะเวลาไว้ก่อน

    //     if (isNight)
    //     {
    //         // CommandUnitsToSleep();
    //     }
    //     else
    //     {
    //         foreach (var unit in shelteredUnits)
    //         {
    //             if (unit == null) continue;

    //             if (!unit.isSleepingInShelter)
    //             {
    //                 unit.isExhausted = true;
    //                 ForceUnitDaySleep(unit);

    //                 Debug.Log($"⚠️ [Shift System]: {unit.name} อดนอนทำงานกะดึก ต้องพักกลางวันแทน!");
    //             }
    //             else
    //             {
    //                 NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
    //                 agent.enabled = true;
    //                 agent.isStopped = false;
    //                 unit.isSleepingInShelter = false;
    //                 unit.isExhausted = false; // 🟢 หายเหนื่อยตรงจุดนี้ (เช้าวันถัดไปหลังพักครบ)
    //             }
    //         }

    //         // CommandUnitsToWakeUp();
    //     }

    //     UpdateCanvasVisibility(); // 🟢 เช็คทุกครั้งที่เวลาเปลี่ยน
    // }

    // private void CommandUnitsToSleep()
    // {
    //     for (int i = 0; i < shelteredUnits.Count; i++)
    //     {
    //         if (shelteredUnits[i] != null)
    //         {
    //             Transform targetSleepPos = (sleepPoints != null && i < sleepPoints.Length) ? sleepPoints[i] : transform;

    //             // สั่งให้ NavMeshAgent ของยูนิตสัตว์เดินมานอนที่จุดนอน
    //             NavMeshAgent agent = shelteredUnits[i].GetComponent<NavMeshAgent>();
    //             if (agent != null && agent.isActiveAndEnabled)
    //             {
    //                 agent.isStopped = false;
    //                 agent.SetDestination(targetSleepPos.position);
    //                 Debug.Log($"💤 [Shelter]: {shelteredUnits[i].name} เดินกลับคอกไปนอนแล้ว");
    //             }
    //         }
    //     }
    // }
    // private void CommandUnitsToSleep()
    // {
    //     for (int i = 0; i < shelteredUnits.Count; i++)
    //     {
    //         UnitBase unit = shelteredUnits[i];
    //         if (unit == null) continue;
    //         if (unit.isSleepingInShelter) continue; // 🟢 หลับอยู่แล้ว (เช่นพักกลางวันจากโอที) ข้ามไป ไม่ต้องสั่งซ้ำ

    //         Transform targetSleepPos = (sleepPoints != null && i < sleepPoints.Length) ? sleepPoints[i] : transform;
    //         NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();

    //         if (agent != null && agent.isActiveAndEnabled)
    //         {
    //             unit.ResetUnitState();
    //             unit.isSleepingInShelter = true;

    //             agent.isStopped = false;
    //             agent.SetDestination(targetSleepPos.position);

    //             StartCoroutine(WaitAndStopWhenReached(unit, agent, targetSleepPos.position));

    //             Debug.Log($"💤 [Shelter]: {unit.name} ทิ้งงานแล้วกำลังเดินกลับคอกไปนอน...");
    //         }
    //     }
    // }

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

    // private void CommandUnitsToWakeUp()
    // {
    //     foreach (var unit in shelteredUnits)
    //     {
    //         if (unit.isExhausted) continue;
    //         if (unit != null)
    //         {
    //             Debug.Log($"☀️ [Shelter]: {unit.name} ตื่นนอนพร้อมทำงาน/เดินเล่นตอนเช้า!");

    //             NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
    //             agent.SetDestination(wakeUpPoint.position);
    //         }
    //     }
    // }
    // [ContextMenu("ForceWorkInNight")]
    // public void ForceWorkInNight()
    // {
    //     foreach (var unit in shelteredUnits)
    //     {
    //         if (unit == null) continue;

    //         Debug.Log($"{unit.name} ถูกปลุกออกไปทำงานกะดึก (Over Time)!");

    //         unit.ResetUnitState();
    //         unit.isSleepingInShelter = false;
    //         unit.isExhausted = true;

    //         NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
    //         if (agent != null)
    //         {
    //             agent.enabled = true;
    //             agent.isStopped = false;
    //             agent.SetDestination(wakeUpPoint.position);
    //         }
    //     }

    //     UpdateCanvasVisibility(); // 🟢 ปิด Canvas ทันที เพราะตอนนี้ทุกตัวเหนื่อยหมดแล้ว
    // }
    #region Sleep Time
    // เพิ่มฟังก์ชันนี้ลงใน AnimalShelter.cs เดิมของคุณ
    // public void ForceUnitDaySleep(UnitBase unit)
    // {
    //     if (!shelteredUnits.Contains(unit)) return;

    //     NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
    //     if (agent == null) return;

    //     unit.ResetUnitState();
    //     unit.isSleepingInShelter = true;

    //     int idx = shelteredUnits.IndexOf(unit);
    //     Transform targetSleepPos = (sleepPoints != null && idx >= 0 && idx < sleepPoints.Length) ? sleepPoints[idx] : transform;

    //     agent.enabled = true;
    //     agent.isStopped = false;
    //     agent.SetDestination(targetSleepPos.position);

    //     StartCoroutine(WaitAndStopWhenReached(unit, agent, targetSleepPos.position));

    //     Debug.Log($"🛌 [Shelter]: {unit.name} เข้านอนกลางวันเพื่อพักจากการทำโอที!");
    // }


    #endregion
    private void UpdateCanvasVisibility()
    {
        if (canvas == null) return;

        // bool hasAvailableUnit = false;
        // foreach (var unit in shelteredUnits)
        // {
        //     if (unit != null && !unit.isExhausted)
        //     {
        //         hasAvailableUnit = true;
        //         break;
        //     }
        // }

        // เปิดได้เฉพาะตอนกลางคืน และมียูนิตที่ยังไม่เหนื่อยอย่างน้อย 1 ตัว
        // bool shouldShow = isCurrentlyNight && hasAvailableUnit;
        bool shouldShow = isCurrentlyNight;
        canvas.gameObject.SetActive(shouldShow);
    }



    public void OnUnitInteract(UnitBase unit) { }
    public void OnUnitExit(UnitBase unit) { }
    public Vector3 GetInteractionPoint() => transform.position + transform.forward * 2f;
}