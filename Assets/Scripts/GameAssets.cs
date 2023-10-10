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

    [Header("CharacterIcon UI Assets")]
    public Sprite dwarfIcon;
    public Sprite giantIcon;
    public Sprite humanIcon;

    [Header("YouAre UI Assets")]
    public Sprite youAreDwarf;
    public Sprite youAreGiant;
    public Sprite youAreHuman;

    public Sprite GetCharacterIcon(CharacterConfig.CharacterType character) {
        return character switch {
            CharacterConfig.CharacterType.Dwarf => dwarfIcon,
            CharacterConfig.CharacterType.Human => humanIcon,
            CharacterConfig.CharacterType.Giant => giantIcon,
            _ => null,
        };
    }

    [Header("Tile Assets")]
    public GameObject OpenTile;
    public GameObject WallTile;
    public GameObject DoorTile;
}
