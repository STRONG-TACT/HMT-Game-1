using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class LobbyNetwork : MonoBehaviourPunCallbacks
{
    public static LobbyNetwork S;

    public bool playerQuitConnection = false;
    
    private void Awake()
    {
        if (S) Destroy(this);
        else S = this;
    }

    public void TryConnectToServer()
    {
        Debug.Log("Try connect to server.");
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }
    
    // initial server connection succeed
    public override void OnConnectedToMaster()
    {
        Debug.Log("Successfully connected to server.");
        NetworkLobbyManager.S.OnConnectToServer();
    }

    // TODO: might be worth to handle connection error, casing on cause
    // this function also handles player quiting the game
    public override void OnDisconnected(DisconnectCause cause)
    {
        if (playerQuitConnection)
        {
            Debug.Log("Successfully disconnected.");
            NetworkLobbyManager.S.OnDisconnectSucceed();
            playerQuitConnection = false;
        }
        else NetworkLobbyManager.S.OnUnexpectedDisconnect();
    }

    public void TryDisconnectFromServer()
    {
        playerQuitConnection = true;
        Debug.Log("Disconnecting");
        PhotonNetwork.Disconnect();
    }
}
