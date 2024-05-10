using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CompetitionMiddleware : MonoBehaviour
{

    public static CompetitionMiddleware Instance = null;

    public string flaskURL = "https://localhost";
    public string serverKey = "NOTSET";
    public bool overrideAIMode = true;
    public bool enableLogging = true;

    public string UserID { get { return currUserID; } }

    public string SessionID { get { return currSessionID; } }

    public struct AgentRecord {
        public string agentID;
        public string sessionID;

        public AgentRecord(string agentID, string sessionID) {
            this.agentID = agentID;
            this.sessionID = sessionID;
        }
    }

    public Dictionary<string, AgentRecord> RegisteredAgents = new Dictionary<string, AgentRecord>();
    
    public bool IsAI { get {
        #if HMT_BUILD
            return !overrideAIMode;
        #else
            return false;
        #endif
    } }

    private string currUserID = null;
    private string currSessionID = null;
    private string runID = null;
    private string level = null;
    private string round = null;
    private string phase = null;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else {
            Destroy(this.gameObject);
        }
    }

    // Update is called once per frame
    void Update() {

    }

    private void OnDestroy() {
        if(RegisteredAgents.Count > 0) {
            foreach(KeyValuePair<string, AgentRecord> entry in RegisteredAgents) {
                CallLogEvent(entry.Value.agentID, entry.Value.sessionID, 1001, "system", "end_session", "session_id", entry.Value.sessionID);
            }
        }
        else {
            LogEndSession();
        }
    }

    void SendPostRequestImmediate(string url, string json) {
        using(UnityWebRequest www = UnityWebRequest.Post(url, json)) {
            www.SetRequestHeader("Content-type", "application/json");
            Debug.LogFormat("Sending {0} request to {1} with data {2}, with", www.method, www.url, json);
            www.SendWebRequest();
        }
    }


    IEnumerator SendPostRequestFireAndForget(string url, string json) {
        using (UnityWebRequest www = UnityWebRequest.Post(url, json)) {
            Debug.LogFormat("Sending {0} request to {1} with data {2}, with", www.method, www.url, json);
            www.SetRequestHeader("Content-type", "application/json");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError(www.error);
            }
        }
    }

    IEnumerator SendPostRequestWithCallback(string url, string json, System.Action<JObject> callback) {
        using (UnityWebRequest www = UnityWebRequest.Post(url, json)) {
            Debug.LogFormat("Sending {0} request to {1} with data {2}, with", www.method, www.url, json);
            www.SetRequestHeader("Content-type", "application/json");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError(www.error);
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

    public void AddAIAgent(string target, string agentID) {
        AgentRecord record = new AgentRecord(agentID, System.Guid.NewGuid().ToString());
        RegisteredAgents[target] = record;
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
        //this can be fire and forget
    }

    private void CallLogEvent(string userID, string sessionID, int eventId, string actor, string verb, string label, string value, bool immediate=false) {
        CallLogEvent(userID, sessionID, eventId, actor, verb, new JObject { { label, value } }, false, immediate);
    }

    private void CallLogEvent(int eventId, string actor, string verb, string label, string value, bool immediate=false) {
        CallLogEvent(this.currUserID, this.currSessionID, eventId, actor, verb, new JObject { { label, value} }, false, immediate);
    }

    private void CallLogEvent(int eventId, string actor, string verb, JObject obj, bool includeContext, bool immediate=false) {
        CallLogEvent(this.currUserID, this.currSessionID, eventId, actor, verb, obj, includeContext, immediate);
    }

    private void CallLogEvent(string userID, string sessionID, int eventID, string actor, string verb, JObject obj, bool includeContext, bool immediate=false) {
        if(!enableLogging) { return; }
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
        if(includeContext) {
            job["context"] = GenerateContext();
        }
        if(immediate) {
            SendPostRequestImmediate(flaskURL + "/log_event", JsonConvert.SerializeObject(job));
        }
        else {
            StartCoroutine(SendPostRequestFireAndForget(flaskURL + "/log_event", JsonConvert.SerializeObject(job)));
        }
    }


    private JObject GenerateContext() {
        //this is a placeholder for now
        //include the run_id in here eventually
        return null;
    }

    #region 1000s Logging Messages, Progression Events

    private void LogStartSession() {
        if (currSessionID != null) {
            Debug.LogWarning("LogSessionStart called when session already open. Closing and Re-opening");
            LogEndSession();
        }
        this.currSessionID = System.Guid.NewGuid().ToString();
        CallLogEvent(1000, "system", "start_session", "session_id", currSessionID);
    }

    private void LogEndSession() {
        CallLogEvent(1001, "system", "end_session", "session_id", currSessionID, true);
        currSessionID = null;
    }


    public void LogStartRun(string runId) {
        if (runID != null) {
            LogEndRun();
        }
        this.runID = runId;
        CallLogEvent(1010, "system", "start_run", "run_id", runID);
    }

    public void LogEndRun() {
        CallLogEvent(1011, "system", "end_run", "run_id", runID);
        runID = null;
    }

    public void LogStartLevel(string levelName) {
        if (levelName != null) {
            LogEndLevel();
        }
        this.level = levelName;
        CallLogEvent(1020, "system", "start_level", "level_name", level);
    }

    public void LogEndLevel() {
        CallLogEvent(1021, "system", "end_level", "level_name", level);
        level = null;
    }

    public void LogStartRound(int round) {
        if (this.round != null) {
            LogEndRound();
        }
        this.round = round.ToString();
        CallLogEvent(1030, "system", "start_round", "round", this.round);
    }

    public void LogEndRound() {
        CallLogEvent(1031, "system", "end_round", "round", round);
        round = null;
    }

    public void LogStartPhase(string phaseName) {
        if(phase != null) {
            LogEndPhase();
        }
        this.phase = phaseName;
        CallLogEvent(1040, "system", "start_phase","phase", phase);
    }

    public void LogEndPhase() {
        CallLogEvent(1041, "system", "end_phase", "phase", phase);
        phase = null;
    }
    #endregion

    #region 2000s Logging Messages, Out of Game Interactions

    public void LogCreateLobby(string roomName) {
        CallLogEvent(2000, "player", "create_lobby", "roomCode", roomName);
    }

    public void LogJoinLobby(string roomName) {
        CallLogEvent(2001, "player", "join_lobby","roomCode",roomName);
    }

    public void LogOpenHelp() {
        CallLogEvent(2010, "player", "open_help", "menu", "help");
    }

    #endregion

    #region 3000s Logging Messages, Player Actions

    public void LogSubmit(string character) {
        if (RegisteredAgents.ContainsKey(character)) {
            CallLogEvent(RegisteredAgents[character].agentID, RegisteredAgents[character].sessionID, 
                3000, character, "submit", null, true);
        }
        else {
            CallLogEvent(3000, character, "submit", null, true);
        }
    }

    public void LogPlacePin(string character, string pinType, int x, int y) {
        if (RegisteredAgents.ContainsKey(character)) {
            CallLogEvent(RegisteredAgents[character].agentID, RegisteredAgents[character].sessionID,
                3100, character, "place_pin",
                new JObject { { "x", x }, { "y", y }, { "type", pinType } },
                true);
        }
        else {
            CallLogEvent(3100,
                character,
                "place_pin",
                new JObject { { "x", x }, { "y", y }, { "type", pinType } },
                true);
        }
    }

    public void LogAddPlan(string character, Character.Direction direct, IList<Character.Direction> resultPlan) {
        if (RegisteredAgents.ContainsKey(character)) {
            CallLogEvent(RegisteredAgents[character].agentID, RegisteredAgents[character].sessionID, 
                3101, character, "edit_plan",
                new JObject { "edit", direct.ToString(), "result_plan", new JArray(resultPlan) }, true);
        }
        else {
            CallLogEvent(3101, character, "edit_plan",
                new JObject { "edit", direct.ToString(), "result_plan", new JArray(resultPlan) },
                true);
        }
    }

    public void LogUndoPlan(string character, IList<Character.Direction> resultPlan) {
        if (RegisteredAgents.ContainsKey(character)) {
            CallLogEvent(RegisteredAgents[character].agentID, RegisteredAgents[character].sessionID,
                3101, character, "edit_plan",
                new JObject { "edit", "undo", "result_plan", new JArray(resultPlan) }, true);
        }
        else {
            CallLogEvent(3101, character, "edit_plan",
                new JObject { { "edit", "undo" }, { "result_plan", new JArray(resultPlan) } }, true);
        }
    }

    /// <summary>
    /// Logs when a player hovers over a challenge to get the tooltip information. We may want to track more information here.
    /// </summary>
    /// <param name="character"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void LogInspectPlan(string character, int x, int y) {
        CallLogEvent(3102, character, "inspect_challenge",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    #endregion

    #region 4000s Logging Messages, Game System Events

    public void LogPlayerSpawn(string character, int x, int y) {
        CallLogEvent(4001, character, "player_spawn",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    public void LogPlayerDeath(string character, int x, int y) {
        CallLogEvent(4002, character, "player_death",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    public void LogChallengeSpawn(string challenge_name, int x, int y) {
        CallLogEvent(4003, challenge_name, "challenge_spawn",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    public void LogChallengeDeath(string challenge_name, int x, int y) {
        CallLogEvent(4004, challenge_name, "challenge_death",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    /// <summary>
    /// Used to log steps during either the player or monster move phases. Plan information and directions should be inferable from state.
    /// </summary>
    /// <param name="dwarfMove"></param>
    /// <param name="giantMove"></param>
    /// <param name="humanMove"></param>
    public void LogMoveStep() {
        CallLogEvent(4100, "system", "move_step", null, true);
    }


    public void LogChallengeEncounter(int x, int y,
        IList<string> characterNames, IList<string> challengeNames,
        IList<int> characterRolls, IList<int> challengeRoles,
        float odds, bool outcome) {
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

    public void LogClearShrine(string character, int x, int y) {
        CallLogEvent(4102, character, "clear_shrine",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    public void LogClearGoal(string character, int x, int y) {
        CallLogEvent(4103, character, "clear_goal",
            new JObject { { "x", x }, { "y", y } },
            true);
    }

    #endregion

    #region 5000s Logging Messages, AI Agent Events

    public void LogHMTConnect(string service_target) {
        if(!RegisteredAgents.ContainsKey(service_target)) {
            Debug.LogErrorFormat("Could not find agent record for service target {0}", service_target);
            return;
        }
        AgentRecord record = RegisteredAgents[service_target];
        CallLogEvent(record.agentID, record.sessionID, 5000, record.agentID, "hmt_connect",
            new JObject { { "service_target", service_target } },
            true);
    }

    public void LogHMTInterfaceCall(string service_target, JObject actionBlob) {
        if (!RegisteredAgents.ContainsKey(service_target)) {
            Debug.LogErrorFormat("Could not find agent record for service target {0}", service_target);
            return;
        }
        AgentRecord record = RegisteredAgents[service_target];
        CallLogEvent(record.agentID, record.sessionID, 5001, record.agentID, "hmt_interface_call",
            new JObject { { "service_target", service_target }, { "action_blob", actionBlob } },
            true);
    }

    #endregion
}
