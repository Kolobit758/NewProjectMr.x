using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSniffingSkill", menuName = "Combat/Skills/Sniffing")]
public class SniffingSkill : UnitSkillSO
{
    [Header("Detection Settings")]
    [Tooltip("รัศมีวงกว้างในการสแกนหาพิกัดกล่องสมบัติรอบๆ ตัวสัตว์เลี้ยง")]
    public float scanRadius = 60f;

    [Header("Movement Settings")]
    public float spotDwellTime = 0.6f;     
    public float trackingSpeedBonus = 1.3f; 

    [Header("Digging Settings")]
    public float digDuration = 2.0f;       

    public override void ExecuteSkill(GameObject user)
    {
        MonoBehaviour runtimeRunner = user.GetComponent<MonoBehaviour>();
        if (runtimeRunner != null)
        {
            runtimeRunner.StartCoroutine(FindExistingChestRoutine(user));
        }
    }

    private IEnumerator FindExistingChestRoutine(GameObject user)
    {
        Debug.Log($"{user.name} 🐾 เริ่มดมกลิ่นหาเบาะแสกล่องสมบัติในพื้นที่...");

        if (FootprintGenerator.Instance == null) yield break;

        // --- 🟢 ขั้นตอนที่ 1: ดึงตำแหน่งของกล่องที่สแตนบายอยู่บนแมพแล้ว ---
        Vector3 startPos = user.transform.position;
        Vector3 endPos = Vector3.zero;
        GameObject targetChestGo = null;

        // 1. ลองดึงจากช่อง Manual ใน Generator ดูก่อน
        if (FootprintGenerator.Instance.manualEndPoint != null)
        {
            targetChestGo = FootprintGenerator.Instance.manualEndPoint.gameObject;
            endPos = targetChestGo.transform.position;
        }
        else
        {
            // 2. ถ้าไม่ได้ลากใส่ไว้ ให้ใช้ระบบสแกนหาออโต้รอบตัวตามคอมโพเนนต์ ItemDropBox
            Collider[] targets = Physics.OverlapSphere(startPos, scanRadius);
            foreach (var col in targets)
            {
                if (col.GetComponent<ItemDropBox>() != null || col.GetComponent<HiddenChest>() != null)
                {
                    targetChestGo = col.gameObject;
                    endPos = targetChestGo.transform.position;
                    break;
                }
            }
        }

        if (targetChestGo == null)
        {
            Debug.LogWarning($"{user.name} ❌ ดมกลิ่นจนทั่วแล้ว ไม่พบพิกัดกล่องสมบัติแอบอยู่ในระยะ!");
            yield break;
        }

        // --- 🟢 ขั้นตอนที่ 2: เจนเส้นทางโค้ง Bezier ลากจากใต้เท้าสัตว์เลี้ยง พุ่งไปพิกัดกล่อง ---
        List<FootprintGenerator.TrailPointData> fixedPathPoints = FootprintGenerator.Instance.CalculateSplinePositions(startPos, endPos);
        if (fixedPathPoints == null || fixedPathPoints.Count == 0) yield break;

        // ปิดระบบเดินตามขบวนแถวชั่วคราว
        NavMeshAgent agent = user.GetComponent<NavMeshAgent>();
        FormationUnitController controller = user.GetComponent<FormationUnitController>();

        if (controller != null) controller.enabled = false;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed *= trackingSpeedBonus;
        }

        List<GameObject> activeFootprintObjects = new List<GameObject>();

        // --- 🟢 ขั้นตอนที่ 3: ทยอยเสกและเดินดมรอยเท้าพุ่งไปหาจุดหมายปลายทาง ---
        for (int i = 0; i < fixedPathPoints.Count; i++)
        {
            if (user == null) yield break;

            // เสกโหนดยิงนำทางสดๆ ทีละจุด
            GameObject nodeGo = Instantiate(
                FootprintGenerator.Instance.footprintNodePrefab, 
                fixedPathPoints[i].position, 
                fixedPathPoints[i].rotation
            );
            activeFootprintObjects.Add(nodeGo);

            FootprintNode nodeScript = nodeGo.GetComponent<FootprintNode>();
            if (nodeScript != null)
            {
                nodeScript.RevealNode(); 
            }

            if (agent != null && agent.enabled)
            {
                agent.SetDestination(fixedPathPoints[i].position);
            }

            // รอเดินถึงพิกัดรอยเท้าโหนดปัจจุบัน
            while (agent != null && (agent.pathPending || agent.remainingDistance > 0.4f))
            {
                if (agent.velocity.sqrMagnitude < 0.01f && !agent.pathPending) break;
                if (user == null) yield break;
                yield return null;
            }

            if (agent != null) agent.isStopped = true;

            // ยืนดมกลิ่น
            float dwellTimer = 0f;
            while (dwellTimer < spotDwellTime)
            {
                dwellTimer += Time.unscaledDeltaTime;
                yield return null;
            }

            // --- 🟢 ขั้นตอนที่ 4: เดินมาถึงตัวกล่องที่แอบอยู่จริงแล้ว -> เริ่มขุดและจบสกิล ---
            if (i == fixedPathPoints.Count - 1)
            {
                Debug.Log($"{user.name} ⛏️ แกะรอยตามกลิ่นจนเจอตัวกล่องแล้ว! เริ่มทำการขุดดิน...");
                
                float digTimer = 0f;
                while (digTimer < digDuration)
                {
                    digTimer += Time.unscaledDeltaTime;
                    yield return null;
                }

                // สั่งเปิดงานกล่องสมบัติ (ถ้าใช้ HiddenChest จมดินก็สั่งเด้งขึ้นมา ถ้าเป็น ItemDropBox ตั้งบนดินก็พร้อมโดนทุบได้เลย)
                HiddenChest hiddenChestScript = targetChestGo.GetComponent<HiddenChest>();
                if (hiddenChestScript != null)
                {
                    hiddenChestScript.SpawnChest();
                }
                else
                {
                    targetChestGo.SetActive(true);
                }
                break; 
            }

            if (agent != null) agent.isStopped = false;
        }

        // คลีนรอยเท้าเก่าทิ้งรีไซเคิลแรม
        if (controller != null)
        {
            controller.StartCoroutine(CleanUpTrail(activeFootprintObjects));
        }
        else
        {
            user.GetComponent<MonoBehaviour>().StartCoroutine(CleanUpTrail(activeFootprintObjects));
        }

        // ดึงตัวสัตว์เลี้ยงกลับเข้าแถวจัดขบวนทัพเดินตามผู้เล่นปกติ
        if (user != null)
        {
            if (agent != null)
            {
                agent.speed /= trackingSpeedBonus;
                if (NavMesh.SamplePosition(user.transform.position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                {
                    agent.Warp(navHit.position);
                }
                agent.isStopped = false;
            }   
            if (controller != null) controller.enabled = true;
        }
    }

    private IEnumerator CleanUpTrail(List<GameObject> footprints)
    {
        yield return new WaitForSeconds(3.0f);
        foreach (var fg in footprints)
        {
            if (fg != null) Destroy(fg);
        }
    }
}