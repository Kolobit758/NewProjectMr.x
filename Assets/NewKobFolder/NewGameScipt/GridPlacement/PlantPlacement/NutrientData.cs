using UnityEngine;

[System.Serializable]
public struct NutrientData
{
    [Header("ธาตุอาหารหลัก (0 - 100)")]
    public float nitrogen;     // N: ไนโตรเจน (เน้นใบ, การเติบโต, ความเขียว)
    public float phosphorus;   // P: ฟอสฟอรัส (เน้นราก, ดอก, การแตกกิ่งก้าน)
    public float potassium;    // K: โพแทสเซียม (เน้นผลผลิต, คุณภาพ, ความหวาน/ทนทาน)
}