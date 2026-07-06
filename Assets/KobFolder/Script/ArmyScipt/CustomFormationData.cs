using UnityEngine;

[System.Serializable]
public class FormationRow
{
    // แถวแนวนอน เก็บค่า bool ของแต่ละช่อง
    public bool[] cols = new bool[5]; 
}

[CreateAssetMenu(fileName = "NewFormation", menuName = "Formation/Custom Shape")]
public class CustomFormationData : ScriptableObject
{
    public int gridWidth = 5;
    public int gridHeight = 5;
    public float spacing = 1.5f;

    // ตาราง 2 มิติจำลองสำหรับใช้ใน Inspector
    public FormationRow[] rows = new FormationRow[5];
}