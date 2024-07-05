using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameConstant;
using HMT;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Photon.Realtime;
using Random = UnityEngine.Random;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkLobbyManager : MonoBehaviour
{
    public OnBoardingState onBoardingState = OnBoardingState.ChooseGameMode;
    public OnBoardingState playChoice = OnBoardingState.ChooseGameMode;

    private int _numPerson;
    public List<RoomInfo> ListOfRooms;

    private float _twoHumanChance;
    private float _timeoutLimit;
    private bool _matchmakingConfigSet;
    private float _timer;
    
    // Singleton reference
    public static NetworkLobbyManager S;

    private void Awake()
    {
        if (S) Destroy(this);
        else S = this;

        _matchmakingConfigSet = false;
        _twoHumanChance = MatchMakingParameter.TWO_PERSON_GAME_CHANCE;
        _timeoutLimit = MatchMakingParameter.TIMEOUT_LIMIT;
    }


#if HMT_BUILD

    private ArgParser Args = new ArgParser();
    private IEnumerator Start() {
        Args.AddArg("photonroom", ArgParser.ArgType.One);
        Args.AddArg("localmode", ArgParser.ArgType.Flag);
        Args.ParseArgs();

        if (CompetitionMiddleware.Instance.overrideAIMode) {
            yield break;
        }


        Debug.LogFormat("Starting Autonomous Launch Sequence");
        Debug.LogFormat("photonroom: {0}, localmode: {1}", Args.GetArgValue("photonroom", ""), Args.GetArgValue("localmode",false));

        if(Args.GetArgValue("localmode", false)) {
            Debug.Log("Starting Local Mode");
            LocalTestSelected();
            yield break;
        }
        else {
            //act like we cliked Online
            Debug.Log("Starting Online Mode");
            OnlinePlaySelected();
            //spin until we're connected
            while(onBoardingState != OnBoardingState.CreateOrJoinRoom) {
                yield return null;
            }

            //attempt to join a room, will need to edit that function to get the name
            JoinRoomAttempt(Args.GetArgValue("photonroom",""));
            yield break;
            //if the connection failes then just bail, AI's don't create rooms
        }
    }
#else

    private void Start()
    {
        if (NetworkMiddleware.S != null)
        {
            Destroy(NetworkMiddleware.S.gameObject);
        }
    }
#endif


    public void LocalTestSelected() {
        string runID = System.Guid.NewGuid().ToString();
        CompetitionMiddleware.Instance.LogStartGameLocal(runID);
        SceneManager.LoadScene(GlobalConstant.LOCAL_SCENE);
    }
    
    // ============ Server Connection & Handle ============

    public void OnlinePlaySelected() {
        onBoardingState = OnBoardingState.Loading;
        LobbyNetwork.S.TryConnectToServer();
        LobbyUI.S.ShowLoadingUI("Connecting to Server...");
    }

    public void OnConnectToServer() {
        onBoardingState = OnBoardingState.ChooseJoinRoomMode;
        // LobbyUI.S.ShowJoinRoomModeUI();
        CreateOrJoinRoomSelected();
    }

    public void OnDisconnectSucceed()
    {
        onBoardingState = OnBoardingState.ChooseGameMode;
        LobbyUI.S.ShowGameModeUI();
    }

    public void OnUnexpectedDisconnect()
    {
        onBoardingState = OnBoardingState.Disconnected;
        Debug.LogWarning("Disconnected Unexpectedly");
        LobbyUI.S.ShowDisconnectedUI();
    }
    
    // ============ Join/Create Room Handle ============

    public void CreateOrJoinRoomSelected()
    {
        onBoardingState = OnBoardingState.Loading;
        LobbyNetwork.S.TryJoinLobby();
        LobbyUI.S.ShowLoadingUI();
    }

    public IEnumerator OnJoinLobbySucceed()
    {
        onBoardingState = OnBoardingState.CreateOrJoinRoom;
#if HMT_BUILD
        yield break;
#else

        LobbyUI.S.ShowLoadingUI("Initializing Matchmaking System");
        
        CompetitionMiddleware.Instance.CallMatchmakingConfig(OnMatchmakingConfigResponse);
        while (!_matchmakingConfigSet)
        {
            yield return null;
        }
        
        _numPerson = (Random.Range(0.0f, 1.0f) < _twoHumanChance) ? 2 : 1;
        CompetitionMiddleware.Instance.LogAssignCondition(_numPerson+"-human");
        StartCoroutine(JointMatchmakingRoom());
#endif
    }
    
    private void OnMatchmakingConfigResponse(JObject response)
    {
        if (false && response != null 
            && response.ContainsKey("two_human_prob") 
            && response.ContainsKey("timeout_limit"))
        {
            _twoHumanChance = response["two_human_prob"].ToObject<float>();
            _timeoutLimit = response["timeout_limit"].ToObject<float>();
            Debug.Log($"Matchmaking parameter fetched from server with " +
                      $"two_human_prob: {_twoHumanChance} and timeout limit: {_timeoutLimit}");
        }
        else
        {
            Debug.LogWarning("failed to retrieve matchmaking parameter from server, " +
                           "reverting to built in default one");
        }

        _matchmakingConfigSet = true;
    }

    private IEnumerator JointMatchmakingRoom()
    {
        Debug.Log($"Creating/Joining a room with {_numPerson} human players");
        CompetitionMiddleware.Instance.LogJoinQueue();
        LobbyUI.S.ShowLoadingUI("Searching for a Room to Join...");
        
        _timer = _timeoutLimit;
        Hashtable roomPropertyHashTable = new Hashtable { { MatchMakingParameter.NUM_PERSON_KEY, _numPerson } };
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 3,
            CustomRoomPropertiesForLobby = new string[]{MatchMakingParameter.NUM_PERSON_KEY},
            CustomRoomProperties = roomPropertyHashTable
        };

        if (_numPerson == 1)
        {
            yield return new WaitForSeconds(Random.Range(0.0f, Mathf.Max(0.0f, _timer)));
            LobbyNetwork.S.TryCreateRoom(
                Guid.NewGuid().ToString(), 
                roomOptions);
        }
        else
        {
            // Note: not sure there's a more efficient way than this linear search
            Debug.Log("Searching if a two human room is present");
            foreach (RoomInfo roomInfo in ListOfRooms)
            {
                object numPlayerProp = roomInfo.CustomProperties[MatchMakingParameter.NUM_PERSON_KEY];
                if (numPlayerProp is int prop && prop == _numPerson && roomInfo.PlayerCount == 1 && roomInfo.IsVisible)
                {
                    Debug.Log($"Two human room found with name {roomInfo.Name}, joining");
                    LobbyNetwork.S.TryJoinRoom(roomInfo.Name);
                    yield break;
                }
            }
            // If two person room not present rn, create one and wait for someone to join
            Debug.Log("Two human room not found in current room list, creating one");
            LobbyNetwork.S.TryCreateRoom(
                Guid.NewGuid().ToString(), 
                roomOptions);
        }
    }

    /* TODO: !!!!!!!!!!!!!!!!
       Current hacky solution: join & create is the same button. Will try to join room with name
       first. If failed a room will be created with the same name. This is obviously NOT SAFE
       but it works for testing purposes*/
    public void JoinRoomAttempt(string roomName = "")
    {
        Debug.LogFormat("Joining room with name: {0}", roomName);

        if (roomName == "") {
            roomName = LobbyUI.S.GetRoomNameEntered();
        }
        // TODO: Handle for names that are too long
        if (roomName != "")
        {
            onBoardingState = OnBoardingState.Loading;
            LobbyNetwork.S.TryJoinRoom(roomName);
            LobbyUI.S.ShowLoadingUI();
        }
    }

    public void OnJoinRoomAttemptFailed()
    {

        if (CompetitionMiddleware.Instance.IsAI) {
            //AIs are not allowed to create rooms so if the room doesn't exist we just bail
        #if HMT_BUILD
            Debug.LogErrorFormat("Provided room name: {0} does not exist. Exiting Application", Args.GetArgValue("photonroom", ""));
        #endif
            Application.Quit();
        }
        else {
            LobbyUI.S.ShowLoadingUI("Room Does Not Exist, Creating One...");
            LobbyNetwork.S.TryCreateRoom(LobbyUI.S.GetRoomNameEntered());
        }
    }

    public void OnCreateRoomFailed()
    {
        StartCoroutine(CreateRoomFailed());
    }

    IEnumerator CreateRoomFailed()
    {
        LobbyUI.S.ShowLoadingUI("Create/Join Room Failed, this is most likely the room name already exist and is closed.");
        yield return new WaitForSeconds(3.0f);
        onBoardingState = OnBoardingState.CreateOrJoinRoom;
        LobbyUI.S.ShowCreateJoinRoomUI();
    }

    public void OnRoomEntered()
    {
        Debug.Log("Joined a room, initiating travel to room");
        SceneManager.LoadScene(GlobalConstant.ROOM_SCENE);
    }

    public IEnumerator OnRoomCreated()
    {
        CompetitionMiddleware.Instance.numPlayer = _numPerson;
        if (_numPerson == 1)
        {
            SceneManager.LoadScene(GlobalConstant.ROOM_SCENE);
        }
        else
        {
            float startTime = Time.time;

            while (Time.time - startTime < _timer) {
                //TODO: should actually just use the OnPlayerEnteredRoom Callback
                if (PhotonNetwork.CurrentRoom.PlayerCount < 2) {
                    yield return null;
                }
                else {
                    SceneManager.LoadScene(GlobalConstant.ROOM_SCENE);
                    yield break;
                }
            }
            CompetitionMiddleware.Instance.LogQueueTimeout(_timer);
            // fall back to one person game
            CompetitionMiddleware.Instance.LogRemoveCondition("2-human");
            _numPerson = 1;
            CompetitionMiddleware.Instance.LogAssignCondition("1-human-fallback");
            CompetitionMiddleware.Instance.numPlayer = _numPerson;
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            SceneManager.LoadScene(GlobalConstant.ROOM_SCENE);
        }
    }

    // ============ Back Button Logic ============

    // public void BackButtonClicked()
    // {
    //     switch (onBoardingState)
    //     {
    //         case OnBoardingState.ChooseJoinRoomMode:
    //             onBoardingState = OnBoardingState.Loading;
    //             LobbyUI.S.ShowLoadingUI();
    //             LobbyNetwork.S.TryDisconnectFromServer();
    //             break;
    //         default:
    //             Debug.LogWarning("Back Btn is presented where it shouldn't be");
    //             break;
    //     }
    // }
}
