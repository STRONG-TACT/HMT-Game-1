using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HMT;
using Photon.Pun.Demo.PunBasics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using Photon.Pun;
using Photon.Pun.Demo.Cockpit.Forms;
using System.Linq;

public class Game1Interface : HMTInterface {

    [Header("Game Specific Settings")]
    public string[] IgnoreScenes; 
    
    public KeyCode[] RecaptureHotKey;

    
    
    GameObject[] monsters;
    GameObject[] stones;
    GameObject[] traps;
    GameObject[] goals;
    GameObject[] walls;
    Character[] characters;
    GameObject door;


    GameData gameData;
    GameManager gameManager;

    protected override void Start() {
        base.Start();
        SceneManager.activeSceneChanged += OnSceneChange;
        //FindKeyObjects();
    }

    protected override void Update() {
        base.Update();
        if (CheckHotKey(RecaptureHotKey)) {
            Debug.Log("Recapturing KeyObjects");
            FindKeyObjects();
        }
    }

    public Vector2Int WorldPointToGridPosition(Vector3 point, float tileSize, float tileGap, Vector3 zeroPoint, bool ceil) {
        point -= zeroPoint;
        point /= (tileSize + tileGap);
        if(ceil) {
            return new Vector2Int(Mathf.CeilToInt(point.x), Mathf.CeilToInt(point.z));
        }
        else {
            return new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.z));
        }
        
    }

    public Vector2Int WorldPointToGridPosition(Vector3 point, float tileSize, float tileGap, Vector3 zeroPoint) {
        return WorldPointToGridPosition(point, tileSize, tileGap, zeroPoint, false);        
    }


    void FindKeyObjects() {
        monsters = GameObject.FindGameObjectsWithTag("Monster");
        stones = GameObject.FindGameObjectsWithTag("Rock");
        traps = GameObject.FindGameObjectsWithTag("Trap");
        goals = GameObject.FindGameObjectsWithTag("Goal");
        door = GameObject.FindGameObjectWithTag("Door");
        walls = GameObject.FindGameObjectsWithTag("Walls");
        characters = FindObjectsOfType<Character>();
        var manager = GameObject.Find("GameManager");
        gameManager = manager != null ? manager.GetComponent<GameManager>() : null;
        gameData = manager != null ? manager.GetComponent<GameData>() : null;

        Debug.LogFormat("FindKeyObjects found {0} Doors, {1} Goals, {2} Players, {3} Walls, {4} Monsters, {5} Traps, {6} Stones",
            door == null ? 0 : 1,
            goals.Length,
            characters.Length,
            walls.Length,
            monsters.Length,
            traps.Length,
            stones.Length);
        /*
         * Still need to find:
         *  The grid positions
         *      which can also be used to establish a discrete grid size instead of global space
         *  The walls
         *      not sure how best to represent those
         *  Players
         *  
         * 
         */
    }


    void OnSceneChange(Scene current, Scene next) {
        if (IgnoreScenes.Contains(next.name)) {
            return;
        }
        FindKeyObjects();
    }

    public void ConstructGrid() {
        Vector3 lowerLeft = door.transform.position;
        Vector3 upperRight = door.transform.position;
        foreach(GameObject wall in walls) {
            Bounds b = wall.GetComponent<BoxCollider>().bounds;
            lowerLeft = Vector3.Min(lowerLeft, b.max);
            upperRight = Vector3.Max(upperRight, b.min);
        }

        var boardWidth = upperRight.x - lowerLeft.x;
        var boardHeight = upperRight.z - lowerLeft.z;
    }

    /*
     * TODO: Add an "Action_in_progess" flag to the state
     * 
     */ 

    public override string GetState(bool formated) {

        if (door == null) {
            return "No Door Found, probably not in a scene or currently transitioning";
        }
        else {
            if(characters.Length ==0) {
                FindKeyObjects();
            }
            if(characters.Length == 0) {
                return "Found No Players, probably not in a scene or currently transitioning";
            }
        }

        Vector3 lowerLeft = door.transform.position;
        Vector3 upperRight = door.transform.position;
        foreach (GameObject wall in walls) {
            Bounds b = wall.GetComponent<BoxCollider>().bounds;
            lowerLeft = Vector3.Min(lowerLeft, b.max);
            upperRight = Vector3.Max(upperRight, b.min);
        }

        var boardWidth = upperRight.x - lowerLeft.x;
        var boardHeight = upperRight.z - lowerLeft.z;
        

        JObject ret = new JObject();
        ret["gameData"] = new JObject {
            {"tileSize", gameData.tileSize },
            {"tileGap", gameData.tileGapLength },
            {"boardWidth", boardWidth},
            {"boardHeight", boardHeight},
            {"gridWidth",  Mathf.CeilToInt((boardWidth - gameData.tileGapLength) / (gameData.tileSize + gameData.tileGapLength))},
            {"gridHeight", Mathf.CeilToInt((boardHeight- gameData.tileGapLength) / (gameData.tileSize + gameData.tileGapLength))},
            {"level", gameData.gameLevel },
            {"currentPlayer", GameManager.Instance.CurrentTurnPlayerNum },
            {"localPlayerId", PhotonNetwork.LocalPlayer.ActorNumber },
        };

        JArray scene = new JArray();

        //DOOR
        var pos = WorldPointToGridPosition(door.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
        scene.Add(new JObject {
            {"name", "door" },
            {"type","door" },
            {"x", pos.x },
            {"y", pos.y }
        });

        //GOALS 
        foreach(GameObject goal in goals) {
            if(goal == null) { continue; }
            pos = WorldPointToGridPosition(goal.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
            scene.Add(new JObject {
                {"name", goal.name },
                {"type", "goal" },
                {"x", pos.x },
                {"y", pos.y }
            });
        }

        //CHARACTERS
        foreach(Character character in characters) {
            if(character == null) { continue;  }
            pos = WorldPointToGridPosition(character.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
            var role = character.name[0] switch {
                'D' => "drawf",
                'G' => "giant",
                'H' => "human",
                _ => "UNKOWN"
            };
            //PlayerHealth health = character.GetComponent<PlayerHealth>();
            scene.Add(new JObject {
                {"name", character.name },
                {"controllingPlayerId", character.playerId }, //TODO this value is wrong
                {"type", role},
                {"moveCount", character.moveCount },
                {"movementRange", character.config.movement },
                {"sightRange", character.config.sightRange },
                {"dieFaces", new JArray(character.config.dieFaces) },
                {"health", character.Health}
            });
        }


        //MONSTERS
        foreach (GameObject monster in monsters) {
            if(monster == null) { continue; }
            pos = WorldPointToGridPosition(monster.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
            Monster mon = monster.GetComponent<Monster>();

            scene.Add(new JObject {
                {"name", mon.name },
                {"type", "monster" },
                {"monsterSize", mon.monsterType },
                {"targets", new JArray(mon.targetValues) },
                {"x", pos.x },
                {"y", pos.y }
            });
        }

        //TRAPS
        foreach(GameObject trap in traps) {
            if(trap == null) { continue; }
            pos = WorldPointToGridPosition(trap.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
            scene.Add(new JObject {
                {"name", trap.name },
                {"type","trap" },
                {"x", pos.x },
                {"y", pos.y }
            });
        }

        //ROCKS
        foreach (GameObject stone in stones) {
            if (stone == null) { continue; }
            pos = WorldPointToGridPosition(stone.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
            scene.Add(new JObject {
                {"name", stone.name },
                {"type","stone" },
                {"x", pos.x },
                {"y", pos.y }
            });
        }

        //WALLS??
        foreach (GameObject wall in walls) {
            Bounds bound = wall.GetComponent<Collider>().bounds;
            pos = WorldPointToGridPosition(bound.min, gameData.tileSize, gameData.tileGapLength, lowerLeft, true);
            scene.Add(new JObject {
                {"name", wall.name },
                {"type", "wall" },
                {"x", pos.x },
                {"y", pos.y },
                {"w", Mathf.Clamp(Mathf.Ceil((bound.size.x - gameData.tileGapLength) / (gameData.tileGapLength + gameData.tileSize)),1,float.PositiveInfinity) },
                {"h", Mathf.Clamp(Mathf.Ceil((bound.size.z - gameData.tileGapLength) / (gameData.tileGapLength + gameData.tileSize)),1,float.PositiveInfinity) }
            }); ;
        }


        ret["scene"] = scene;
        if (formated) {
            return JsonConvert.SerializeObject(ret, Formatting.Indented);
        }
        else {
            return JsonConvert.SerializeObject(ret, Formatting.None);
        }
    }

    public bool MyTurn {
        get {
            return GameManager.Instance.CurrentTurnPlayerNum == PhotonNetwork.LocalPlayer.ActorNumber;
        }
    }

    public Character LocalCharacter {
        get {
            if (gameData != null && gameData.Initialized && PlayerMapper.Instance.Inititialized) {
                return gameData.inSceneCharacters[PlayerMapper.Instance.LocalCharacterNumber];
            }
            else {
                return null;
            }
        }
    }

    public override string ExecuteAction(JObject actionJob) {
        if (!MyTurn) {
            return "Not your turn";
        }

        string action = actionJob["action"].ToString();
        switch (action.ToLower()) {
            case "move":
                string direction = ((JArray)actionJob["inputs"])[0].ToString();
                switch (direction.ToLower()) {
                    case "up":
                        GameManager.Instance.CallMoveCharacter(PlayerMapper.Instance.LocalCharacterNumber, Character.Direction.Up);
                        break;
                    case "down":
                        GameManager.Instance.CallMoveCharacter(PlayerMapper.Instance.LocalCharacterNumber, Character.Direction.Down);
                        break;
                    case "left":
                        GameManager.Instance.CallMoveCharacter(PlayerMapper.Instance.LocalCharacterNumber, Character.Direction.Left);
                        break;
                    case "right":
                        GameManager.Instance.CallMoveCharacter(PlayerMapper.Instance.LocalCharacterNumber, Character.Direction.Right);
                        break;
                    default:
                        return string.Format("Unknown Direction: {0}", direction);
                }
                return "OK";
            case "interact":
                if(CombatSystem.Instance.State == CombatSystem.FightState.Waiting) {
                    GameManager.Instance.CallRollDie();
                }
                break;
            default:
                return string.Format("Unknown Action: {0}", action);
        }

        return string.Empty;

    }

}
