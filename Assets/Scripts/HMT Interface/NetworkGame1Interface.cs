using HMT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkGame1Interface : HMTInterface {
    
    [Header("Game Specific Settings")]
    public string[] IgnoreScenes;

    [Tooltip("The Index of the Dwarf Character in the GameManager's inScheneCharaters Array")]
    public int dwarfID = 0;
    [Tooltip("The Index of the Giant Character in the GameManager's inScheneCharaters Array")]
    public int giantID = 1;
    [Tooltip("The Index of the Human Character in the GameManager's inScheneCharaters Array")]
    public int humanID = 2;

    // Start is called before the first frame update
    protected override void Start() {
        base.Start();
        SceneManager.activeSceneChanged += OnSceneChange;
    }

    void OnSceneChange(Scene current, Scene next) {
        if (IgnoreScenes.Contains(next.name)) {
            return;
        }
    }




    public override string GetState(bool formated = false) {
        NetworkMapGenerator map = NetworkMapGenerator.Instance;
        NetworkGameManager gameManager = NetworkGameManager.S;

        JObject ret = new JObject();
        ret["gameData"] = new JObject {
             {"boardWidth", map.Map.GetLength(0)},
            {"boardHeight", map.Map.GetLength(1)},
            {"level", gameManager.currentLevel },
            {"currentPhase", gameManager.gameStatus.ToString() }
        };

        JArray scene = new JArray();

        for (int x = 0; x < map.Map.GetLength(0); x++) {
            for (int y = 0; y < map.Map.GetLength(1); y++) {
                if (map.Map[x, y] == null) {
                    Debug.LogErrorFormat("Map contains a null at {0},{1}", x, y);
                    continue;
                }
                NetworkTile tile = map.Map[x, y];
                switch (tile.gameObject.tag) {
                    case "Walls":
                        scene.Add(new JObject { { "type", "wall" }, { "x", x }, { "y", y } });
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
                foreach (NetworkCharacter character in tile.charaList) {
                    JObject rep = character.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    rep["pinCursorX"] = character.pingCursor.x + x;
                    rep["pinCursorY"] = character.pingCursor.y + x;
                    scene.Add(rep);
                }
                foreach (NetworkMonster monster in tile.enemyList) {
                    JObject rep = monster.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    scene.Add(rep);
                }
                foreach (NetworkPin pin in tile.pinList) {
                    JObject rep = pin.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    scene.Add(rep);
                }
                if (tile.shrine != null) {
                    JObject rep = tile.shrine.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    scene.Add(rep);
                }
            }
        }
        
        ret["scene"] = scene;
        if (formated) {
            return JsonConvert.SerializeObject(ret, Formatting.Indented);
        }
        else {
            return JsonConvert.SerializeObject(ret, Formatting.None);
        }
    }

    public override IEnumerator ExecuteAction(Command command) {
        NetworkGameManager manager = NetworkGameManager.S;
        switch (manager.gameStatus) {
            case GameConstant.GameStatus.GetReady:
                command.SendErrorResponse("Game not started yet");
                yield break;
            case GameConstant.GameStatus.Player_Pinning:
                yield return ExecuteActionInPinning(command);
                break;
            case GameConstant.GameStatus.Player_Planning:
                yield return ExecuteActionInPlanning(command);
                break;
            case GameConstant.GameStatus.Player_Moving:
                command.SendErrorResponse("Actions not accepted during Moving Phase");
                break;
            case GameConstant.GameStatus.Monster_Moving:
                command.SendErrorResponse("Actions not accepted during Monster Turn");
                break;
            case GameConstant.GameStatus.Animation_Pause:
                command.SendErrorResponse("Animation Pause Phase SHOULD be unreachable, report this.");
                yield break;
            case GameConstant.GameStatus.GameEnd:
                command.SendErrorResponse("Game is Over");
                yield break;
            default:
                Debug.LogWarningFormat("Execute Action called in Unknown Game State {0}", manager.gameStatus);
                command.SendErrorResponse("Unknown Game State");
                yield break;
        }
    }

    private NetworkCharacter GetTargetCharacter(string target) {
        switch (target.ToLower()) {
            case "giant":
                return NetworkGameManager.S.inSceneCharacters[giantID];
            case "human":
                return NetworkGameManager.S.inSceneCharacters[humanID];
            case "dwarf":
                return NetworkGameManager.S.inSceneCharacters[dwarfID];
            default:
                return null;
        }
    }

    //Refernce NetworkPinningSystems idx2PinPrefab
    private static int ResolvePinType(string pinType) {
        switch (pinType.ToLower()) {
            case "danger":
            case "a":
                return 1;
            case "assist":
            case "b":
                return 2;
            case "way":
            case "omw":
            case "c":
                return 3;
            case "unknown":
            case "question":
            case "d":
                return 0;
            default:
                return -1;
        }
    }

    private static NetworkCharacter.Direction DirectionFromString(string direct) {
        switch (direct.ToLower()) {
            case "up":
                return NetworkCharacter.Direction.Up;
            case "down":
                return NetworkCharacter.Direction.Down;
            case "left":
                return NetworkCharacter.Direction.Left;
            case "right":
                return NetworkCharacter.Direction.Right;
            default:
                Debug.LogErrorFormat("Unknown Direction {0}", direct);
                return NetworkCharacter.Direction.Wait;
        }
    }

    private IEnumerator ExecuteActionInPinning(Command command) {
        string action = command.json["action"].ToString();
        NetworkCharacter target = GetTargetCharacter(command.target);
        if(target == null) {
            command.SendErrorResponse(string.Format("Unknown target {0} in pinning phase. This shouldn't be possible please report it.", command.target));
            yield break;
        }

        NetworkGameManager manager = NetworkGameManager.S;
        JObject inputs;
        switch (action.ToLower()){
            case "pinga":
                command.json["inputs"] = new JObject { { "type", "danger" } };
                goto case "ping";
            case "pingb":
                command.json["inputs"] = new JObject { { "type", "assist" } };
                goto case "ping";
            case "pingc":
                command.json["inputs"] = new JObject { { "type", "way" } };
                goto case "ping";
            case "pingd":
                command.json["inputs"] = new JObject { { "type", "unknown" } };
                goto case "ping";
            case "ping":
                try {
                    inputs = command.json["inputs"] as JObject;
                }
                catch {
                    inputs = null;
                }
                if (inputs == null) {
                    command.SendErrorResponse("Error parsing pin parameters");
                    yield break;
                }

                int pinType = ResolvePinType(inputs["type"].ToString());
                Vector2Int pos = target.currentTile.GridPosition + target.pingCursor;

                if (inputs.ContainsKey("x")) {
                    pos.x = (int)inputs["x"];
                    pos.y = (int)inputs["y"];
                }

                if (!NetworkMapGenerator.Instance.InMap(pos)) {
                    command.SendErrorResponse(string.Format("Attempting to Ping outside of map {0}, {1}", pos.x, pos.y));
                    yield break;
                }

                NetworkPinningSystem.S.DropPinAt(pinType,pos.x, pos.y, target.CharacterId);
                target.PlacePin();
                break;

            case "submit":
                NetworkMiddleware.S.ReadyForNextPhaseLocal(target.CharacterId, true);
                //target.ReadyForNextPhase = true;
                command.SendOKResponse("Pings Submited");
                //manager.CheckPingPhaseEnd();  //Shouldn't need this any more because the RPC does the check.
                break;
            case "up":
            case "down":
            case "left":
            case "right":
                if (target.ActionPointsRemaining == 0) {
                    command.SendErrorResponse("No Action Points remaining so not moving ping cursor");
                }
                else {
                    NetworkMiddleware.S.MovePingCursorOnCharacterLocal(DirectionFromString(action), target.CharacterId);
                    //target.MovePingCusor(action);
                    command.SendOKResponse("Ping Cursor Moved");
                }
                yield break;
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
        NetworkGameManager manager = NetworkGameManager.S;
        NetworkCharacter target = GetTargetCharacter(command.target);
        if (target == null) {
            command.SendErrorResponse(string.Format("Unknown target {0} in pinning phase. This shouldn't be possible...", command.target));
            yield break;
        }
        switch (action.ToLower()) {
            case "pinga":
            case "pingb":
            case "pingc":
            case "pingd":
            case "ping":
                command.SendErrorResponse("Received ping action in planning phase");
                yield break;
            case "submit":
                NetworkMiddleware.S.ReadyForNextPhaseLocal(target.CharacterId, true);
                //target.ReadyForNextPhase = true;
                command.SendOKResponse("Plan Submited");
                //manager.CheckPlanPhaseEnd(); //Shouldn't need this any more because the RPC does the check.
                break;
            case "up":
                CheckAndAddMove(target, NetworkCharacter.Direction.Up, command);
                yield break;
            case "down":
                CheckAndAddMove(target, NetworkCharacter.Direction.Down, command);
                yield break;
            case "left":
                CheckAndAddMove(target, NetworkCharacter.Direction.Left, command);
                yield break;
            case "right":
                CheckAndAddMove(target, NetworkCharacter.Direction.Right, command);
                yield break;
            case "wait":
                CheckAndAddMove(target, NetworkCharacter.Direction.Wait, command);
                yield break;
            case "undo":
                if (!target.ReadyForNextPhase) {
                    if (target.ActionPlan.Count == 0) {
                        command.SendErrorResponse("Cannot undo action, no actions to undo");
                    }
                    else {
                        target.UndoPlanStep();
                        command.SendOKResponse("Action Undone");
                    }
                    yield break;
                }
                else {
                    command.SendErrorResponse("Cannot undo action after submitting");
                    yield break;
                }
            default:
                command.SendErrorResponse(string.Format("Unknown action {0} in pinning phase", action));
                yield break;
        }
        yield break;
    }

    private void CheckAndAddMove(NetworkCharacter target, NetworkCharacter.Direction move, Command command) {
        if (target.ActionPointsRemaining > 0) {
            if (target.CheckMove(move)) {
                NetworkMiddleware.S.AddMoveToCharacterLocal(move, target.CharacterId);
                //target.AddActionToPlan(move);
                command.SendOKResponse("Action Added");
            }
            else {
                command.SendErrorResponse("Illegal Move, Target is Impassible");
            }
        }
        else {
            command.SendErrorResponse("Illegal Move, Out of Action Points");
        }
    }
}
