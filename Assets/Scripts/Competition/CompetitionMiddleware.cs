using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using GameConstant;
using Photon.Pun;
using System.Linq;

public class CompetitionMiddleware : MonoBehaviour {

    public static CompetitionMiddleware Instance = null;

    public string flaskURL = "https://localhost";
    public string serverKey = "NOTSET";
    public bool overrideAIMode = true;
    [Tooltip("Whether logs should be send to the server.")]
    public bool HttpSendLogs = true;
    [Tooltip("Wheter logs should be printed to the console window and, in compiled builds, to the player log file.")]
    public bool DebugTraceLogs = true;

    public string UserID { get { return currUserID; } }

    public string SessionID { get { return currSessionID; } }

    public struct AgentRecord {
        public string agentID;
        public string sessionID;
        public string target;
        public int characterID;

        public AgentRecord(string agentID, string sessionID, string target, int characterID) {
            this.agentID = agentID;
            this.sessionID = sessionID;
            this.target = target;
            this.characterID = characterID;
        }
    }

    public Dictionary<int, AgentRecord> RegisteredAgents = new Dictionary<int, AgentRecord>();

    public bool IsAI {
        get {
#if HMT_BUILD
            return !overrideAIMode;
#else
            return false;
#endif
        }
    }

    public bool LogSystemEvents {
        get {
            return PhotonNetwork.IsMasterClient || (IntegratedGameManager.S != null && !IntegratedGameManager.S.isNetworkGame);
        }
    }

    private string currUserID = null;
    private string currSessionID = null;
    private string currGameId = null;
    private string currLevel = null;
    private int currRound = -1;
    private string currPhase = null;

    // Start is called before the first frame update
    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else {
            this.RegisteredAgents = new Dictionary<int, AgentRecord>();
            this.currSessionID = null;
            Destroy(this.gameObject);
        }
    }

    // Update is called once per frame
    void Update() {

    }

    private void OnDestroy() {
        if (RegisteredAgents.Count > 0) {
            foreach (AgentRecord entry in RegisteredAgents.Values) {
                CallLogEvent(entry.agentID, entry.sessionID, 1001, "system", "end_session", "session_id", entry.sessionID);
            }
        }
        else if(currSessionID != null) {
            LogEndSession();
        }
    }

    private void SendPostRequestImmediate(string url, string json, bool supressDebug = false) {
        using (UnityWebRequest www = UnityWebRequest.Post(url, json)) {
            www.SetRequestHeader("Content-type", "application/json");
            if (!supressDebug) {
                Debug.LogFormat("Sending {0} request to {1} with data {2}, with", www.method, www.url, json);
            }
            www.SendWebRequest();
        }
    }


    private IEnumerator SendPostRequestFireAndForget(string url, string json, bool supressDebug = false) {
        using (UnityWebRequest www = UnityWebRequest.Post(url, json)) {
            if (!supressDebug) {
                Debug.LogFormat("Sending {0} request to {1} with data {2}, with", www.method, www.url, json);
            }
            www.SetRequestHeader("Content-type", "application/json");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogErrorFormat("{0} on call to {1}, with {2}", www.error, url, json);
            }else {
                Debug.LogFormat("HTTP Request Response {0}: {1}", www.responseCode, www.result);
            }
        }
    }

    private IEnumerator SendPostRequestWithCallback(string url, string json, System.Action<JObject> callback, bool supressDebug=false) {
        using (UnityWebRequest www = UnityWebRequest.Post(url, json)) {
            if (!supressDebug) {
                Debug.LogFormat("Sending {0} request to {1} with data {2}, with", www.method, www.url, json);
            }
            www.SetRequestHeader("Content-type", "application/json");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogErrorFormat("{0} on call to {1}, with {2}", www.error, url, json);
            }
            else {
                callback(JsonConvert.DeserializeObject<JObject>(www.downloadHandler.text));
            }
        }
    }

    public void SetUserID(string userID) {
        if (this.currUserID != null) {
            Debug.LogWarning("Setting currUserID when already set.");
        }
        this.currUserID = userID;
        LogStartSession();
    }

    public void SetGameID(string gameID) {
        this.currGameId = gameID;
    }

    public void AddAIAgent(string target, string agentID, int characterID) {
        Debug.LogFormat("Registering AI agent {0} with target {1} and characterID {2}", agentID, target, characterID);

        AgentRecord record = new AgentRecord(agentID, System.Guid.NewGuid().ToString(), target, characterID);
        RegisteredAgents[characterID] = record;

        LogHMTConnect(characterID);
        CallLogEvent(agentID, record.sessionID, 1000, "system", "start_session", "session_id", currSessionID);
    }

    public void CallListAgents(System.Action<JObject> callback) {
        //this till need to be a coroutine in some way
        SendPostRequestWithCallback(flaskURL + "/list_agents", JsonConvert.SerializeObject(new JObject { { "api_key", serverKey } }), callback);
    }

    public void CallLaunchGame(string photonRoom, string dwarfPlayerId, string giantPlayerId, string humanPlayerId) {
        //this may benefit form being a coroutine as well

        JObject obj = new JObject {
            { "api_key", serverKey },
            { "photon_room", photonRoom },
        };
        obj["characters"] = new JObject {
            { "dwarf", dwarfPlayerId },
            { "giant", giantPlayerId },
            { "human", humanPlayerId }
        };

        StartCoroutine(SendPostRequestFireAndForget(flaskURL + "/launch_game", JsonConvert.SerializeObject(obj)));
    }


    public void CallReportResult() {
        //TODO this needs to be filled
    }

    private void CallLogEvent(string userID, string sessionID, int eventId, string actor, string verb, string label, int value, bool immediate = false) {
        CallLogEvent(userID, sessionID, eventId, actor, verb, new JObject { { label, value } }, false, immediate);
    }

    private void CallLogEvent(string userID, string sessionID, int eventId, string actor, string verb, string label, string value, bool immediate = false) {
        CallLogEvent(userID, sessionID, eventId, actor, verb, new JObject { { label, value } }, false, immediate);
    }

    private void CallLogEvent(int eventId, string actor, string verb, string label, int value, bool immediate = false) {
        CallLogEvent(this.currUserID, this.currSessionID, eventId, actor, verb, new JObject { { label, value } }, false, immediate);
    }

    private void CallLogEvent(int eventId, string actor, string verb, string label, string value, bool immediate = false) {
        CallLogEvent(this.currUserID, this.currSessionID, eventId, actor, verb, new JObject { { label, value } }, false, immediate);
    }

    private void CallLogEvent(int eventId, string actor, string verb, JObject obj, bool includeContext = false, bool immediate = false) {
        CallLogEvent(this.currUserID, this.currSessionID, eventId, actor, verb, obj, includeContext, immediate);
    }

    private void CallLogEvent(string userID, string sessionID, int eventID, string actor, string verb, JObject obj, bool includeContext = false, bool immediate = false) {
        if (!HttpSendLogs && !DebugTraceLogs) { return; }
        JObject job = new JObject {
           {"api_key", serverKey},
           {"event_id", eventID},
           {"time", System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff")},
           {"user_id", userID},
           {"session_id", sessionID},
           {"transaction_id", System.Guid.NewGuid().ToString()},
           {"actor", actor},
           {"verb", verb},
           {"object", obj}
        };
        if (includeContext) {
            job["context"] = GenerateContext();
        }

        if (DebugTraceLogs) { 
            Debug.LogFormat("Log <color=cyan>{0}</color> <color=yellow>{1}</color> Full JSON: {2}", eventID, verb, job.ToString(Formatting.None));
        }
        if (HttpSendLogs) {
            if (immediate) {
                SendPostRequestImmediate(flaskURL + "/log_event", JsonConvert.SerializeObject(job), true);
            }
            else {
                StartCoroutine(SendPostRequestFireAndForget(flaskURL + "/log_event", JsonConvert.SerializeObject(job), true));
            }
        }
    }

    private JObject GenerateContext() {
        //this is a placeholder for now
        //include the run_id in here eventually
        if(IntegratedGameManager.S == null || IntegratedMapGenerator.Instance == null) {
            return null;
        }
        JObject ret = new JObject {
            {"level", currLevel },
            {"round", currRound },
            {"phase", currPhase },
            {"timer", IntegratedGameManager.S.TimeRemaining },
            {"shrineCount", IntegratedGameManager.S.goalCount }
        };

        string maplayout = "";
        string dwarf_map = "";
        string giant_map = "";
        string human_map = "";

        JArray challenges = new JArray();
        JArray shrines = new JArray();
        JArray characters = new JArray ( IntegratedGameManager.S.inSceneCharacters.Select(c => c.HMTStateRep(Character.StateRepLevel.Full)).ToArray() );

        for(int x = 0; x < IntegratedMapGenerator.Instance.Map.GetLength(0); x++) {
            for(int y = IntegratedMapGenerator.Instance.Map.GetLength(1)-1; y >=0 ; y--) {
                Tile tile = IntegratedMapGenerator.Instance.GetTileAt(x, y);
                maplayout += tile.ObjKey;
                dwarf_map += tile.fogOfWarDictionary[0].ToString()[0];
                giant_map += tile.fogOfWarDictionary[1].ToString()[0];
                human_map += tile.fogOfWarDictionary[2].ToString()[0];

                if(tile.tileType == Tile.ObstacleType.Trap || tile.tileType == Tile.ObstacleType.Rock) {
                    challenges.Add(tile.HMTStateRep());
                }
                if(tile.shrine != null) {
                    shrines.Add(tile.shrine.HMTStateRep());
                }
                foreach(Monster monster in tile.MonsterList) {
                    challenges.Add(monster.LogStateRep());
                }

            }
            maplayout += "\n";
            dwarf_map += "\n";
            giant_map += "\n";
            human_map += "\n";
        }

        ret["map"] = new JObject {
            {"layout", maplayout },
            {"dwarfView", dwarf_map },
            {"giantView", giant_map },
            {"humanView", human_map }
        };
        ret["characters"] = characters;
        ret["challenges"] = challenges;
        ret["shrines"] = shrines;

        return ret;
    }

    #region 1000s Logging Messages, Progression Events

    private void LogStartSession() {
        if (currSessionID != null) {
            Debug.LogWarning("LogSessionStart called when session already open. Closing and Re-opening");
            LogEndSession();
        }
        this.currSessionID = System.Guid.NewGuid().ToString();
        CallLogEvent(1000, "system", "start_session",
            new JObject {
                {"session_id", currSessionID },
                {"version", GlobalConstant.GAME_VERSION},
                {"platform", Application.platform.ToString()},
#if HMT_BUILD
                {"HMT_BUILD",true },
#else
                {"HMT_BUILD",false },   
#endif
            });
    }

    private void LogEndSession() {
        CallLogEvent(1001, "system", "end_session", "session_id", currSessionID, true);
        currSessionID = null;
    }


    public void LogStartGameLocal(string gameID) {
        if (currGameId != null) {
            LogEndGame();
        }
        currGameId = gameID;
        CallLogEvent(1002, "system", "start_game",
            new JObject {
                {"game_id", gameID },
                { "mode", "local"},
            });
    }

    public void LogStartGameNetwork(string gameID,
                            string dwarfUserID, string dwarfSessionID, bool dwarfIsAI,
                            string giantUserID, string giantSessionID, bool giantIsAI,
                            string humanUserID, string humanSessionID, bool humanIsAI) {
        if (currGameId != null) {
            LogEndGame();
        }
        currGameId = gameID;
        CallLogEvent(1010, "system", "start_game",
            new JObject {
                {"game_id", gameID },
                {"mode", "network"},
                {"dwarf", new JObject { { "user_id", dwarfUserID}, { "session_id", dwarfSessionID}, {"ai", dwarfIsAI } } },
                {"giant", new JObject { { "user_id", giantUserID}, { "session_id", giantSessionID}, {"ai", giantIsAI } } },
                {"human", new JObject { { "user_id", humanUserID}, { "session_id", humanSessionID}, {"ai", humanIsAI} } }
            });
    }

    public void LogEndGame() {
        CallLogEvent(1011, "system", "end_game", "game_id", currGameId);
        currGameId = null;
    }

    public void LogStartLevel(string levelName) {
        if (!LogSystemEvents) return;
        if (currLevel != null) {
            LogEndLevel();
        }
        currLevel = levelName;
        CallLogEvent(1020, "system", "start_level", "level_name", currLevel);
    }

    public void LogEndLevel() {
        if (!LogSystemEvents) return;
        CallLogEvent(1021, "system", "end_level", "level_name", currLevel);
        currLevel = null;
    }

    public void LogStartRound(int round) {
        if (!LogSystemEvents) return;
        if (this.currRound > 0) {
            LogEndRound();
        }
        this.currRound = round;
        CallLogEvent(1030, "system", "start_round", "currRound", this.currRound);
    }

    public void LogEndRound() {
        if (!LogSystemEvents) return;
        CallLogEvent(1031, "system", "end_round", "currRound", currRound);
        currRound = -1;
    }

    public void LogStartPhase(string phaseName) {
        if (!LogSystemEvents) return;
        if (currPhase != null) {
            LogEndPhase();
        }
        this.currPhase = phaseName;
        CallLogEvent(1040, "system", "start_phase", "currPhase", currPhase);
    }

    public void LogEndPhase() {
        if (!LogSystemEvents) return;
        CallLogEvent(1041, "system", "end_phase", "currPhase", currPhase);
        currPhase = null;
    }
    #endregion

    #region 2000s Logging Messages, Out of Game Interactions

    public void LogCreateRoom(string roomName) {
        CallLogEvent(2000, "player", "create_lobby", "roomCode", roomName);
    }

    public void LogJoinRoom(string roomName) {
        CallLogEvent(2001, "player", "join_lobby", "roomCode", roomName);
    }

    public void LogOpenHelp() {
        CallLogEvent(2010, "player", "open_help", "menu", "help");
    }

    #endregion

    #region 3000s Logging Messages, Player Actions

    public void LogSubmit(int characterId) {
        // Apply tihs structure to all other similar functions
        // make a system to convert from characterId to character name based on IntegratedGameManager
        if (RegisteredAgents.ContainsKey(characterId)) {
            CallLogEvent(RegisteredAgents[characterId].agentID, RegisteredAgents[characterId].sessionID,
                3000, IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName, "submit", null, true);
        }
        else {
            CallLogEvent(3000, IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName, "submit", null, true);
        }
    }

    public void LogTimeOut(int characterId) {
        if (RegisteredAgents.ContainsKey(characterId)) {
            CallLogEvent(RegisteredAgents[characterId].agentID, RegisteredAgents[characterId].sessionID,
                3001, IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName, "action_timeout", null, true);
        }
        else {
            CallLogEvent(3001, IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName, "action_timeout", null, true);
        }
    }


    public void LogPlacePin(int characterId, int pinTypeIdx, int x, int y) {
        if (RegisteredAgents.ContainsKey(characterId)) {
            CallLogEvent(RegisteredAgents[characterId].agentID, RegisteredAgents[characterId].sessionID,
                3100, IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName, "place_pin",
                new JObject { { "x", x }, { "y", y }, { "type", PinningSystem.PinIndxToPinType(pinTypeIdx) } },
                true);
        }
        else {
            CallLogEvent(3100,
                IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName,
                "place_pin",
                new JObject { { "x", x }, { "y", y }, { "type", PinningSystem.PinIndxToPinType(pinTypeIdx) } },
                true);
        }
    }

    public void LogAddPlan(int characterId, Character.Direction direct) {
        IList<Character.Direction> resultPlan = IntegratedGameManager.S.inSceneCharacters[characterId].ActionPlan;
        if (RegisteredAgents.ContainsKey(characterId)) {
            CallLogEvent(RegisteredAgents[characterId].agentID, RegisteredAgents[characterId].sessionID,
                3101, IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName, "edit_plan",
                new JObject { { "edit", direct.ToString()}, { "result_plan", new JArray(resultPlan) } }, true);
        }
        else {
            CallLogEvent(3101, IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName, "edit_plan",
                new JObject { { "edit", direct.ToString() },{ "result_plan", new JArray(resultPlan) } },
                true);
        }
    }

    public void LogUndoPlan(int characterId) {
        IList<Character.Direction> resultPlan = IntegratedGameManager.S.inSceneCharacters[characterId].ActionPlan;
        if (RegisteredAgents.ContainsKey(characterId)) {
            CallLogEvent(RegisteredAgents[characterId].agentID, RegisteredAgents[characterId].sessionID,
                3101, IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName, "edit_plan",
                new JObject { { "edit", "undo" }, { "result_plan", new JArray(resultPlan) } }, true);
        }
        else {
            CallLogEvent(3101, IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName, "edit_plan",
                new JObject { { "edit", "undo" }, { "result_plan", new JArray(resultPlan) } }, true);
        }
    }

    /// <summary>
    /// Logs when a player hovers over a challenge to get the tooltip information. We may want to track more information here.
    /// </summary>
    /// <param name="character"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void LogInspectChallenge(int characterId, int x, int y, string challengeType, 
                                    Combat.Dice selfDie, Combat.Dice partner1, Combat.Dice partner2, Combat.Dice challenge) {


        CallLogEvent(3102, IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName, "inspect_challenge",
            new JObject {
                { "x", x }, { "y", y },
                { "challenge_type", challengeType },
                { "self_die", selfDie.ToString() },
                { "partner1_die", partner1.ToString() },
                { "partner2_die", partner2.ToString() },
                { "challenge_die", challenge.ToString() },
            },
            true);
    }

    #endregion

    #region 4000s Logging Messages, Game System Events

    public void LogPlayerSpawn(int characterId, int x, int y) {
        if (!LogSystemEvents) return;
        string character = IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName;
        CallLogEvent(4001, character, "player_spawn",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    public void LogPlayerDeath(int characterId, int x, int y) {
        if (!LogSystemEvents) return;
        string character = IntegratedGameManager.S.inSceneCharacters[characterId].config.characterName;
        CallLogEvent(4002, character, "player_death",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    public void LogChallengeSpawn(string challenge_name, int x, int y) {
        if (!LogSystemEvents) return;
        CallLogEvent(4003, challenge_name, "challenge_spawn",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    public void LogChallengeDeath(string challenge_name, int x, int y) {
        if (!LogSystemEvents) return;
        CallLogEvent(4004, challenge_name, "challenge_death",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    /// <summary>
    /// Used to log steps during either the player or monster move phases. 
    /// Plan information and directions should be inferable from state.
    /// </summary>
    /// <param name="dwarfMove"></param>
    /// <param name="giantMove"></param>
    /// <param name="humanMove"></param>
    public void LogPlayerMoveStep() {
        if (!LogSystemEvents) return;
        CallLogEvent(4100, "system", "move_step", 
            new JObject {
                {"dwarf", 
                    new JObject {
                        { "done", IntegratedGameManager.S.inSceneCharacters[0].ActionPlan.Count == 0 },
                        { "move", IntegratedGameManager.S.inSceneCharacters[0].ActionPlan.Count > 0 ? IntegratedGameManager.S.inSceneCharacters[0].ActionPlan[0].ToString() : "none" },
                    }
                },
                {"giant", 
                    new JObject {
                        { "done", IntegratedGameManager.S.inSceneCharacters[1].ActionPlan.Count == 0 },
                        { "move", IntegratedGameManager.S.inSceneCharacters[1].ActionPlan.Count > 0 ? IntegratedGameManager.S.inSceneCharacters[1].ActionPlan[0].ToString() : "none" },
                    }
                },
                {"human", 
                    new JObject {
                        { "done", IntegratedGameManager.S.inSceneCharacters[2].ActionPlan.Count == 0 },
                        { "move", IntegratedGameManager.S.inSceneCharacters[2].ActionPlan.Count > 0 ? IntegratedGameManager.S.inSceneCharacters[2].ActionPlan[0].ToString() : "none" },
                    }
                }
            }, true);
    }

    public void LogMonsterMoveStep(IList<Monster> monsters) {
        if (!LogSystemEvents) return;
        JObject job = new JObject();
        foreach(Monster monster in IntegratedGameManager.S.inSceneMonsters) {
            job[monster.ObjKey] = new JObject {
                            { "done", monster.MovesLeftThisTurn == 0 },
                            { "move", monster.NextMove().ToString() }
                        };
        }
    }



    public void LogChallengeEncounter(int x, int y,
        IList<string> characterNames, IList<string> challengeNames,
        IList<int> characterRolls, IList<int> challengeRoles,
        float odds, bool outcome) {
        if (!LogSystemEvents) return;
        CallLogEvent(4101, "system", "challenge_encounter",
            new JObject {
                { "x", x },
                { "y", y },
                { "characters", new JArray(characterNames) },
                { "challenges", new JArray(challengeNames) },
                { "character_rolls", new JArray(characterRolls) },
                { "challenge_rolls", new JArray(challengeRoles) },
                { "odds", odds },
                { "outcome", outcome }
            },
            true);
    }

    public void LogClearShrine(int characterID, int x, int y) {
        if (!LogSystemEvents) return;
        CallLogEvent(4102, IntegratedGameManager.S.inSceneCharacters[characterID].config.characterName, "clear_shrine",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    public void LogRevokeShrine(int characterID, int x, int y) {
        if (!LogSystemEvents) return;
        CallLogEvent(4103, IntegratedGameManager.S.inSceneCharacters[characterID].config.characterName, "revoke_shrine",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    public void LogClearGoal(int characterID, int x, int y) {
        if (!LogSystemEvents) return;
        CallLogEvent(4104, IntegratedGameManager.S.inSceneCharacters[characterID].config.characterName, "clear_goal",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    #endregion

    #region 5000s Logging Messages, AI Agent Events

    public void LogHMTConnect(int characterId) {
        if (!RegisteredAgents.ContainsKey(characterId)) {
            Debug.LogErrorFormat("Could not find agent record for characterId {0}", characterId);
            return;
        }
        AgentRecord record = RegisteredAgents[characterId];
        CallLogEvent(record.agentID, record.sessionID, 5000, record.agentID, "hmt_connect",
            new JObject { { "service_target", record.target } },
            true);
    }


    //TODO need to figure out what to do with this for the Register Command, which predates the characterId system
    public void LogHMTInterfaceCall(int characterId, HMT.Command command) {
        if (!RegisteredAgents.ContainsKey(characterId)) {
            Debug.LogErrorFormat("Could not find agent record for service target {0}", characterId);
            return;
        }
        AgentRecord record = RegisteredAgents[characterId];
        CallLogEvent(record.agentID, record.sessionID, 5001, record.agentID, "hmt_interface_call",
            new JObject { { "service_target", record.target }, { "command_json", command.json } },
            true);
    }

    #endregion
}
