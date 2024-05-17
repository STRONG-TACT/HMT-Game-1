using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class RoomNetwork : MonoBehaviourPunCallbacks {
    // Actor 2 character map
    // TODO: Implement a random shuffle

    // random seed
    private int _randomSeed = -1;

    public static RoomNetwork S;

    private class PlayerEntry {
        public string userId;
        public string sessionId;
        public bool isAI;
        public bool Ready;

        public PlayerEntry(string userId, string sessionId, bool isAI) {
            this.userId = userId;
            this.sessionId = sessionId;
            this.isAI = isAI;
            this.Ready = isAI;
        }
    }

    private Dictionary<int, PlayerEntry> _playerEntries = new Dictionary<int, PlayerEntry>();

    private void Awake() {
        if (S) Destroy(this);
        else S = this;
    }

    private IEnumerator Start() {
        Debug.Log("My UserId: " + PhotonNetwork.LocalPlayer.UserId);
        Debug.Log("Am I the master: " + PhotonNetwork.IsMasterClient);

        if (PhotonNetwork.IsMasterClient) {
            _randomSeed = Random.Range(0, 10000);
            // SetupNetworkMiddleware(_randomSeed, PhotonNetwork.LocalPlayer.ActorNumber);
            photonView.RPC("SetupNetworkMiddleware",
                RpcTarget.MasterClient,
                _randomSeed,
                PhotonNetwork.LocalPlayer.ActorNumber);
            photonView.RPC("HandshakeSessionIds",
                RpcTarget.MasterClient,
                PhotonNetwork.LocalPlayer.ActorNumber,
                CompetitionMiddleware.Instance.UserID,
                CompetitionMiddleware.Instance.SessionID,
                CompetitionMiddleware.Instance.IsAI);
            yield break;
        }
        else if (CompetitionMiddleware.Instance.IsAI) {
            while (CompetitionMiddleware.Instance.RegisteredAgents.Count + PhotonNetwork.CurrentRoom.PlayerCount < 4) {
                /*
                 * Agents register themselves to the HMTInterface as their first command.
                 * This adds them to the RegisteredAgents dictionary.
                 * Once we have agents equal to fill the available player slots (4 because this instance takes up a Photon seat),
                 * we progress and handshake in the session Ids to master client, which should trigger progression to play
                 * once all agents are registered. (see the HandshakeSessionIds RPC function).
                 * 
                 * We should benchmark this processs and consider if we want some kind of timeout both here locally
                 * and/or on the web client end.
                 */ 
                yield return null;
            }
            foreach (var agent in CompetitionMiddleware.Instance.RegisteredAgents.Values) {
                photonView.RPC("HandshakeSessionIds",
                                RpcTarget.MasterClient,
                                PhotonNetwork.LocalPlayer.ActorNumber,
                                agent.agentID,
                                agent.sessionID,
                                true);
            }
        }
        else {
            photonView.RPC("HandshakeSessionIds",
                RpcTarget.MasterClient,
                PhotonNetwork.LocalPlayer.ActorNumber,
                CompetitionMiddleware.Instance.UserID,
                CompetitionMiddleware.Instance.SessionID,
                CompetitionMiddleware.Instance.IsAI);
        }
        yield break;
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) {
        if (PhotonNetwork.IsMasterClient) {
            // sync network data to all clients' network middleware
            photonView.RPC("SetupNetworkMiddleware",
                RpcTarget.Others,
                _randomSeed,
                newPlayer.ActorNumber);

            // if the room is full, set room property so no other player will join
            if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers) {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }
        }

    }

    [PunRPC]
    private void SetupNetworkMiddleware(int randomSeed, int characterID) {
        if (NetworkMiddleware.S.myCharacterID != -1) return;
        NetworkMiddleware.S.SetupMiddleware(randomSeed, characterID);
    }

    [PunRPC]
    private void HandshakeSessionIds(int actorNumber, string userId, string sessionId, bool isAI) {
        if (PhotonNetwork.IsMasterClient) {
            _playerEntries[actorNumber] = new PlayerEntry(userId, sessionId, isAI);

            if (ReadyCount() == 3) {
                Debug.LogFormat("Recieved Handshake from an AI, locking room and moving on");
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
                CallBeginRun();
            }
        }
    }

    //need a PUNRPC target for reciving the competition ids from clients
    //store those in a mapping to character IDs

    private void CallBeginRun() {
        string runID = Guid.NewGuid().ToString();
        photonView.RPC("BeginRunLocal", RpcTarget.All, runID);
    }


    [PunRPC]
    private void BeginRunLocal(string runId) {
        if (PhotonNetwork.IsMasterClient) {
            CompetitionMiddleware.Instance.LogStartRunNetwork(runId,
                _playerEntries[1].userId, _playerEntries[1].sessionId, _playerEntries[1].isAI,
                _playerEntries[2].userId, _playerEntries[2].sessionId, _playerEntries[2].isAI,
                _playerEntries[3].userId, _playerEntries[3].sessionId, _playerEntries[3].isAI);
        }
        else {
            CompetitionMiddleware.Instance.SetRunID(runId);
        }

        SceneManager.LoadScene(GameConstant.GlobalConstant.NETWORK_SCENE);
    }

    private int ReadyCount() {
        int count = 0;
        foreach (var entry in _playerEntries) {
            if (entry.Value.Ready) {
                count++;
            }
        }
        return count;
    }

    [PunRPC]
    private void RegisterPlayerReady(int actorNumber) {
        Debug.Log($"Player {actorNumber} ready");
        _playerEntries[actorNumber].Ready = true;
        if (ReadyCount() == 3) {
            CallBeginRun();
        }
    }

    public void RegisterPlayerReadyLocal() {
        photonView.RPC("RegisterPlayerReady", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public void LaunchGameWithAIsLocal() {
        string dwarfID, giantID, humanID;
        //TODO make this more robust to changes in player-character mapping
        if (_playerEntries.TryGetValue(1, out PlayerEntry playerEntry)) {
            dwarfID = null;
        }
        else {
            dwarfID = "random";
        }
        if (_playerEntries.TryGetValue(2, out playerEntry)) {
            giantID = null;
        }
        else {
            giantID = "random";
        }
        if (_playerEntries.TryGetValue(3, out playerEntry)) {
            humanID = null;
        }
        else {
            humanID = "random";
        }
        CompetitionMiddleware.Instance.CallLaunchGame(PhotonNetwork.CurrentRoom.Name, dwarfID, giantID, humanID);
    }
}
