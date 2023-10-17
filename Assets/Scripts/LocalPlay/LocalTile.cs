using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalTile : MonoBehaviour
{
    public enum TileType
    {
        Other, Trap, Rock
    }

    // The type of this tile
    public TileType tileType;
    public Combat.DiceType localDice = Combat.DiceType.D0;
    public int diceBonus = 0;

    public int row;
    public int col;

    // Lists of enemies and characters on this tile
    public List<LocalMonster> enemyList;
    public List<LocalCharacter> charaList;


    private void OnTriggerEnter(Collider col)
    {
        switch (col.gameObject.tag)
        {
            case "Monster":
                //Debug.Log("A monster enters.");
                enemyList.Add(col.gameObject.GetComponent<LocalMonster>());
                break;
            case "Character":
                //Debug.Log("A character enters.");
                charaList.Add(col.gameObject.GetComponent<LocalCharacter>());
                break;
            default:
                Debug.LogFormat("Tile Hit Trigger: {0}", col.gameObject.tag);
                break;
        }

        if (charaList.Count != 0 && enemyList.Count != 0)
        {
            LocalGameManager.Instance.updateEventQueue(this);
        }

        if (tileType == TileType.Trap && charaList.Count != 0)
        {
            LocalGameManager.Instance.updateEventQueue(this);
        }
        else if (tileType == TileType.Rock && charaList.Count != 0)
        {
            LocalGameManager.Instance.updateEventQueue(this);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        switch (col.gameObject.tag)
        {
            case "Monster":
                //Debug.Log("A monster exits.");
                enemyList.Remove(col.gameObject.GetComponent<LocalMonster>());
                break;
            case "Character":
                //Debug.Log("A character exits.");
                charaList.Remove(col.gameObject.GetComponent<LocalCharacter>());
                break;
            default:
                Debug.LogFormat("Character Hit Trigger: {0}", col.gameObject.tag);
                break;
        }
    }
}
