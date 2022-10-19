using Newtonsoft.Json.Linq;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace HMT {

    /// <summary>
    /// This class will serve as the abstract template for HMT Interfaces going forward.
    /// For now it is just going to be where I experiment with things.
    /// </summary>
    public abstract class HMTInterface : MonoBehaviour {

        public static HMTInterface Instance { get; private set; }

        [Header("AI Socket Settings")]
        public bool StartServerOnStart = false;
        public string url = "ws://localhost";
        public int socketPort = 4649;
        public string serviceName = "hmt";

        [Header("Hot Keys")]
        public KeyCode[] OpenHMTInterfaceWindowHotKey;
        public KeyCode[] PrintCurrentStateHotKey;
        
        
        private bool isOpen=false;
        private Vector2 scrollPos = Vector2.zero;
        private WebSocketServer server = null;
          

        // Start is called before the first frame update
        virtual protected void Start() {
            if (Instance != null) {
                Destroy(this);
                return;
            }
            else {
                Instance = this;
            }

            DontDestroyOnLoad(this);
            if (StartServerOnStart) {
                StartSocketServer();
            }
        }

        public void StartSocketServer() {
            if (socketPort == 80) {
                Debug.LogWarning("Socket set to Port 80, which will probably have permissions issues.");
            }
            server = new WebSocketServer(url + ":" + socketPort);

            server.AddWebSocketService<HMTService>("/" + serviceName);
            server.Start(); 
            Debug.LogFormat("[HMTInterface] Websocket for HMTService at url {0}:{1}/{2}",
                url, socketPort, serviceName);
        }

        protected bool CheckHotKey(KeyCode[] code ) {
            foreach(KeyCode key in code) {
                if (!Input.GetKey(key)) {
                    return false;
                }
            }
            return code.Length > 0;
        }

        virtual protected void OnDestroy() {
            if (server != null) {
                server.Stop();
            }
        }

        // Update is called once per frame
        virtual protected void Update() {
            if (CheckHotKey(OpenHMTInterfaceWindowHotKey)) {
                isOpen = !isOpen;
            }
            if (CheckHotKey(PrintCurrentStateHotKey)) {
                Debug.LogFormat("[HMTInterface] State Hotkey: {0}", GetState(false));
            }
        }

        private string lastState = string.Empty;

        private void OnGUI() {
            if (isOpen) {
                GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
                scrollPos = GUILayout.BeginScrollView(scrollPos);
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Close")) {
                    isOpen = !isOpen;
                }

                if(server == null) {
                    if(GUILayout.Button("Start Socket Server")) {
                        StartSocketServer();
                    }
                }
                else {
                    if (server.IsListening) {
                        if (GUILayout.Button("Stop Socket Server")) {
                            server.Stop();
                        }
                    }
                    else {
                        if(GUILayout.Button("Start Socket Server")) {
                            server.Start();
                        }
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.Label("Socket Server Status");
                if (server == null) {
                    GUILayout.Label("Server NOT connected.");
                }
                else {
                    GUILayout.Label(string.Format("Address: {0}", server.Address.ToString()));
                    GUILayout.Label(string.Format("Listening: {0}", server.IsListening));
                    GUILayout.Label(string.Format("Secure: {0}", server.IsSecure));
                    GUILayout.Label(string.Format("WaitTime: {0}", server.WaitTime));
                }

                GUILayout.Space(50);
                
                GUILayout.Label("STATE:");
                if(GUILayout.Button("Snap State")) {
                    lastState = GetState(true);
                }
                GUILayout.Label(lastState);
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        /// <summary>
        /// Captures a representation of the current game state to send to an agent.
        /// 
        /// The method is abstract because it will require a game-specific implementation.
        /// 
        /// I'm envisioning this will eventually be some kind of JSON representation but I'm leaving the return as a
        /// string for now.
        /// </summary>
        /// <param name="formated">Whether to "pretty print" format the JSON or not.</param>
        /// <returns></returns>
        public abstract string GetState(bool formated=false);

        /// <summary>
        /// Exectutes a player action on behalf of the agent.
        /// 
        /// This is mostly a stub for now since actions will probalby require more than a single string for context.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public abstract string ExecuteAction(string action); 

        /// <summary>
        /// Process a command comming off of the Websocket Protocol.
        /// 
        /// By default this functional will handle "get_state" and "execute_action" commands by calling the associated 
        /// abstraction functions. If any other command is sent then it will Debug Log an error and return a message 
        /// to the caller.
        /// 
        /// If additional commands are desired the function can be override and the default behavior for an 
        /// unrecognized command can be prevented.
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="json"></param>
        /// <param name="preventDefault"></param>
        /// <returns></returns>
        public virtual string ProcessCommand(string command, JObject json, bool preventDefault=false) {
            string response = string.Empty;
            switch (command) {
                case "get_state":
                    response = GetState();
                    break;
                case "execute_action":
                    //TODO this is just a stub API for now. Actions' will likely be much more complex.
                    string action = json["action"].ToString();
                    response = ExecuteAction(action);
                    //do the action
                    //respond with result?
                    break;
                default:
                    if (!preventDefault) {
                        Debug.LogErrorFormat("[{0}] Unrecognized Command: {1}", "HMTInterface", command);
                        response = string.Format("Unrecognized Command: {0}", command);
                    }
                    break;
            }
            return response;
        }
    }

    /// <summary>
    /// This class is just for facilitating the socket interface. 
    /// 
    /// My goal would be for no logic to actually live here and instead by 
    /// handled by the ProcessCommand virtual method in the main HMTInterface class.
    /// </summary>
    public class HMTService : WebSocketBehavior {
        protected override void OnMessage(MessageEventArgs e) {
            string response = string.Empty;

            JObject json = JObject.Parse(e.Data);
            string command = json["command"].ToString();

            Debug.LogFormat("[HMTInterface] recieved command: {0}", command);
            response = HMTInterface.Instance.ProcessCommand(command, json);
            Send(response);
        }

        protected override void OnOpen() {
            Debug.Log("[HMTInterface] Client Connected.");
        }

        protected override void OnClose(CloseEventArgs e) {
            Debug.Log("[HMTInterface] Cliend Disconnected.");
        }

        protected override void OnError(ErrorEventArgs e) {
            Debug.LogErrorFormat("[HMTInterface] Error: {0}", e.Message);
            Debug.LogException(e.Exception);
        }
    }

   
}