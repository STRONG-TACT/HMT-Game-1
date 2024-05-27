using UnityEngine;


[CreateAssetMenu(fileName = "NewMonsterConfig", menuName = "Monster/MonsterConfig")]
public class MonsterConfig : ScriptableObject {

    public enum MovementStyle {
        RandomWalk = 0,
        Vertical = 1,
        Horizontal = 2,
        Static = 3
    }

    [Tooltip("Name of the type of monster")]
    public string configName;

    [Tooltip("How the monster should move on its turn.")]
    public MovementStyle movementStyle;
    
    [Tooltip("Movement Limit.")]
    [Min(1)]
    public int movement;

    [Tooltip("The dice use for combat.")]
    public Combat.Dice combatDice;

    [Tooltip("The usual icon used for the monster.")]
    public Sprite MonsterIcon;
}