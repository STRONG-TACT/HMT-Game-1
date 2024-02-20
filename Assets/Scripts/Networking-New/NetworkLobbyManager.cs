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
