using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using GameConstant;
using UnityEngine.SceneManagement;

public class LobbyNetwork : MonoBehaviourPunCallbacks
{
    public static LobbyNetwork S;

    public bool playerQuitConnection = false;

    private RoomOptions _defaultRoomOptions = new RoomOptions { MaxPlayers = 3 };
    
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
    
    // =============
    public void TryJoinLobby()
    {
        Debug.Log("Try Join Lobby");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        StartCoroutine(NetworkLobbyManager.S.OnJoinLobbySucceed());
    }

    public void TryJoinRoom(string roomName)
    {
        Debug.Log($"Trying to join room {roomName}");
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Join room failed, attempt to create one");
        NetworkLobbyManager.S.OnJoinRoomAttemptFailed();
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
        NetworkLobbyManager.S.OnCreateRoomFailed();
    }

    public override void OnCreatedRoom()
    {
        StartCoroutine(NetworkLobbyManager.S.OnRoomCreated());
    }

    public override void OnJoinedRoom()
    {
        CompetitionMiddleware.Instance.LogJoinRoom(PhotonNetwork.CurrentRoom.Name);

        if (!PhotonNetwork.IsMasterClient)
        {
            NetworkLobbyManager.S.OnRoomEntered();
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        NetworkLobbyManager.S.ListOfRooms = roomList;
    }

    public void AllClientTravelToRoom()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC("AllClientTravelToRoomRPC", RpcTarget.All);
    }

    [PunRPC]
    public void AllClientTravelToRoomRPC()
    {
        SceneManager.LoadScene(GlobalConstant.ROOM_SCENE);
    }
}
