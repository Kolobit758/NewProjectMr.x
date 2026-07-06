using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomFormationComtroller : MonoBehaviour
{
    public CustomFormationData formationData;
    public List<NavMeshAgent> armyUnits;
    public LayerMask groundLayer;


    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                MoveToPoint(hit.point);
            }
        }
    }

    public void MoveToPoint(Vector3 clickPoint)
    {
        if (armyUnits.Count == 0 || formationData == null) return;

        Vector3 armyCenter = Vector3.zero;
        int activeUnits = 0;
        foreach (var unit in armyUnits)
        {
            if (unit != null)
            {
                armyCenter += unit.transform.position;
                activeUnits++;
            }
        }

        armyCenter /= activeUnits; // หาค่าเฉลี่ยของตำแหน่ง ซึ่งมันคือ ตรงกลาง

        Vector3 dir = (clickPoint - armyCenter).normalized;
        Quaternion formationRotation = dir != Vector3.zero ? Quaternion.LookRotation(dir) : Quaternion.identity;

        int unitIndex = 0;

        for (int y = 0; y < armyUnits.Count; y++)
        {
            for (int x = 0; x < armyUnits.Count; x++)
            {
                if (unitIndex >= armyUnits.Count) break;

                if (formationData.rows[y].cols[x] == true)// มันวางได้
                {
                    float xOffset = (x - (formationData.gridWidth - 1) / 2f) * formationData.spacing;
                    float zOffset = (((formationData.gridHeight - 1) / 2f) - y) * formationData.spacing;

                    Vector3 localOffset = new Vector3(xOffset, 0, zOffset);
                    Vector3 finalWorldPos = clickPoint + (formationRotation * localOffset);

                    if (armyUnits[unitIndex] != null)
                    {
                        if (NavMesh.SamplePosition(finalWorldPos, out NavMeshHit navHit, formationData.spacing * 1.5f, NavMesh.AllAreas))
                        {
                            armyUnits[unitIndex].SetDestination(navHit.position);
                            unitIndex++;
                        }
                    }
                }


            }
        }
    }
}
