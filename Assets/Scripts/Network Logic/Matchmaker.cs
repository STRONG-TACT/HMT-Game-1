using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using GameConstant;

public class Matchmaker : MonoBehaviourPunCallbacks
{
    public static Matchmaker Instance { get; private set; } = null;

    public bool playerQuitConnection = false;

    private RoomOptions _defaultRoomOptions = new RoomOptions { MaxPlayers = 3 };
    
    private void Awake()
    {
        if (Instance) Destroy(this);
        else Instance = this;
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
        LobbyManager.Instance.OnConnectToServer();
    }

    // TODO: might be worth to handle connection error, casing on cause
    // this function also handles player quiting the game
    public override void OnDisconnected(DisconnectCause cause)
    {
        if (playerQuitConnection)
        {
            Debug.Log("Successfully disconnected.");
            LobbyManager.Instance.OnDisconnectSucceed();
            playerQuitConnection = false;
        }
        else LobbyManager.Instance.OnUnexpectedDisconnect();
    }

    public void TryDisconnectFromServer()
    {
        playerQuitConnection = true;
        Debug.Log("Disconnecting");
        PhotonNetwork.Disconnect();
    }
    
    // =============
    public void TryJoinLobby()
    {
        Debug.Log("Try Join Lobby");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        StartCoroutine(LobbyManager.Instance.OnJoinLobbySucceed());
    }

    public void TryJoinRoom(string roomName)
    {
        Debug.Log($"Trying to join room {roomName}");
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Join room failed, attempt to create one");
        LobbyManager.Instance.OnJoinRoomAttemptFailed();
    }

    public void TryCreateRoom(string roomName, RoomOptions roomOptions = null)
    {
        roomOptions ??= _defaultRoomOptions;
        Debug.Log($"Trying to create room {roomName}");
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void TryJoinRoomWithProperty(Hashtable expectedProperties)
    {
        PhotonNetwork.JoinRandomRoom(expectedProperties, 3);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Failed to create a room");
        LobbyManager.Instance.OnCreateRoomFailed();
    }

    public override void OnCreatedRoom()
    {
        StartCoroutine(LobbyManager.Instance.OnRoomCreated());
    }

    public override void OnJoinedRoom()
    {
        CompetitionMiddleware.Instance.LogJoinRoom(PhotonNetwork.CurrentRoom.Name);

        if (!PhotonNetwork.IsMasterClient)
        {
            LobbyManager.Instance.OnRoomEntered();
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        LobbyManager.Instance.ListOfRooms = roomList;
    }
}
