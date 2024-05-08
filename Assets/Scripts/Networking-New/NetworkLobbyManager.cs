using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameConstant;
using HMT;

public class NetworkLobbyManager : MonoBehaviour
{
    public OnBoardingState onBoardingState = OnBoardingState.ChooseGameMode;
    public OnBoardingState playChoice = OnBoardingState.ChooseGameMode;
    
    // Singleton reference
    public static NetworkLobbyManager S;

    private void Awake()
    {
        if (S) Destroy(this);
        else S = this;
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

        if(Args.GetArgValue("localmode", false)) {
            LocalTestSelected();
            yield break;
        }
        else {
            //act like we cliked Online
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
#endif


    public void LocalTestSelected()
    {
        SceneManager.LoadScene(GlobalConstant.LOCAL_SCENE);
    }
    
    // ============ Server Connection & Handle ============

    public void OnlinePlaySelected()
    {
        onBoardingState = OnBoardingState.Loading;
        LobbyNetwork.S.TryConnectToServer();
        LobbyUI.S.ShowLoadingUI("Connecting to Server...");
    }

    public void OnConnectToServer()
    {
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

    public void OnJoinLobbySucceed()
    {
        onBoardingState = OnBoardingState.CreateOrJoinRoom;
        LobbyUI.S.ShowCreateJoinRoomUI();
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
            Debug.LogErrorFormat("Provided room name: {0} does not exist. Exiting Application", Args.GetArgValue("photonroom", ""));
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
