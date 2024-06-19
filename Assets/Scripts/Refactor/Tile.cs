using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tile : MonoBehaviour
{
    private static Dictionary<string, int> TILE_IDS_BY_OBJ_KEY = new Dictionary<string, int>();
    private static string GetObjID(string objKey) {
        if (!TILE_IDS_BY_OBJ_KEY.ContainsKey(objKey)) {
            TILE_IDS_BY_OBJ_KEY[objKey] = 0;
        }
        TILE_IDS_BY_OBJ_KEY[objKey]++;
        return objKey + TILE_IDS_BY_OBJ_KEY[objKey];
    }

    public string ObjKey {
        get {
            return _objKey;
        }
        set {
            if (_objKey == null) {
                _objKey = value;
                HMTObjID = GetObjID(_objKey);
            }
        }
    }

    private string _objKey = null;
    private string HMTObjID = null;

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

    public bool IsOccupied {
        get {
            return enemyList.Count > 0 || charaList.Count > 0;
        }
    }

    public bool IsOccupiedByPlayer {
        get {
            return charaList.Count > 0;
        }
    }

    public bool IsOccupiedByEnemy {
        get {
            return enemyList.Count > 0;
        }
    }

    /// <summary>
    /// A list of characters on the tile. This is a copy of the actual list, so it can be modified without affecting the actual list.
    /// </summary>
    public List<Character> CharacterList {
        get {
            return new List<Character>(charaList).OrderBy(c => c.CharacterId).ToList();
        }
    }

    /// <summary>
    /// A list of characters on the tile that are still alive. This is a copy of the actual list, so it can be modified without affecting the actual list.
    /// </summary>
    public List<Character> LivingCharacterList {
        get {
            return charaList.Select(c => c).Where(c => !c.dead).OrderBy(c => c.CharacterId).ToList();
        }
    }

    /// <summary>
    /// A list of the enemies on the tile. This is a copy of the actual list, so it can be modified without affecting the actual list.
    /// </summary>
    public List<Monster> MonsterList {
        get {
            return new List<Monster>(enemyList).OrderBy(m => m.ObjKey).ToList();
        }
    }

    public bool MultipleMonsters {
        get {
            return enemyList.Count > 1;
        }
    }

    // Lists of enemies and characters on this tile
    private List<Monster> enemyList;
    private List<Character> charaList;
    public List<Pin> pinList;
    public Shrine shrine = null;

    public Dictionary<int, FogOfWarState> fogOfWarDictionary;

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    public Material seen_material;
    public Material unseen_material;

    private void Awake()
    {
        charaList = new List<Character>();
        enemyList = new List<Monster>();
        seen_material = Resources.Load<Material>("seen");
        unseen_material = Resources.Load<Material>("unseen");
        fogOfWarDictionary = new Dictionary<int, FogOfWarState> {
            { 0, FogOfWarState.Unseen },
            { 1, FogOfWarState.Unseen },
            { 2, FogOfWarState.Unseen }
        };

    }
    private void Start()
    {

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            // Store the original materials
            originalMaterials[renderer] = renderer.materials;
        }
    }

    public void ClearMonsters() {
        foreach(Monster m in enemyList) {
            m.Kill();
            IntegratedGameManager.S.inSceneMonsters.Remove(m);
        }
        enemyList = new List<Monster>();
    }

    public void RemoveMonster(Monster monster) {
        enemyList.Remove(monster);
    }

    private void OnTriggerStay(Collider col)
    {
        if (col.gameObject.tag == "VisibleMask")
        {
            Character mask_character = col.gameObject.transform.parent.GetComponent<Character>();
            //Debug.Log(mask_character.CharacterId);
            fogOfWarDictionary[mask_character.CharacterId] = FogOfWarState.Visible;

        }
    }

    private void OnTriggerEnter(Collider col) {

        switch (col.gameObject.tag)
        {
            case "VisibleMask":
                //Debug.Log("Tile collide with mask-----------------------");
                Character mask_character = col.gameObject.transform.parent.GetComponent<Character>();
                //Debug.Log(fogOfWarDictionary[mask_character.CharacterId]);
                fogOfWarDictionary[mask_character.CharacterId] = FogOfWarState.Visible;
                //setRenderer(true);
                break;
            case "Monster":
                //Debug.Log("A monster enters.");
                Monster monster = col.gameObject.GetComponent<Monster>();
                monster.currentTile = this;
                enemyList.Add(monster);
                break;
            case "Character":
                //Debug.Log("A character enters.");
                Character character = col.gameObject.GetComponent<Character>();
                character.currentTile = this;
                charaList.Add(character);
                break;
            default:
                Debug.LogFormat("Tile Hit Trigger: {0}", col.gameObject.tag);
                break;
        }

        if (charaList.Count != 0 && enemyList.Count != 0)
        {
            foreach (var character in charaList)
            {
                if (character.dead)
                    charaList.Remove(character);
            }
            if (charaList.Count != 0)
                IntegratedGameManager.S.updateEventQueue(this);
        }

        if (tileType == ObstacleType.Trap && charaList.Count != 0)
        {
            IntegratedGameManager.S.updateEventQueue(this);
        }
        else if (tileType == ObstacleType.Rock && charaList.Count != 0)
        {
            IntegratedGameManager.S.updateEventQueue(this);
        }
        
    }

    private void OnTriggerExit(Collider col)
    {
        switch (col.gameObject.tag)
        {
            case "VisibleMask":
                //Debug.Log("Tile collide with mask-----------------------");
                Character mask_character = col.gameObject.transform.parent.GetComponent<Character>();
                //Debug.Log(fogOfWarDictionary[mask_character.CharacterId]);
                //Debug.Log("CharacterID: -------------------------");
                //Debug.Log(mask_character.CharacterId);
                fogOfWarDictionary[mask_character.CharacterId] = FogOfWarState.Seen;
                //setRenderer(false);
                break;
            case "Monster":
                //Debug.Log("A monster exits.");
                enemyList.Remove(col.gameObject.GetComponent<Monster>());
                break;
            case "Character":
                //Debug.Log("A character exits.");
                charaList.Remove(col.gameObject.GetComponent<Character>());
                break;
            default:
                Debug.LogFormat("Character Hit Trigger: {0}", col.gameObject.tag);
                break;
        }
    }

    

    public Vector3 GetNextPingLocation() {
        float pinOffsetSize = .3f;

        switch(pinList.Count) {
            case 0:
                return transform.position + pinOffsetSize * Vector3.back + pinOffsetSize * Vector3.right;
            case 1:
                return transform.position + pinOffsetSize * Vector3.right;
            case 2:
                return transform.position + pinOffsetSize * Vector3.back;
            case 3:
                return transform.position + pinOffsetSize *Vector3.forward + pinOffsetSize * Vector3.right;
            case 4:
                return transform.position + pinOffsetSize * Vector3.back + pinOffsetSize * Vector3.left;
            case 5:
                return transform.position + pinOffsetSize * Vector3.forward;
            case 6:
                return transform.position + pinOffsetSize * Vector3.left;
            case 7:
                return transform.position + pinOffsetSize * Vector3.forward + pinOffsetSize * Vector3.left;
            default:
                return transform.position;
        }
    }


    private void setRenderer(FogOfWarState state)
    {
        bool object_active;
        bool agent_active;
        if (state == FogOfWarState.Unseen) object_active = false; else object_active = true;
        if (state == FogOfWarState.Visible) agent_active = true; else agent_active = false;
        //turn off renderer for characters on the tile
        foreach (Character character in charaList)
        {
            Animator[] char_animators = character.GetComponentsInChildren<Animator>(true);
            foreach (Animator char_animator in char_animators)
            {
                if (char_animator != null)
                {

                    char_animator.enabled = agent_active;
                }
            }
            Renderer[] char_renderers = character.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer char_renderer in char_renderers)
            {
                if (char_renderer != null)
                {
                    if (char_renderer.gameObject.tag != "TileGround" && char_renderer.gameObject.tag != "VisibleMask")
                    {
                        char_renderer.enabled = agent_active;
                    }
                }
            }
        }
        //turn off renderer for monsters on the tile.
        foreach (Monster monster in enemyList)
        {
            Animator[] mons_animators = monster.GetComponentsInChildren<Animator>(true);
            foreach (Animator mons_animator in mons_animators)
            {
                if (mons_animator != null)
                {
                    mons_animator.enabled = agent_active;
                }
            }
            Renderer[] mons_renderers = monster.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer mons_renderer in mons_renderers)
            {
                if (mons_renderer != null)
                {
                    if (mons_renderer.gameObject.tag != "TileGround")
                    {
                        mons_renderer.enabled = agent_active;
                    }
                }
            }
        }

        //handle the stationary objects on the tile
        // Get all Animator components in children GameObjects, include inactive ones
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator.gameObject.tag == "ShrineStone")
            {
                animator.enabled = agent_active;
            }
            else if (animator != null)
            {
                animator.enabled = object_active;
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
                    if (renderer.gameObject.tag != "TileGround" && renderer.gameObject.tag != "Ping")
                    {
                        renderer.enabled = object_active;
                    }
                    else if (renderer.gameObject.tag != "Ping")
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
                    else { }
                }
                //if state is seen, turn on renderers for all stationary objects and apply seen shader
                else if (state == FogOfWarState.Seen)
                {
                    if (renderer.gameObject.tag == "ShrineStone")
                    {
                        renderer.enabled = agent_active;
                    }
                    else if (renderer.gameObject.tag != "Ping")
                    {
                        renderer.enabled = object_active;
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
                    else { }
                }
                //if state is visiable, turn on renderers for all stationary objects and revert to original shader
                else if (state == FogOfWarState.Visible)
                {
                    renderer.enabled = object_active;
                    foreach (var kvp in originalMaterials)
                    {
                        // Revert the materials back to the original ones
                        kvp.Key.materials = kvp.Value;
                    }
                }
                else Debug.LogError("Fog of War State Error in NetworkTile");
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



    public void SetFOWVisualsToCharacter(int characterID) {
        switch (fogOfWarDictionary[characterID]) {
            case FogOfWarState.Unseen:
                setRenderer(FogOfWarState.Unseen);
                break;
            case FogOfWarState.Seen:
                setRenderer(FogOfWarState.Seen);
                break;
            case FogOfWarState.Visible:
                Character focusChar = IntegratedGameManager.S.inSceneCharacters[characterID];
                if (focusChar.dead) {
                    if (this != focusChar.currentTile) {
                        setRenderer(FogOfWarState.Seen);
                    }
                    else {
                        setRenderer(FogOfWarState.Visible);
                    }
                }
                else {
                    setRenderer(FogOfWarState.Visible);
                }
                break;
        }
    }

    private void OnDestroy() {
        enemyList.Clear();
        charaList.Clear();
        pinList.Clear();
    }

    public JObject HMTStateRep() {
        JObject ret = new JObject();

        ret["entityType"] = tileType switch {
            ObstacleType.None => "Open",
            ObstacleType.Wall => "Wall",
            ObstacleType.Trap => "Trap",
            ObstacleType.Rock => "Rock",
            _ => "Open"
        };
        ret["objKey"] = _objKey;
        if(_objKey == "**") {
            ret["entityType"] = "Goal";
            ret["subGoalCount"] = IntegratedGameManager.S.goalCount;
        }
        ret["id"] = HMTObjID;
        ret["x"] = col;
        ret["y"] = row;
        if (tileType == ObstacleType.Rock || tileType == ObstacleType.Trap) {
            ret["challenge"] = dice.ToString();
        }

        return ret;
    }
}
