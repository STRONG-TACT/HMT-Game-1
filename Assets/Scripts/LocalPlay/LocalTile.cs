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

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    public Material seen_material;
    public Material unseen_material;

    private void Awake() {
        seen_material = Resources.Load<Material>("seen");
        unseen_material = Resources.Load<Material>("unseen");
        fogOfWarDictionary = new Dictionary<int, LocalTile.FogOfWarState>();
        fogOfWarDictionary.Add(0, LocalTile.FogOfWarState.Unseen);
        fogOfWarDictionary.Add(1, LocalTile.FogOfWarState.Unseen);
        fogOfWarDictionary.Add(2, LocalTile.FogOfWarState.Unseen);
    }
    private void Start() {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers) {
            // Store the original materials
            originalMaterials[renderer] = renderer.materials;
        }
    }

    private void OnTriggerStay(Collider col)
    {
        if (col.gameObject.tag == "VisibleMask") {
            LocalCharacter mask_character = col.gameObject.transform.parent.GetComponent<LocalCharacter>();
            //Debug.Log(mask_character.CharacterId);
            fogOfWarDictionary[mask_character.CharacterId] = FogOfWarState.Visible;

        }
    }


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
                Debug.Log(mask_character.CharacterId);
                fogOfWarDictionary[mask_character.CharacterId] = FogOfWarState.Visible;
                //setRenderer(true);
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
                //setRenderer(false);
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


    private void setRenderer(FogOfWarState state) {
        bool active;
        if (state == FogOfWarState.Unseen) active = false; else active = true;
        //turn off renderer for characters on the tile
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
                    if (char_renderer.gameObject.tag != "TileGround" && char_renderer.gameObject.tag != "VisibleMask")
                    {
                        char_renderer.enabled = active;
                    }
                }
            }
        }
        //turn off renderer for monsters on the tile.
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

        //handle the stationary objects on the tile
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
                //if state is Unseen, turn off renderer for all objects except for tileGround and apply unseen shader
                if (state == FogOfWarState.Unseen)
                {
                    if (renderer.gameObject.tag != "TileGround")
                    {
                        renderer.enabled = active;
                    }
                    else
                    {
                        Material[] overideMaterials = new Material[renderer.materials.Length];
                        Material newMaterial = unseen_material;
                        for (int i = 0; i < overideMaterials.Length; i++)
                        {
                            // Assign the new material to each material slot
                            overideMaterials[i] = newMaterial;
                        }
                        // Apply the new material array to the renderer
                        renderer.materials = overideMaterials;
                    }

                }
                //if state is seen, turn on renderers for all stationary objects and apply seen shader
                else if (state == FogOfWarState.Seen)
                {
                    renderer.enabled = active;
                    Material[] overideMaterials = new Material[renderer.materials.Length];
                    Material newMaterial = seen_material;
                    for (int i = 0; i < overideMaterials.Length; i++)
                    {
                        // Assign the new material to each material slot
                        overideMaterials[i] = newMaterial;
                    }
                    // Apply the new material array to the renderer
                    renderer.materials = overideMaterials;
                }
                //if state is visiable, turn on renderers for all stationary objects and revert to original shader
                else if (state == FogOfWarState.Visible) {
                    renderer.enabled = active;
                    foreach (var kvp in originalMaterials)
                    {
                        // Revert the materials back to the original ones
                        kvp.Key.materials = kvp.Value;
                    }
                }
                else Debug.Log("Fog of War State Error in LocalTile");
                //if state is "unseen" or "seen", change the material for tile ground
                /*
                else if (state == FogOfWarState.Unseen || state == FogOfWarState.Seen)
                {
                    // Create an array to hold the new materials, which will override the orivinal ones
                    Material[] overideMaterials = new Material[renderer.materials.Length];
                    Material newMaterial;
                    if (state == FogOfWarState.Unseen) newMaterial = unseen_material; else newMaterial = seen_material;
                    for (int i = 0; i < overideMaterials.Length; i++)
                    {
                        // Assign the new material to each material slot
                        overideMaterials[i] = newMaterial;
                    }
                    // Apply the new material array to the renderer
                    renderer.materials = overideMaterials;
                }
                //if state is "visible", revert material for tile ground to original look
                else
                {
                    foreach (var kvp in originalMaterials)
                    {
                        // Revert the materials back to the original ones
                        kvp.Key.materials = kvp.Value;
                    }
                }
                */
            }
        }
    }



    public void updateFogOfWar_tile(int characterID) {
        if (this.fogOfWarDictionary[characterID] == FogOfWarState.Unseen)
        {
            setRenderer(FogOfWarState.Unseen);
        }
        else if (this.fogOfWarDictionary[characterID] == FogOfWarState.Seen) {
            setRenderer(FogOfWarState.Seen);
        }
        else if (this.fogOfWarDictionary[characterID] == FogOfWarState.Visible)
        {
            setRenderer(FogOfWarState.Visible);
        }
    }


    private void OnDestroy() {
        enemyList.Clear();
        charaList.Clear();
        pinList.Clear();
    }
}
