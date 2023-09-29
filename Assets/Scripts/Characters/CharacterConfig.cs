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
    [Tooltip("Movement Limit.")]
    [Min(1)]
    public int movement;

    [Tooltip("Combat dice when facing monsters.")]
    public Combat.DiceType monsterDice;
    [Tooltip("Combat dice when facing traps.")]
    public Combat.DiceType trapDice;
    [Tooltip("Combat dice when facing stones.")]
    public Combat.DiceType stoneDice;

    //Below here currently only used for reference.
    [Tooltip("How many adjacent tiles they can see. Currently for reference only.")]
    [Min(1)]
    public int sightRange; 

    //TODO, Ultimately, we shouldn't need this as we should be able to determine it analytically from the sightRange
    [Tooltip("Where to initial place the camera realtive to the character. Currently for reference only.")]
    public Vector3 cameraPosition; 
    [Tooltip("The faces on the character die.")]
    public int[] dieFaces = { 1, 2, 3, 4, 5, 6 };

    [Tooltip("The usual icon used for the character.")]
    public Sprite characterIcon;

    [Tooltip("The icon used for the YouAre UI.")]
    public Sprite youAreIcon;
}

