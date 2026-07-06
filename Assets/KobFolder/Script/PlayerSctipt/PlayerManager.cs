using UnityEngine; // เอาอันอื่นออกให้หมด เหลือแค่อันนี้พอ!

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(PlayerActionMovement))]
public class PlayerManager : MonoBehaviour
{
    public CharacterStats Stats { get; private set; }
    public PlayerActionMovement Movement { get; private set; }
    public PlayerAttack Attack { get; private set; }

    private void Awake()
    {
        Stats = GetComponent<CharacterStats>();
        Movement = GetComponent<PlayerActionMovement>();
        Attack = GetComponent<PlayerAttack>();
    }
}