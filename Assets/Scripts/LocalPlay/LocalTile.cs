using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalTile : MonoBehaviour
{
    public enum ObstacleType {
        None, Trap, Rock
    }

    // The type of this tile
    public ObstacleType tileType;
    //public Combat.DiceSize localDice = Combat.DiceSize.D0;
    //public int diceBonus = 0;
    public Combat.Dice dice;

    public int row;
    public int col;

    // Lists of enemies and characters on this tile
    public List<LocalMonster> enemyList;
    public List<LocalCharacter> charaList;
    public List<LocalPin> pinList;

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

        if (tileType == ObstacleType.Trap && charaList.Count != 0)
        {
            LocalGameManager.Instance.updateEventQueue(this);
        }
        else if (tileType == ObstacleType.Rock && charaList.Count != 0)
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

    private void OnDestroy() {
        enemyList.Clear();
        charaList.Clear();
        pinList.Clear();
    }
}
