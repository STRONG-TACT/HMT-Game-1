using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTile : MonoBehaviour
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
    public List<NetworkMonster> enemyList;
    public List<NetworkCharacter> charaList;
    public List<NetworkPin> pinList;
    public NetworkShrine shrine = null;

    private void OnTriggerEnter(Collider col) {

        switch (col.gameObject.tag)
        {
            case "Monster":
                //Debug.Log("A monster enters.");
                NetworkMonster monster = col.gameObject.GetComponent<NetworkMonster>();
                monster.currentTile = this;
                enemyList.Add(monster);
                break;
            case "Character":
                //Debug.Log("A character enters.");
                NetworkCharacter character = col.gameObject.GetComponent<NetworkCharacter>();
                character.currentTile = this;
                charaList.Add(character);
                break;
            default:
                Debug.LogFormat("Tile Hit Trigger: {0}", col.gameObject.tag);
                break;
        }

        if (charaList.Count != 0 && enemyList.Count != 0)
        {
            NetworkGameManager.S.updateEventQueue(this);
        }

        if (tileType == ObstacleType.Trap && charaList.Count != 0)
        {
            NetworkGameManager.S.updateEventQueue(this);
        }
        else if (tileType == ObstacleType.Rock && charaList.Count != 0)
        {
            NetworkGameManager.S.updateEventQueue(this);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        switch (col.gameObject.tag)
        {
            case "Monster":
                //Debug.Log("A monster exits.");
                enemyList.Remove(col.gameObject.GetComponent<NetworkMonster>());
                break;
            case "Character":
                //Debug.Log("A character exits.");
                charaList.Remove(col.gameObject.GetComponent<NetworkCharacter>());
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
