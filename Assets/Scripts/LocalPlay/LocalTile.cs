using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalTile : MonoBehaviour
{
    public enum ObstacleType {
        None, Trap, Rock, Wall
    }

    // The type of this tile
    public ObstacleType tileType;
    //public Combat.DiceSize localDice = Combat.DiceSize.D0;
    //public int diceBonus = 0;
    public Combat.Dice dice;

    public int row;
    public int col;

    public Vector2Int GridPosition {
        get {
            return new Vector2Int(row, col);
        }
    }

    // Lists of enemies and characters on this tile
    public List<LocalMonster> enemyList;
    public List<LocalCharacter> charaList;
    public List<LocalPin> pinList;

    private void OnTriggerEnter(Collider col) {
        //if(!(LocalGameManager.Instance.gameStatus == LocalGameManager.GameStatus.Player_Moving || 
        //    LocalGameManager.Instance.gameStatus == LocalGameManager.GameStatus.Player_Pinning)) {
        //    return;
        //}

        switch (col.gameObject.tag)
        {
            case "Monster":
                //Debug.Log("A monster enters.");
                LocalMonster monster = col.gameObject.GetComponent<LocalMonster>();
                monster.currentTile = this;
                enemyList.Add(monster);
                break;
            case "Character":
                //Debug.Log("A character enters.");
                LocalCharacter character = col.gameObject.GetComponent<LocalCharacter>();
                character.currentTile = this;
                charaList.Add(character);
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
