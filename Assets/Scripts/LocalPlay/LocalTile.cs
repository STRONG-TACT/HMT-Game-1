using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalTile : MonoBehaviour
{
    public enum ObstacleType {
        None, Trap, Rock, Wall
    }

    public enum FogOfWarState
    {
        Unseen = 0,
        Seen = 1,
        Visible = 2
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
    public LocalShrine shrine = null;
    public Dictionary<int, FogOfWarState> fogOfWarDictionary;

    private void OnTriggerEnter(Collider col) {
        //if(!(LocalGameManager.Instance.gameStatus == LocalGameManager.GameStatus.Player_Moving || 
        //    LocalGameManager.Instance.gameStatus == LocalGameManager.GameStatus.Player_Pinning)) {
        //    return;
        //}

        switch (col.gameObject.tag)
        {
            case "VisibleMask":
                //Debug.Log("Tile collide with mask-----------------------");
                LocalCharacter mask_character = col.gameObject.transform.parent.GetComponent<LocalCharacter>();
                //Debug.Log(fogOfWarDictionary[mask_character.CharacterId]);
                //Debug.Log("CharacterID: -------------------------");
                //Debug.Log(mask_character.CharacterId);
                fogOfWarDictionary[mask_character.CharacterId] = FogOfWarState.Visible;
                setRenderer(true);
                break;
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
            case "VisibleMask":
                //Debug.Log("Tile collide with mask-----------------------");
                LocalCharacter mask_character = col.gameObject.transform.parent.GetComponent<LocalCharacter>();
                //Debug.Log(fogOfWarDictionary[mask_character.CharacterId]);
                //Debug.Log("CharacterID: -------------------------");
                //Debug.Log(mask_character.CharacterId);
                fogOfWarDictionary[mask_character.CharacterId] = FogOfWarState.Seen;
                setRenderer(false);
                break;
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


    private void setRenderer(bool active) {
        //Get characters currently on the tile.
        foreach (LocalCharacter character in charaList) {
            Animator[] char_animators = character.GetComponentsInChildren<Animator>(true);
            foreach (Animator char_animator in char_animators)
            {
                if (char_animator != null)
                {

                    char_animator.enabled = active;
                }
            }
            Renderer[] char_renderers = character.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer char_renderer in char_renderers)
            {
                if (char_renderer != null)
                {
                    if (char_renderer.gameObject.tag != "TileGround")
                    {
                        char_renderer.enabled = active;
                    }
                }
            }
        }
        //Get monsters currently on the tile.
        foreach (LocalMonster monster in enemyList)
        {
            Animator[] mons_animators = monster.GetComponentsInChildren<Animator>(true);
            foreach (Animator mons_animator in mons_animators)
            {
                if (mons_animator != null)
                {

                    mons_animator.enabled = active;
                }
            }
            Renderer[] mons_renderers = monster.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer mons_renderer in mons_renderers)
            {
                if (mons_renderer != null)
                {
                    if (mons_renderer.gameObject.tag != "TileGround")
                    {
                        mons_renderer.enabled = active;
                    }
                }
            }
        }
        // Get all Animator components in children GameObjects, include inactive ones
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator != null)
            {

                animator.enabled = active;
            }
        }
        // Get all Renderer components in children GameObjects, include inactive ones
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                if (renderer.gameObject.tag != "TileGround")
                {
                    renderer.enabled = active;
                }
            }
        }
    }



    public void updateFogOfWar_tile(int characterID) {
        if (this.fogOfWarDictionary[characterID] == FogOfWarState.Unseen || this.fogOfWarDictionary[characterID] == FogOfWarState.Seen)
        {
            setRenderer(false);
        }
        else if (this.fogOfWarDictionary[characterID] == FogOfWarState.Visible) {
            setRenderer(true);
        }
    }


    private void OnDestroy() {
        enemyList.Clear();
        charaList.Clear();
        pinList.Clear();
    }
}
