using System;
using UnityEngine;


[CreateAssetMenu(fileName = "NewCharacterConfig", menuName = "Character/CharacterConfig")]
public class CharacterConfig : ScriptableObject {
    public enum CharacterType {
        Human,
        Dwarf,
        Giant
    }

    [Tooltip("Name for this character type. Usually just Dwarf, Giant, or Human. Will Show up in logs and state descriptions")]
    public string characterName;
    [Tooltip("The Type of character to use, used for graphical model and icons.")]
    public CharacterType type;
    [Tooltip("The number of action points a character starts a currRound with.")]
    [Min(1)]
    public int StartingActionPoints;

    [Tooltip("The number of hearts a chracter starts a life with.")]
    [Min(1)]
    public int StartingHealth = 3;

    [Tooltip("Combat dice when facing monsters.")]
    public Combat.Dice monsterDice;
    [Tooltip("Combat dice when facing traps.")]
    public Combat.Dice trapDice;
    [Tooltip("Combat dice when facing stones.")]
    public Combat.Dice stoneDice;

    //Below here currently only used for reference.
    [Tooltip("How many adjacent tiles they can see.")]
    [Min(1)]
    public int sightRange; 

    [Tooltip("The usual icon used for the character.")]
    public Sprite characterIcon;

    [Tooltip("The icon used for the YouAre UI.")]
    public Sprite youAreIcon;

    [Tooltip("The color used to identify the character.")]
    public Color color;
}

