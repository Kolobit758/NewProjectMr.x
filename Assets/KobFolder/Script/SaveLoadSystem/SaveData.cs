using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // 💼 หมวดที่ 1: กระเป๋าผู้เล่นและคลัง Outpost (เซฟเป็น ID และ จำนวน)
    public List<string> playerInventoryKeys = new List<string>();
    public List<int> playerInventoryValues = new List<int>();

    public List<string> outpostVaultKeys = new List<string>();
    public List<int> outpostVaultValues = new List<int>();
    
    // 🏛️ หมวดที่ 2: ประวัติฐาน Outpost ที่ยึดครองแล้ว
    public List<string> capturedOutpostIDs = new List<string>();
    
    // 🟢 NEW: สถานะการยึด Outpost (outpostId -> isPlayerOccupy)
    public List<string> outpostStateIds = new List<string>();
    public List<bool> outpostStateOccupied = new List<bool>();

    // 🐺 หมวดที่ 3: สวนสัตว์ สัตว์เลี้ยงที่ Tame ได้ทั้งหมดพร้อมสเตตัสเฉพาะตัว
    public List<UnitInstance> tamedAnimals = new List<UnitInstance>();

    // 🗺️ หมวดที่ 4: พิกัดตำแหน่งตัวละครล่าสุด เผื่อโหลดเซฟแล้วมาโผล่จุดเดิม
    public float playerPosX, playerPosY, playerPosZ;
    public string currentMapName;

    public List<string> savedAnimalTemplates = new List<string>();
    public List<string> savedAnimalUniqueIds = new List<string>();
    public List<string> savedAnimalCustomNames = new List<string>();
    public List<int> savedAnimalHPs = new List<int>();
}