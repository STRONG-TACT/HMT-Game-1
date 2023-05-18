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

    public Sprite GetCharacterIcon(GameData.CharacterType character) {
        return character switch {
            GameData.CharacterType.Dwarf => dwarfIcon,
            GameData.CharacterType.Human => humanIcon,
            GameData.CharacterType.Giant => giantIcon,
            _ => null,
        };
    }
}
