using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameConstant;

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

    public void LocalTestSelected()
    {
        SceneManager.LoadScene(GlobalConstant.LOCAL_TEST_LEVEL);
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
        LobbyUI.S.ShowJoinRoomModeUI();
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
    public void JoinRoomAttempt()
    {
        string roomName = LobbyUI.S.GetRoomNameEntered();
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
        LobbyUI.S.ShowLoadingUI("Room Does Not Exist, Creating One...");
        LobbyNetwork.S.TryCreateRoom(LobbyUI.S.GetRoomNameEntered());
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
        SceneManager.LoadScene(GlobalConstant.ROOM_LEVEL);
    }

    // ============ Back Button Logic ============

    public void BackButtonClicked()
    {
        switch (onBoardingState)
        {
            case OnBoardingState.ChooseJoinRoomMode:
                onBoardingState = OnBoardingState.Loading;
                LobbyUI.S.ShowLoadingUI();
                LobbyNetwork.S.TryDisconnectFromServer();
                break;
            default:
                Debug.LogWarning("Back Btn is presented where it shouldn't be");
                break;
        }
    }
}