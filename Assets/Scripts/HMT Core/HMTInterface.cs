using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Collections;
#if HMT_BUILD
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Collections.Concurrent;
#endif

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
        public string[] serviceTargets = new string[0];

#if HMT_BUILD
        internal WebSocketServer server = null;

        public ConcurrentQueue<Command> CommandQueue = new ConcurrentQueue<Command>();

        public ArgParser Args = new ArgParser();

        // Start is called before the first frame update
        virtual protected void Start() {
            if (Instance != null) {
                Destroy(this);
                return;
            }
            else {
                Instance = this;
            }
            

            Args.AddArg("hmtsocketurl", ArgParser.ArgType.One);
            Args.AddArg("hmtsocketport", ArgParser.ArgType.One);

            Args.ParseArgs();

            DontDestroyOnLoad(this);
            if (StartServerOnStart) {
                StartSocketServer();
            }
        }

        public void StartSocketServer() {
            socketPort = Args.GetArgValue("hmtsocketport", socketPort);
            url = Args.GetArgValue("hmtsocketurl", url);

            if (socketPort == 80) {
                Debug.LogWarning("Socket set to Port 80, which will probably have permissions issues.");
            }

            if(url == string.Empty) {
                Debug.LogWarning("url empty so opening socket equivalent local context.");
                server = new WebSocketServer(socketPort);
            }
            else {
                server = new WebSocketServer(url + ":" + socketPort);
            }
            
            foreach(string target in serviceTargets) {
                server.AddWebSocketService<HMTService>("/" + serviceName + "/" + target);
            }


            server.AddWebSocketService<HMTService>("/" + serviceName);
            server.Start();

            foreach (string target in serviceTargets) {
                Debug.LogFormat("[HMTInterface] Agent Target available at: {0}:{1}/{2}/{3}",
                    url == string.Empty ? "ws://localhost" : url, socketPort, serviceName, target);
            }
        }

        virtual protected void OnDestroy() {
            if (server != null) {
                server.Stop();
            }
        }

        // Update is called once per frame
        virtual protected void Update() {
            

            while(CommandQueue.Count > 0)  {
                if (CommandQueue.TryDequeue(out Command command)) {
                   StartCoroutine(ProcessCommand(command));
                }
            }
        }

#else
        virtual protected void Start() {
            Debug.LogWarning("HMT_BUILD flag not set. Destroying HMTInterface");
            Destroy(this.gameObject);
        }

        virtual protected void Update() { }

        public void StartSocketServer() {}


#endif

        /// <summary>
        /// Used as an intiial handshake with the agent.
        /// 
        /// Mainly used to send the agent's competition id/name so that other systems can know it.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public abstract IEnumerator Register(Command command);
        
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
        public abstract string GetState(Command command);

        /// <summary>
        /// Exectutes a character action on behalf of the agent.
        /// 
        /// Currently it expects a JObject that should have come over the wire.
        /// In general we would assume the JObject would have selection, action, and inputs fields.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public abstract IEnumerator ExecuteAction(Command command);

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
        /// <param name="command">A struct containing relevant Command Parameters</param>
        /// <returns></returns>
        public virtual IEnumerator ProcessCommand(Command command) {
            switch (command.command) {
                case "register":
                    yield return Register(command);
                    break;
                case "get_state":
                    string response = GetState(command);
                    command.SendOKResponse("Local State", response);
                    yield break;
                case "execute_action":
                    //TODO this is just a stub API for now. Actions' will likely be much more complex.
                    yield return ExecuteAction(command);
                    break;
                case "JSON_PARSE_ERROR":
                    command.SendErrorResponse("JSON Parse Error", string.Format("{{\"errorCode\":0,\"errorMessage\":{0}   }}", command.json["errorMessage"]));
                    break;
                default:
                    if (!command.supressDefault) {
                        Debug.LogErrorFormat("[{0}] Unrecognized Command: {1}", "HMTInterface", command);
                        command.SendErrorResponse(string.Format("Unrecognized Command: {0}", command), 1);
                    }
                    break;
            }
            yield break;
        }
    }



#if HMT_BUILD

    /// <summary>
    /// This class is just for facilitating the socket interface. 
    /// 
    /// My goal would be for no logic to actually live here and instead by 
    /// handled by the ProcessCommand virtual method in the main HMTInterface class.
    /// </summary>
    public class HMTService : WebSocketBehavior {

        protected override void OnMessage(MessageEventArgs e) {
            Debug.LogFormat("[HMTInterface] recieved command: {0}", e.Data);

            JObject json;
            try { 
                json = JObject.Parse(e.Data);
            }
            catch (Newtonsoft.Json.JsonReaderException ex) {
                Debug.LogErrorFormat("[HMTInterface] Error parsing JSON: {0}", ex.Message);
                json = new JObject{
                    {"command","JSON_PARSE_ERROR" },
                    { "supressDefault", true},
                    {"errorMessage",ex.Message}
                };
            }
            Command newCommand = new Command();

            newCommand.target = this.Context.RequestUri.Segments[Context.RequestUri.Segments.Length - 1];
            newCommand.command = json["command"].ToString();
            newCommand.json = json;
            newCommand.supressDefault = json.ContainsKey("supressDefault") && json["supressDefault"].ToString().ToLower().Equals("true");
            newCommand.originService = this;


            HMTInterface.Instance.CommandQueue.Enqueue(newCommand);

            

            /*string command = json["command"].ToString();


            response = HMTInterface.Instance.ProcessCommand(command, json);
            Send(response);*/
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

    public struct Command {
        public string target;
        public string command;
        public bool supressDefault;
        public JObject json;
        public WebSocketBehavior originService;

        public void SendOKResponse(string message, string content = null) {
            string mess = string.Format("{{\"command\":\"{0}\", \"status\":\"OK\", \"message\":\"{1}\", \"content\":{2} }}", command, message, content);
            originService.Context.WebSocket.Send(mess);
            Debug.Log($"OK response sent with message: {message}");
        }

        public void SendErrorResponse(string message, string content = null) {
            string mess = string.Format("{{\"command\":\"{0}\", \"status\":\"ERROR\", \"message\":\"{1}\", \"content\":{2}}}", command, message, content);
            originService.Context.WebSocket.Send(mess);
        }

        public void SendErrorResponse(string message, int content) {
            string mess = string.Format("{{\"command\":\"{0}\", \"status\":\"ERROR\", \"message\":\"{1}\", \"content\":{{\"errorCode:\"{2} }} }}", command, message, content);
            originService.Context.WebSocket.Send(mess);
        }

        public void SendGameOverResponse(string message, string content = null) {
            string mess = string.Format("{{\"command\":\"{0}\", \"status\":\"GAME_OVER\", \"message\":\"{1}\", \"content\":{2} }}", command, message, content);
            originService.Context.WebSocket.Send(mess);
        }

        public void SendIllegalActionResponse(string message, string content = null) {
            string mess = string.Format("{{\"command\":\"{0}\", \"status\":\"ILLEGAL_ACTION\", \"message\":\"{1}\", \"content\":{2}}}", command, message, content);
            originService.Context.WebSocket.Send(mess);
        }

        public void SendIllegalActionResponse(string message, int content) {
            string mess = string.Format("{{\"command\":\"{0}\", \"status\":\"ILLEGAL_ACTION\", \"message\":\"{1}\", \"content\":{{\"errorCode:\"{2} }} }}", command, message, content);
            originService.Context.WebSocket.Send(mess);
        }
    }

#else 
    /// <summary>
    /// A placeholder version of the Command struct used for when the HMT Interface is turned off.
    /// </summary>
    public struct Command { 
        public string command;
        public string target;
        public JObject json;
        public bool supressDefault;
        public void SendOKResponse(string message, string content = null) {
            return;
        }

        public void SendErrorResponse(string message, string content = null) {
            return;
        }

        public void SendErrorResponse(string message, int content) {
            return;
        }

        public void SendGameOverResponse(string message, string content = null) {
            return;
        }

        public void SendIllegalActionResponse(string message, string content = null) {
            return;
        }

        public void SendIllegalActionResponse(string message, int content) {
            return;
        }
    }

#endif

}