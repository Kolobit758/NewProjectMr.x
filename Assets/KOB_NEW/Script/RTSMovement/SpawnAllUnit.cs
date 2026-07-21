using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class SpawnAllUnit : MonoBehaviour
{
    public List<UnitInstance> unitInstances = new List<UnitInstance>();
    public Transform unitFolder;
    public Transform MockSpawnPoint;
    public string layerName;

    void Start()
    {
        // 🟢 เปลี่ยนจาก Start ธรรมดา เป็นสั่งให้มันรอโหลด
        StartCoroutine(DelayedSpawn());
    }

    IEnumerator DelayedSpawn()
    {
        // รอแป๊บนึง ให้ระบบ Manager ทั้งหลาย Load ข้อมูลให้เสร็จก่อน
        yield return new WaitForSeconds(0.6f); 

        Debug.Log("===============");
        // ตอนนี้ข้อมูลจาก SaveLoadManager น่าจะเข้า AnimalInventory แล้ว
        foreach (var unit in AnimalInventory.Instance.tamedUnits)
        {
            unitInstances.Add(unit);
            Debug.Log("Spawn : " + unit.customName);
        }
        Debug.Log("===============");

        foreach(UnitInstance unitGameobject in unitInstances)
        {
            if(unitGameobject.template != null && unitGameobject.template.unitPrefab != null)
            {
                GameObject animalSpawned = Instantiate(unitGameobject.template.unitPrefab, unitFolder);
                animalSpawned.transform.position = MockSpawnPoint.position;
                
                
                if(animalSpawned.GetComponent<UnitBase>())
                {
                    animalSpawned.GetComponent<UnitBase>().enabled = true;
                }
                if (animalSpawned.GetComponent<AnimalAIController>())
                {
                    animalSpawned.GetComponent<AnimalAIController>().enabled = false;
                }

                animalSpawned.tag = "Unit";
                animalSpawned.layer = LayerMask.NameToLayer(layerName);

                RTS_movement.instance.allUnits.Add(animalSpawned.GetComponent<UnitBase>());
            }
        }

        
    }
}