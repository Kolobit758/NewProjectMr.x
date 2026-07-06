using UnityEngine;


[CreateAssetMenu(fileName = "NEW Giant buff", menuName = "Buffs/Giant Buff")]
public class SO_GiantBuff : BaseBuffData
{
    public float scaleMultiplier = 2f;
    public override void ApplyBuff(GameObject target)
    {
        target.transform.localScale *= scaleMultiplier;
    }
    public override void RemoveBuff(GameObject target)
    {
        target.transform.localScale /= scaleMultiplier;
    }
}