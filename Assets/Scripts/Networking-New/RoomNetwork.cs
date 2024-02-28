using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class RoomNetwork : MonoBehaviourPunCallbacks
{
    // Actor 2 character map
    // TODO: Implement a random shuffle
    
    // random seed
    private int _randomSeed = -1;

    private int _playerReady = 0;

    public static RoomNetwork S;

    private void Awake()
    {
        if (S) Destroy(this);
        else S = this;
    }

    private void Start()
    {
        Debug.Log("My UserId: " + PhotonNetwork.LocalPlayer.UserId);
        Debug.Log("Am I the master: " + PhotonNetwork.IsMasterClient);

        if (PhotonNetwork.IsMasterClient)
        {
            _randomSeed = Random.Range(0, 10000);
            // SetupNetworkMiddleware(_randomSeed, PhotonNetwork.LocalPlayer.ActorNumber);
            photonView.RPC(
                "SetupNetworkMiddleware", 
                RpcTarget.MasterClient, 
                _randomSeed, 
                PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // sync network data to all clients' network middleware
            photonView.RPC(
            "SetupNetworkMiddleware", 
            RpcTarget.Others, 
            _randomSeed, 
            newPlayer.ActorNumber);
            
            // if the room is full, set room property so no other player will join
            if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }
        }
    }

    [PunRPC]
    private void SetupNetworkMiddleware(int randomSeed, int characterID)
    {
        if (NetworkMiddleware.S.myCharacterID != -1) return;
        NetworkMiddleware.S.SetupMiddleware(randomSeed, characterID);
    }

    [PunRPC]
    private void TravelToGameLevel()
    {
        SceneManager.LoadScene(GameConstant.GlobalConstant.GAME_LEVEL);
    }

    [PunRPC]
    private void RegisterPlayerReady(int actorNumber)
    {
        Debug.Log($"Player {actorNumber} ready");
        _playerReady++;
        if (_playerReady == 3)
        {
            photonView.RPC("TravelToGameLevel", RpcTarget.All);
        }
    }

    public void RegisterPlayerReadyLocal()
    {
        photonView.RPC("RegisterPlayerReady", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }
}
