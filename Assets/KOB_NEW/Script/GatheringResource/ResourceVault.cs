using UnityEngine;

public class ResourceVault : MonoBehaviour, ITaskable
{
    public Vector3 GetInteractionPoint() => transform.position;

    public void OnUnitInteract(UnitBase unit)
    {
        if (unit.isCarrying)
        {
            ResourceInventory.Instance?.AddResource(unit.carriedItem, unit.carriedAmount);
            unit.DropItemAtVault();
        }
    }

    public void OnUnitExit(UnitBase unit) { }
}