using UnityEngine;


[CreateAssetMenu(fileName = "NewCompostRecipe", menuName = "Game/Compost Recipe")]
public class SO_CompostRecipe : ScriptableObject
{
    public string recipeName = "ปุ๋ยสูตรพิเศษ";
    public SO_ItemData resultingFertilizer; // ปุ๋ยที่จะได้
    public ResourceCost[] requiredIngredients; // รายการไอเทมหลายประเภทที่ต้องใช้
    public float productionTime = 15f; // เวลาในการหมัก (วินาที)
}