using ExitGames.Client.Photon.StructWrapping;
using HMT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntegratedGame1Interface : HMTInterface {
    
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

    public override IEnumerator ProcessCommand(Command command) {

        switch (IntegratedGameManager.S.gameStatus) {
            case GameConstant.GameStatus.GetReady:
                command.SendErrorResponse("Game is not started yet.", 1002);
                break;
            case GameConstant.GameStatus.GameEnd:
                command.SendGameOverResponse("Game Over"); // may want to send more interesting information than this
                yield break;
            case GameConstant.GameStatus.Animation_Pause:
                command.SendErrorResponse("Game in Deprecated Phase. This shouldn't be possible please report it.", 9001);
                yield break;


            case GameConstant.GameStatus.Player_Moving:
            case GameConstant.GameStatus.Monster_Moving:
                if(command.command == "execute_action") {
                    command.SendIllegalActionResponse("Attempted Action in Movement Phase, actions are not allowed.", 2000);
                    yield break;
                }
                break;
            case GameConstant.GameStatus.Player_Pinning:
            case GameConstant.GameStatus.Player_Planning:
                break;
            default:
                Debug.LogWarningFormat("Execute Action called in Unknown Game State {0}", IntegratedGameManager.S.gameStatus);
                command.SendErrorResponse(string.Format("Game in Unknown Phase: {0}", IntegratedGameManager.S.gameStatus), 9000);
                yield break;
        }


        switch (command.command) {
            case "get_full_state":
                string state = GetFullState(command);
                command.SendOKResponse("Full State", state);
                yield break;
            case "get_fow_state":
                state = GetFOWState(command);
                command.SendOKResponse("FOW State", state);
                yield break;
            default:
                yield return base.ProcessCommand(command);
                break;
        }
    }

    public override string GetState(Command command) {
        IntegratedMapGenerator map = IntegratedMapGenerator.Instance;
        IntegratedGameManager gameManager = IntegratedGameManager.S;

        Character target = GetTargetCharacter(command.target);
        if (target == null) {
            command.SendErrorResponse(string.Format("Unknown HMT Target {0}. Make sure the targets are configured correctly in the Inspector", command.target), 9002);
            return null;
        }

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
                Tile tile = map.Map[x, y];
                JObject rep;
                switch (tile.fogOfWarDictionary[target.CharacterId]) {
                    case Tile.FogOfWarState.Visible:
                        switch (tile.gameObject.tag) {
                            case "Walls":
                            case "Trap":
                            case "Rock":
                                rep = tile.HMTStateRep();
                                rep["x"] = x;
                                rep["y"] = y;
                                scene.Add(rep);
                                break;
                            case "Door":
                            case "Goal":
                                rep = tile.HMTStateRep();
                                rep["x"] = x;
                                rep["y"] = y;
                                rep["subGoalCount"] = gameManager.goalCount;
                                scene.Add(rep);
                                break;
                        }
                        foreach (Character character in tile.charaList) {
                            if (character == target) {
                                rep = character.HMTStateRep(Character.StateRepLevel.Full);
                                rep["x"] = x;
                                rep["y"] = y;
                                rep["pinCursorX"] = character.pingCursor.x + x;
                                rep["pinCursorY"] = character.pingCursor.y + x;
                            }
                            else {
                                rep = character.HMTStateRep(Character.StateRepLevel.TeamVisible);
                                rep["x"] = x;
                                rep["y"] = y;
                            }
                            scene.Add(rep);
                        }
                        foreach (Monster monster in tile.enemyList) {
                            rep = monster.HMTStateRep();
                            rep["x"] = x;
                            rep["y"] = y;
                            scene.Add(rep);
                        }
                        if (tile.shrine != null) {
                            rep = tile.shrine.HMTStateRep();
                            rep["x"] = x;
                            rep["y"] = y;
                            scene.Add(rep);
                        }
                        break;
                    case Tile.FogOfWarState.Seen:
                    case Tile.FogOfWarState.Unseen:
                        if (tile.shrine != null) {
                            rep = tile.shrine.HMTStateRep();
                            scene.Add(rep);
                        }
                        break;
                }
                foreach (Pin pin in tile.pinList) { //this one moves out
                    rep = pin.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    scene.Add(rep);
                }
            }
        }

        ret["scene"] = scene;
        return JsonConvert.SerializeObject(ret, Formatting.None);
    }

    public string GetFullState(Command command) {
        IntegratedMapGenerator map = IntegratedMapGenerator.Instance;
        IntegratedGameManager gameManager = IntegratedGameManager.S;

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
                Tile tile = map.Map[x, y];
                JObject rep;
                switch (tile.gameObject.tag) {
                    case "Walls":
                    case "Trap":
                    case "Rock":
                        rep = tile.HMTStateRep();
                        rep["x"] = x;
                        rep["y"] = y;
                        scene.Add(rep);
                        break;
                    case "Door":
                    case "Goal":
                        rep = tile.HMTStateRep();
                        rep["x"] = x;
                        rep["y"] = y;
                        rep["subGoalCount"] = gameManager.goalCount;
                        scene.Add(rep);
                        break;
                }
                foreach (Character character in tile.charaList) {
                    rep = character.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    rep["pinCursorX"] = character.pingCursor.x + x;
                    rep["pinCursorY"] = character.pingCursor.y + x;
                    scene.Add(rep);
                }
                foreach (Monster monster in tile.enemyList) {
                    rep = monster.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    scene.Add(rep);
                }
                foreach (Pin pin in tile.pinList) {
                    rep = pin.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    scene.Add(rep);
                }
                if (tile.shrine != null) {
                    rep = tile.shrine.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    scene.Add(rep);
                }
            }
        }

        ret["scene"] = scene;
        return JsonConvert.SerializeObject(ret, Formatting.None);
    }

    public string GetFOWState(Command command) {
        IntegratedMapGenerator map = IntegratedMapGenerator.Instance;
        IntegratedGameManager gameManager = IntegratedGameManager.S;

        Character target = GetTargetCharacter(command.target);
        if (target == null) {
            command.SendErrorResponse(string.Format("Unknown HMT Target {0}. Make sure the targets are configured correctly in the Inspector", command.target), 9002);
            return null;
        }

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
                Tile tile = map.Map[x, y];
                JObject rep;
                switch (tile.fogOfWarDictionary[target.CharacterId]) {
                    case Tile.FogOfWarState.Visible:
                        switch (tile.gameObject.tag) {
                            case "Walls":
                            case "Trap":
                            case "Rock":
                                rep = tile.HMTStateRep();
                                rep["x"] = x;
                                rep["y"] = y;
                                scene.Add(rep);
                                break;
                            case "Door":
                            case "Goal":
                                rep = tile.HMTStateRep();
                                rep["x"] = x;
                                rep["y"] = y;
                                rep["subGoalCount"] = gameManager.goalCount;
                                scene.Add(rep);
                                break;
                        }
                        foreach (Character character in tile.charaList) {
                            if (character == target) {
                                rep = character.HMTStateRep(Character.StateRepLevel.Full);
                                rep["x"] = x;
                                rep["y"] = y;
                                rep["pinCursorX"] = character.pingCursor.x + x;
                                rep["pinCursorY"] = character.pingCursor.y + x;
                            }
                            else {
                                rep = character.HMTStateRep(Character.StateRepLevel.TeamVisible);
                                rep["x"] = x;
                                rep["y"] = y;
                            }
                            scene.Add(rep);
                        }
                        foreach (Monster monster in tile.enemyList) {
                            rep = monster.HMTStateRep();
                            rep["x"] = x;
                            rep["y"] = y;
                            scene.Add(rep);
                        }
                        if (tile.shrine != null) {
                            rep = tile.shrine.HMTStateRep();
                            rep["x"] = x;
                            rep["y"] = y;
                            scene.Add(rep);
                        }
                        break;
                    case Tile.FogOfWarState.Seen:
                        switch (tile.gameObject.tag) {
                            case "Walls":
                            case "Trap":
                            case "Rock":
                                rep = tile.HMTStateRep();
                                rep["x"] = x;
                                rep["y"] = y;
                                scene.Add(rep);
                                break;
                            case "Door":
                            case "Goal":
                                rep = tile.HMTStateRep();
                                rep["x"] = x;
                                rep["y"] = y;
                                rep["subGoalCount"] = gameManager.goalCount;
                                scene.Add(rep);
                                break;
                        }
                        goto case Tile.FogOfWarState.Unseen;
                    case Tile.FogOfWarState.Unseen:
                        if (tile.shrine != null) {
                            rep = tile.shrine.HMTStateRep();
                            scene.Add(rep);
                        }
                        break;
                }
                foreach (Pin pin in tile.pinList) { //this one moves out
                    rep = pin.HMTStateRep();
                    rep["x"] = x;
                    rep["y"] = y;
                    scene.Add(rep);
                }
            }
        }

        ret["scene"] = scene;
        return JsonConvert.SerializeObject(ret, Formatting.None);
    }

    public override IEnumerator ExecuteAction(Command command) {
        IntegratedGameManager manager = IntegratedGameManager.S;
        switch (manager.gameStatus) {  
            case GameConstant.GameStatus.Player_Pinning:
                yield return ExecuteActionInPinning(command);
                break;
            case GameConstant.GameStatus.Player_Planning:
                yield return ExecuteActionInPlanning(command);
                break;
        }
    }

    private Character GetTargetCharacter(string target) {
        switch (target.ToLower()) {
            case "giant":
                return IntegratedGameManager.S.inSceneCharacters[giantID];
            case "human":
                return IntegratedGameManager.S.inSceneCharacters[humanID];
            case "dwarf":
                return IntegratedGameManager.S.inSceneCharacters[dwarfID];
            default:
                return null;
        }
    }

    //Refernce PinningSystems idx2PinPrefab
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

    private static Character.Direction DirectionFromString(string direct) {
        switch (direct.ToLower()) {
            case "up":
                return Character.Direction.Up;
            case "down":
                return Character.Direction.Down;
            case "left":
                return Character.Direction.Left;
            case "right":
                return Character.Direction.Right;
            case "wait":
                return Character.Direction.Wait;
            default:
                Debug.LogErrorFormat("Unknown Direction {0}", direct);
                return Character.Direction.Wait;
        }
    }

    private IEnumerator ExecuteActionInPinning(Command command) {
        string action = command.json["action"].ToString();
        Character target = GetTargetCharacter(command.target);
        if(target == null) {
            command.SendErrorResponse(string.Format("Unknown HMT Target {0}. Make sure the targets are configured correctly in the Inspector", command.target),9002);
            yield break;
        }

        IntegratedGameManager manager = IntegratedGameManager.S;
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
                    command.SendErrorResponse("Error parsing pin parameters", 9003);
                    yield break;
                }

                int pinType = ResolvePinType(inputs["type"].ToString());
                Vector2Int pos = target.currentTile.GridPosition + target.pingCursor;

                if (inputs.ContainsKey("x")) {
                    pos.x = (int)inputs["x"];
                    pos.y = (int)inputs["y"];
                }

                if (!IntegratedMapGenerator.Instance.InMap(pos)) {
                    command.SendIllegalActionResponse(string.Format("Attempting to pin outside of map {0}, {1}", pos.x, pos.y),2001);
                    yield break;
                }

                PinningSystem.S.DropPinAt(pinType,pos.x, pos.y, target.CharacterId);
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
                    command.SendIllegalActionResponse("No Action Points Remaining on Pin Move",2002);
                }
                else {
                    NetworkMiddleware.S.MovePingCursorOnCharacterLocal(DirectionFromString(action), target.CharacterId);
                    //target.MovePingCusor(action);
                    command.SendOKResponse("Ping Cursor Moved");
                }
                yield break;
            case "undo":
                command.SendIllegalActionResponse("Cannot Undo in Pinning Phase",2003);
                yield break;
            default:
                command.SendErrorResponse("Unknown Action in Pinning Phase", UnknownActionContent(action.ToLower(), 1003));
                yield break;
        }
        yield break;
    }

    private static string UnknownActionContent(string action, int errorCode) {
        return @"{
            ""errorCode"":erroCode,
            ""attemptedAction"":action,
            ""validActions"":[""pinga"",""pingb"",""pingc"",""pingd"",""ping"",""submit"",""up"",""down"",""left"",""right"",""undo""]}";
    }

    private IEnumerator ExecuteActionInPlanning(Command command) {
        string action = command.json["action"].ToString();
        IntegratedGameManager manager = IntegratedGameManager.S;
        Character target = GetTargetCharacter(command.target);
        if (target == null) {
            command.SendErrorResponse(string.Format("Unknown HMT Target {0}. Make sure the targets are configured correctly in the Inspector", command.target), 9002);
            yield break;
        }
        switch (action.ToLower()) {
            case "pinga":
            case "pingb":
            case "pingc":
            case "pingd":
            case "ping":
                command.SendIllegalActionResponse("Pin Command Sent in Planning Phase",2004);
                yield break;
            case "submit":
                NetworkMiddleware.S.ReadyForNextPhaseLocal(target.CharacterId, true);
                //target.ReadyForNextPhase = true;
                command.SendOKResponse("Plan Submited");
                //manager.CheckPlanPhaseEnd(); //Shouldn't need this any more because the RPC does the check.
                break;
            case "up":
                CheckAndAddMove(target, Character.Direction.Up, command);
                yield break;
            case "down":
                CheckAndAddMove(target, Character.Direction.Down, command);
                yield break;
            case "left":
                CheckAndAddMove(target, Character.Direction.Left, command);
                yield break;
            case "right":
                CheckAndAddMove(target, Character.Direction.Right, command);
                yield break;
            case "wait":
                CheckAndAddMove(target, Character.Direction.Wait, command);
                yield break;
            case "undo":
                if (!target.ReadyForNextPhase) {
                    if (target.ActionPlan.Count == 0) {
                        command.SendIllegalActionResponse("Cannot undo action, no actions to undo",2005);
                    }
                    else {
                        target.UndoPlanStep();
                        command.SendOKResponse("Action Undone");
                    }
                    yield break;
                }
                else {
                    command.SendIllegalActionResponse("Cannot undo action after submitting",2006);
                    yield break;
                }
            default:
                command.SendErrorResponse("Unknown Action in Planning Phase", UnknownActionContent(action.ToLower(), 1003));
                yield break;
        }
        yield break;
    }

    private void CheckAndAddMove(Character target, Character.Direction move, Command command) {
        if (target.ActionPointsRemaining > 0) {
            if (target.CheckMove(move)) {
                NetworkMiddleware.S.AddMoveToCharacterLocal(move, target.CharacterId);
                //target.AddActionToPlan(move);
                command.SendOKResponse("Action Added");
            }
            else {
                command.SendIllegalActionResponse("Target is Impassible", 2007);
            }
        }
        else {
            command.SendIllegalActionResponse("Out of Action Points", 2008);
        }
    }
}