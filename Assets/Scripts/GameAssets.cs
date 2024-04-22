using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// This should probably be a scriptable Asset...
/// </summary>
public class GameAssets : MonoBehaviour
{
    [Header("Combat UI Assets")]
    public Sprite trapCombatUI;
    public Sprite rockCombatUI;
    public Sprite monsterCombatUI;
    public Sprite[] diceImg;
    public Sprite Scoreboard_visible;
    public Sprite Scoreboard_hidden;

    [Header("CharacterIcon UI Assets")]
    public Sprite dwarfIcon;
    public Sprite giantIcon;
    public Sprite humanIcon;

    [Header("EnemyIcon UI Assets")]
    public Sprite monsterIcon;
    public Sprite trapIcon;
    public Sprite rockIcon;

    [Header("YouAre UI Assets")]
    public Sprite youAreDwarf;
    public Sprite youAreGiant;
    public Sprite youAreHuman;


    [Header("Player Live Status Icon")]
    public Sprite deadIcon;
    public Sprite aliveIcon;

    public Sprite GetCharacterIcon(CharacterConfig.CharacterType character) {
        return character switch {
            CharacterConfig.CharacterType.Dwarf => dwarfIcon,
            CharacterConfig.CharacterType.Human => humanIcon,
            CharacterConfig.CharacterType.Giant => giantIcon,
            _ => null,
        };
    }

    [Header("Dice UI Assets")]
    public Sprite D0;
    public Sprite D4;
    public Sprite D6;
    public Sprite D8;
    public Sprite D10;

    public Sprite GetDiceIcon(Combat.DiceSize dice)
    {
        return dice switch
        {
            Combat.DiceSize.D0 => D0,
            Combat.DiceSize.D4 => D4,
            Combat.DiceSize.D6 => D6,
            Combat.DiceSize.D8 => D8,
            Combat.DiceSize.D10 => D10,
            _ => null,
        };
    }

    [Header("Dice Outline Assets")]
    public Sprite D0_Outline;
    public Sprite D4_Outline;
    public Sprite D6_Outline;
    public Sprite D8_Outline;
    public Sprite D10_Outline;

    public Sprite GetDiceOutline(Combat.DiceSize dice)
    {
        return dice switch
        {
            Combat.DiceSize.D0 => D0_Outline,
            Combat.DiceSize.D4 => D4_Outline,
            Combat.DiceSize.D6 => D6_Outline,
            Combat.DiceSize.D8 => D8_Outline,
            Combat.DiceSize.D10 => D10_Outline,
            _ => null,
        };
    }

    [Header("Goal Filled Assets")]
    public Sprite Dwarf_filled;
    public Sprite Giant_filled;
    public Sprite Human_filled;

    public Sprite GetGoalFilled(int charaID)
    {
        return charaID switch
        {
            0 => Dwarf_filled,
            1 => Giant_filled,
            2 => Human_filled,
            _ => null,
        };
    }

    [Header("Goal Unfilled Assets")]
    public Sprite Dwarf_unfilled;
    public Sprite Giant_unfilled;
    public Sprite Human_unfilled;

    public Sprite GetGoalUnfilled(int charaID)
    {
        return charaID switch
        {
            0 => Dwarf_unfilled,
            1 => Giant_unfilled,
            2 => Human_unfilled,
            _ => null,
        };
    }

    [Header("Tile Assets")]
    public GameObject OpenTile;
    public GameObject DoorTile;
    public List<GameObject> WallTiles;
    public List<GameObject> TrapTiles;
    public List<GameObject> RockTiles;
    public GameObject MapBoundary;

    [Header("Character/Monster Assets")]
    public List<GameObject> Characters;
    public List<GameObject> Monsters;
    public List<GameObject> Goals;
}