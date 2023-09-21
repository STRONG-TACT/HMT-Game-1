using UnityEngine;


[CreateAssetMenu(fileName = "NewMonsterConfig", menuName = "Monster/MonsterConfig")]
public class MonsterConfig : ScriptableObject
{
    public enum MonsterType
    {
        L_Monster,
        M_Monster,
        S_Monster
    }

    [Tooltip("Name for this character type. Usually just Dwarf, Giant, or Human. Will Show up in logs and state descriptions")]
    public string monsterName;
    [Tooltip("The Type of character to use, used for graphical model and icons.")]
    public MonsterType type;
    [Tooltip("Movement Limit.")]
    [Min(1)]
    public int movement;



    [Tooltip("The usual icon used for the monster.")]
    public Sprite MonsterIcon;
}