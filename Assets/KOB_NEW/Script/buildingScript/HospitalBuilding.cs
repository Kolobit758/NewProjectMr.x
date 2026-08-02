using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class HospitalBuilding : MonoBehaviour, ITaskable
{
    [Header("Hospital Settings")]
    public string hospitalName = "โรงพยาบาลสัตว์";
    public int bedCapacity = 4; // จำนวนเตียง/จำนวนที่รองรับในโรงพยาบาล
    public float healDuration = 10f; // เวลาที่ใช้ในการนอนรักษาตัว (วินาที)

    [Header("Exit Point")]
    public Transform exitPoint;             // จุดเดินออกมาด้านนอกหลังรักษาหาย

    [Header("Active Patients")]
    public List<UnitBase> currentPatients = new List<UnitBase>();

    // 🌐 [Static Manager] รวมโรงพยาบาลทั้งหมดในฉาก
    public static List<HospitalBuilding> allHospitals = new List<HospitalBuilding>();

    void Awake()
    {
        if (!allHospitals.Contains(this)) allHospitals.Add(this);
    }

    void OnDestroy()
    {
        if (allHospitals.Contains(this)) allHospitals.Remove(this);
    }

    // 🔍 เช็คว่าโรงพยาบาลนี้เต็มหรือยัง
    public bool IsFull()
    {
        return currentPatients.Count >= bedCapacity;
    }

    // 🏥 ฟังก์ชันสั่งให้ยูนิตเข้ามานอนรักษาตัว
    public void AdmitPatient(UnitBase unit)
    {
        if (IsFull() || currentPatients.Contains(unit)) return;

        currentPatients.Add(unit);

        // ใช้ตำแหน่งของตึกหลัก (transform.position) เป็นจุดนอน
        Vector3 bedPosition = transform.position;

        // เริ่มกระบวนการเดินเข้าโรงพยาบาล
        StartCoroutine(PatientRoutine(unit, bedPosition));
    }

    private IEnumerator PatientRoutine(UnitBase unit, Vector3 bedPosition)
    {
        NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
        if (agent == null) yield break;

        // 1. ยกเลิกงานปัจจุบันและสั่งเดินไปที่จุดนอน
        unit.ResetUnitState();
        agent.isStopped = false;
        agent.SetDestination(bedPosition);

        // รอจนกว่าจะเดินไปถึง
        yield return new WaitForEndOfFrame();
        while (agent.pathPending) yield return null;

        while (agent.enabled && Vector3.Distance(unit.transform.position, bedPosition) > agent.stoppingDistance + 0.5f)
        {
            yield return null;
        }

        // 2. ถึงแล้ว: ปิดการควบคุม/ซ่อนตัวยูนิตชั่วคราวระหว่างรักษา
        agent.isStopped = true;
        agent.enabled = false;

        // ซ่อน Renderer ชั่วคราวตอนนอนรักษา
        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = false;

        Debug.Log($"🏥 [Hospital]: {unit.name} เข้าสู่กระบวนการรักษาในโรงพยาบาล...");

        // 3. นอนพักรักษาตัวตามเวลาที่กำหนด (healDuration)
        float timer = 0f;
        while (timer < healDuration)
        {
            timer += Time.deltaTime;
            CharacterStats stats = unit.GetComponent<CharacterStats>();
            if (stats != null)
            {
                int healAmount = Mathf.RoundToInt(Time.deltaTime * 10f);
                if (healAmount > 0)
                {
                    stats.currentHP += healAmount;
                }
            }
            yield return null;
        }

        // 4. รักษาเสร็จแล้ว: เปิดแสดงผลและเปิดใช้งานตัวยูนิตกลับมา
        foreach (var r in renderers) r.enabled = true;

        // 🟢 เปิด Agent ก่อน แล้วรอให้ระบบ NavMesh เซ็ตอัพตัวละครลงพื้นก่อนสั่งเดิน
        agent.enabled = true;
        yield return new WaitForFixedUpdate(); // รอให้ Agent จับวางลงบน NavMesh สำเร็จ

        // 5. สั่งให้เดินออกจากโรงพยาบาลไปยังจุด ExitPoint หรือหน้าตึก
        Vector3 outPosition = (exitPoint != null) ? exitPoint.position : transform.position + transform.forward * 3f;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(outPosition);
        }

        // เอาออกจากรายชื่อผู้ป่วย
        currentPatients.Remove(unit);
        Debug.Log($"✨ [Hospital]: {unit.name} รักษาหายดีและเดินออกจากโรงพยาบาลแล้ว!");
    }

    // รองรับระบบ ITaskable เพื่อให้ผู้เล่นสั่งยูนิตเดินมาคลิกเข้าโรงพยาบาลได้
    public void OnUnitInteract(UnitBase unit)
    {
        if (!IsFull())
        {
            AdmitPatient(unit);
        }
        else
        {
            Debug.LogWarning("⚠️ [Hospital]: โรงพยาบาลเต็มทุกเตียงแล้ว!");
        }
    }

    public void OnUnitExit(UnitBase unit) { }
    public Vector3 GetInteractionPoint() => transform.position + transform.forward * 2f;
}