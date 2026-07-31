public enum FactionType { AnimalLarge, AnimalSmall, Robot }

public static class CombatSystem
{
    // คำนวณโบนัสดาเมจตามหลักแพ้ทางชนะทาง (Rock-Paper-Scissors)
    public static float GetDamageMultiplier(FactionType attacker, FactionType defender)
    {
        if (attacker == FactionType.AnimalLarge && defender == FactionType.AnimalSmall) return 1.5f; // โบนัส 50%
        if (attacker == FactionType.AnimalSmall && defender == FactionType.Robot) return 1.5f;
        if (attacker == FactionType.Robot && defender == FactionType.AnimalLarge) return 1.5f;

        if (attacker == FactionType.AnimalSmall && defender == FactionType.AnimalLarge) return 0.7f; // เบาลง 30%
        if (attacker == FactionType.Robot && defender == FactionType.AnimalSmall) return 0.7f;
        if (attacker == FactionType.AnimalLarge && defender == FactionType.Robot) return 0.7f;

        return 1.0f;
    }
}