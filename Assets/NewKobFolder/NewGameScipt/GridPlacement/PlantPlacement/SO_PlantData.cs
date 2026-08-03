using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSeed", menuName = "Plants/Seed Data")]
public class SO_PlantData : SO_ItemData
{
    [Header("Growth Time Settings")]
    public int daysToGrow = 3;             
    public float timeToGrowSeconds = 60f;  
    public float timeToGrow = 60f;         // เวลาทั้งหมดที่ใช้ในการเติบโต (วินาที)

    [Header("Ideal Soil Requirements (ธาตุอาหาร NPK ในอุดมคติ)")]
    public NutrientData idealNutrients; 
    public float toleranceRange = 20f;  

    [Header("Care Schedule (ความถี่ในการดูแลต่อ 1 วัน)")]
    public float wateringsPerDay = 2f;      // ต้องรดน้ำกี่ครั้งต่อรอบโต
    public float fertilizingsPerDay = 1f;   // ต้องใส่ปุ๋ยรี่ครั้งต่อรอบโต

    [Header("Harvest Results & Rewards")]
    public SO_ItemData cropProduct; 
    public SO_ItemData cropSeed; 
    public int minProductAmount = 1;
    public int maxProductAmount = 3;
    public SO_ItemData poopFertilizerProduct;
    public int fertilizerAmount = 1;

    [Header("Prefabs for Visual Stages")]
    public GameObject seedPrefab;
    public GameObject growingPrefab;
    public GameObject fullyGrownPrefab;

    [Header("Enemy Attraction")]
    public List<SO_EnemyData> specificAttractedEnemies = new List<SO_EnemyData>();

    public override bool UseItem(GameObject user)
    {
        if (FarmingManager.Instance != null)
        {
            FarmingManager.Instance.currentSelectedSeed = this;
            Debug.Log($"🌱 [Farming System] ถือเมล็ด {itemName} เตรียมปลูก!");
            return true;
        }
        return false;
    }
}