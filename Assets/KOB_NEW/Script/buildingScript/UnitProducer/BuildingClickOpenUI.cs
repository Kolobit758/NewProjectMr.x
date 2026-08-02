using UnityEngine;
using UnityEngine.UI;

public class BuildingClickOpenUI : MonoBehaviour
{
    public UnitProducerBuilding myBuilding; // ลากตึก (UnitProducerBuilding) ของตัวเองมาใส่

    void Start()
    {
        GetComponent<Button>()?.onClick.AddListener(() => {
            // 🟢 เรียกใช้ Instance แล้วส่งค่าตึกตัวเองเข้าไปเปิดได้ทันทีแบบง่ายสุดๆ!
            if (BuildingProductionUI.Instance != null && myBuilding != null)
            {
                BuildingProductionUI.Instance.Open(myBuilding);
            }
        });
    }
}