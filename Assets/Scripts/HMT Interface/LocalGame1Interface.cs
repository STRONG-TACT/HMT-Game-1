using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HMT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using System.Linq;

public class LocalGame1Interface : HMTInterface {

    [Header("Game Specific Settings")]
    public string[] IgnoreScenes; 

    public int giantID = 0;
    public int dwarfID = 1;
    public int humanID = 2;

    GameData gameData;

    protected override void Start() {
        base.Start();
        SceneManager.activeSceneChanged += OnSceneChange;
        //FindKeyObjects();
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


    void OnSceneChange(Scene current, Scene next) {
        if (IgnoreScenes.Contains(next.name)) {
            return;
        }
    }


    /*
     * TODO: Add an "Action_in_progess" flag to the state
     * 
     */ 

    public override string GetState(bool formated) {
        MapGenerator map = MapGenerator.Instance;
        LocalGameManager gameManager = LocalGameManager.Instance;

        JObject ret = new JObject();
        ret["gameData"] = new JObject {
            //{"tileGap", gameData.tileGapLength },
            {"boardWidth", map.Map.GetLength(0)},
            {"boardHeight", map.Map.GetLength(1)},
            //{"gridWidth",  Mathf.CeilToInt((boardWidth - gameData.tileGapLength) / (gameData.tileSize + gameData.tileGapLength))},
            //{"gridHeight", Mathf.CeilToInt((boardHeight- gameData.tileGapLength) / (gameData.tileSize + gameData.tileGapLength))},
            {"level", gameData.gameLevel },
            {"currentPlayer", GameManager.Instance.CurrentTurnPlayerNum },
            {"localPlayerId", PhotonNetwork.LocalPlayer.ActorNumber },
            {"currentPhase", gameManager.gameStatus.ToString() }
        };
        
        JArray scene = new JArray();

        for(int x=0; x < map.Map.GetLength(0); x++) {
            for(int y=0; y < map.Map.GetLength(1); y++) {
                if (map.Map[x,y] == null) { 
                    Debug.LogErrorFormat("Map contains a null at {0},{1}", x, y);    
                    continue; 
                }
                LocalTile tile = map.Map[x,y];
                switch (tile.gameObject.tag) {
                    case "Walls":
                        scene.Add(new JObject { { "type","wall"}, {"x", x }, { "y", y } });
                        break;
                    case "Trap":
                        scene.Add(new JObject { { "type", "trap" }, { "x", x }, { "y", y },
                            {"challenge", tile.dice.ToString() }
                        });
                        break;
                    case "Rock":
                        scene.Add(new JObject { { "type", "rock" }, { "x", x }, { "y", y },
                            {"challenge", tile.dice.ToString() }
                        });
                        break;
                    case "Door":
                        scene.Add(new JObject { { "type", "goal" }, { "x", x }, { "y", y },
                            {"subgoalCount", gameManager.goalCount } });
                        break;
                }
                foreach (LocalCharacter character in tile.charaList) {
                    JObject rep = character.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    scene.Add(rep);
                }
                foreach (LocalMonster monster in tile.enemyList) {
                    JObject rep = monster.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    scene.Add(rep);
                }
                foreach (LocalPin pin in tile.pinList) {
                    JObject rep = pin.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    scene.Add(rep);
                }
            }
        }


        //DOOR
        //var pos = WorldPointToGridPosition(door.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
        //scene.Add(new JObject {
        //    {"name", "door" },
        //    {"type","door" },
        //    {"x", pos.x },
        //    {"y", pos.y }
        //});

        //GOALS 
        //foreach(GameObject goal in goals) {
        //    if(goal == null) { continue; }
        //    pos = WorldPointToGridPosition(goal.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
        //    scene.Add(new JObject {
        //        {"name", goal.name },
        //        {"type", "goal" },
        //        {"x", pos.x },
        //        {"y", pos.y }
        //    });
        //}

        //CHARACTERS
        //foreach(Character character in characters) {
        //    if(character == null) { continue;  }
        //    pos = WorldPointToGridPosition(character.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
        //    var role = character.name[0] switch {
        //        'D' => "drawf",
        //        'G' => "giant",
        //        'H' => "human",
        //        _ => "UNKOWN"
        //    };
        //    //PlayerHealth health = character.GetComponent<PlayerHealth>();
        //    scene.Add(new JObject {
        //        {"name", character.name },
        //        {"controllingPlayerId", PlayerMapper.Instance.GetPlayerIdFromCharacter(character)}, //TODO this value is wrong
        //        {"type", role},
        //        {"moveCount", character.moveCount },
        //        {"movementRange", character.config.movement },
        //        {"sightRange", character.config.sightRange },
        //        {"dieFaces", new JArray(character.config.dieFaces) },
        //        {"health", character.Health}
        //    });
        //}


        //MONSTERS
        //foreach (GameObject monster in monsters) {
        //    if(monster == null) { continue; }
        //    pos = WorldPointToGridPosition(monster.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
        //    Monster mon = monster.GetComponent<Monster>();

        //    scene.Add(new JObject {
        //        {"name", mon.name },
        //        {"type", "monster" },
        //        {"monsterSize", mon.monsterType },
        //        {"targets", new JArray(mon.targetValues) },
        //        {"x", pos.x },
        //        {"y", pos.y }
        //    });
        //}

        //TRAPS
        //foreach(GameObject trap in traps) {
        //    if(trap == null) { continue; }
        //    pos = WorldPointToGridPosition(trap.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
        //    scene.Add(new JObject {
        //        {"name", trap.name },
        //        {"type","trap" },
        //        {"x", pos.x },
        //        {"y", pos.y }
        //    });
        //}

        //ROCKS
        //foreach (GameObject stone in stones) {
        //    if (stone == null) { continue; }
        //    pos = WorldPointToGridPosition(stone.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
        //    scene.Add(new JObject {
        //        {"name", stone.name },
        //        {"type","stone" },
        //        {"x", pos.x },
        //        {"y", pos.y }
        //    });
        //}

        //WALLS??
        //foreach (GameObject wall in walls) {
        //    Bounds bound = wall.GetComponent<Collider>().bounds;
        //    pos = WorldPointToGridPosition(bound.min, gameData.tileSize, gameData.tileGapLength, lowerLeft, true);
        //    scene.Add(new JObject {
        //        {"name", wall.name },
        //        {"type", "wall" },
        //        {"x", pos.x },
        //        {"y", pos.y },
        //        {"w", Mathf.Clamp(Mathf.Ceil((bound.size.x - gameData.tileGapLength) / (gameData.tileGapLength + gameData.tileSize)),1,float.PositiveInfinity) },
        //        {"h", Mathf.Clamp(Mathf.Ceil((bound.size.z - gameData.tileGapLength) / (gameData.tileGapLength + gameData.tileSize)),1,float.PositiveInfinity) }
        //    }); ;
        //}

        ret["scene"] = scene;
        if (formated) {
            return JsonConvert.SerializeObject(ret, Formatting.Indented);
        }
        else {
            return JsonConvert.SerializeObject(ret, Formatting.None);
        }
    }

    public override IEnumerator ExecuteAction(Command command) {
        LocalGameManager manager = LocalGameManager.Instance;
        switch (manager.gameStatus) {
            case LocalGameManager.GameStatus.GetReady:
                command.SendErrorResponse("Game not started yet");
                yield break;
            case LocalGameManager.GameStatus.Player_Pinning:
                yield return ExecuteActionInPinning(command);
                break;
            case LocalGameManager.GameStatus.Player_Planning:
                yield return ExecuteActionInPlanning(command);
                break;
            case LocalGameManager.GameStatus.Player_Moving:
                //TODO maybe we accept a next or submit action for combat 
                command.SendErrorResponse("Actions Not Accepted on Monster Turn");
                yield break;
            case LocalGameManager.GameStatus.Monster_Moving:
                //TODO maybe we accept a next or submit action for combat 
                command.SendErrorResponse("Actions Not Accepted on Monster Turn");
                yield break;
            case LocalGameManager.GameStatus.Animation_Pause:
                command.SendErrorResponse("Animation Pause Phase SHOULD be unreachable, report this.");
                yield break;
            case LocalGameManager.GameStatus.GameEnd:
                command.SendErrorResponse("Game is Over");
                yield break;
            default:
                Debug.LogWarningFormat("Execute Action called in Unknown Game State {0}", manager.gameStatus);
                command.SendErrorResponse("Unknown Game State");
                yield break;
        }
    }

    private LocalCharacter GetTargetCharacter(string target) {
        switch (target.ToLower()) {
            case "giant":
                return LocalGameManager.Instance.inSceneCharacters[giantID];
            case "human":
                return LocalGameManager.Instance.inSceneCharacters[humanID];
            case "dwarf":
                return LocalGameManager.Instance.inSceneCharacters[dwarfID];
            default:
                return null;
        }
    }

    private IEnumerator ExecuteActionInPinning(Command command) {
        string action = command.json["action"].ToString();
        LocalCharacter target = GetTargetCharacter(command.target);
        if (target == null) {
            command.SendErrorResponse(string.Format("Unknown target {0} in pinning phase. This shouldn't be possible...", command.target));
            yield break;
        }
        LocalGameManager manager = LocalGameManager.Instance; 
        switch (action) {
            case "ping":
                //TODO do the ping
                break;
            case "submit":
                //TODO end ping phase
                break;
            case "up":
            case "down":
            case "left":
            case "right":
                if (!target.MovePingCusor(action)) {
                    command.SendErrorResponse("No Action Points remaining so not moving ping cursor");
                    yield break;
                }
                break;
            case "undo":
                command.SendErrorResponse(string.Format("Received {0} action in pinning phase", action));
                yield break;
            default:
                command.SendErrorResponse(string.Format("Unknown action {0} in pinning phase", action));
                yield break;
        }
        yield break;
    }

    private IEnumerator ExecuteActionInPlanning(Command command) {
        string action = command.json["action"].ToString();
        LocalGameManager manager = LocalGameManager.Instance;
        switch (action) {
            case "ping":
                command.SendErrorResponse("Received ping action in planning phase");
                yield break;
            case "submit":
                //TODO end ping phase
                break;
            case "up":
                //TODO move character up
                break;
            case "down":
                //TODO move character down
                break;
            case "left":
                //TODO move character left
                break;
            case "right":
                //TODO move character right
                break;
            case "undo":
                //TODO undo last move
                break;
            default:
                command.SendErrorResponse(string.Format("Unknown action {0} in pinning phase", action));
                yield break;
        }
        yield break;
    }

}
