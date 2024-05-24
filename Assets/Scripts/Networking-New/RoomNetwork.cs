using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    private List<string> CharacterMapping = null;

    private class PlayerEntry {
        public string userId;
        public string sessionId;
        public int actorNumber;
        public bool isAI;
        public bool Ready;

        public PlayerEntry(int actorNumber, string userId, string sessionId, bool isAI) {
            this.userId = userId;
            this.sessionId = sessionId;
            this.actorNumber = actorNumber;
            this.isAI = isAI;
            this.Ready = isAI;
        }
    }

    private Dictionary<string, PlayerEntry> _playerEntries = new Dictionary<string, PlayerEntry>();

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
            //photonView.RPC("SetupNetworkMiddleware",
            //    RpcTarget.MasterClient,
            //    _randomSeed,
            //    PhotonNetwork.LocalPlayer.ActorNumber);
            photonView.RPC("HandshakeSessionIds",
                RpcTarget.MasterClient,
                PhotonNetwork.LocalPlayer.ActorNumber,
                CompetitionMiddleware.Instance.UserID,
                CompetitionMiddleware.Instance.SessionID,
                CompetitionMiddleware.Instance.IsAI);
            yield break;
        }
        else if (CompetitionMiddleware.Instance.IsAI) {
            float startTime = Time.time;
            int minutes = 1;
            Debug.LogFormat("Autonomus mode joined room, starting wait for {0} agents", 4- PhotonNetwork.CurrentRoom.PlayerCount);
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
                if(Time.time - startTime > minutes*60) {
                    Debug.LogFormat("Waiting for AIs to connect... {0} agents connected, {1} minutes", CompetitionMiddleware.Instance.RegisteredAgents.Count, minutes);
                    minutes++;
                }
                yield return null;
            }

            Debug.LogFormat("All agents connected (took {0} seconds), sending handshakes.", Time.time - startTime);
            foreach (var agent in CompetitionMiddleware.Instance.RegisteredAgents.Values) {
                photonView.RPC("HandshakeSessionIds",
                                RpcTarget.MasterClient,
                                agent.characterID,
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

/*    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) {
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
    }*/

    [PunRPC]
    private void HandshakeSessionIds(int actorNumber, string userId, string sessionId, bool isAI) {
        if (PhotonNetwork.IsMasterClient) {
            Debug.LogFormat("Recieved Session Id Handshake for actor:{0} userId:{1} sessionId:{2} isAI:{3}", actorNumber, userId, sessionId, isAI);
            if (isAI) {
                //TODO AI's send actorNumbers that are incrememnted CharacterIds we may want to treat them differently
                // For now the incrememntation happens on send See Start above.
                _playerEntries[userId] = new PlayerEntry(actorNumber, userId, sessionId, isAI);
                CharacterMapping[actorNumber] = userId;
            }
            else {
                _playerEntries[userId] = new PlayerEntry(actorNumber, userId, sessionId, isAI);
            }
            if (ReadyCount() == 3) {
                Debug.LogFormat("Recieved Handshake from an AI, locking room and moving on");
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
                CallBeginRun();
            }
        }
    }

    private void CallBeginRun() {
        if(CharacterMapping == null) {
            CharacterMapping = CreateCharacterMapping();
        }
        string runID = Guid.NewGuid().ToString();
        _randomSeed = Random.Range(0, 10000);
        photonView.RPC("BeginRunLocal", RpcTarget.All, runID, _randomSeed, CharacterMapping[0], CharacterMapping[1], CharacterMapping[2]);
    }




    [PunRPC]
    private void BeginRunLocal(string runId, int seed, string dwarfPlayer, string giantPlayer, string humanPlayer) {
        Debug.LogFormat("BeginRunLocal runId:{0} seed:{1} dwarfPlayer:{2} giantPlayer:{3} humanPlayer:{4}, myUserID:{5}", runId, seed, dwarfPlayer, giantPlayer, humanPlayer, CompetitionMiddleware.Instance.UserID);



        if (PhotonNetwork.IsMasterClient) {
            CompetitionMiddleware.Instance.LogStartGameNetwork(runId,
                dwarfPlayer, _playerEntries[dwarfPlayer].sessionId, _playerEntries[dwarfPlayer].isAI,
                giantPlayer, _playerEntries[giantPlayer].sessionId, _playerEntries[giantPlayer].isAI,
                humanPlayer, _playerEntries[humanPlayer].sessionId, _playerEntries[humanPlayer].isAI);
        }
        else {
            CompetitionMiddleware.Instance.SetGameID(runId);
        }

        if (CompetitionMiddleware.Instance.IsAI) {
            PhotonNetwork.LocalPlayer.NickName = "AI AGENT";
            NetworkMiddleware.S.SetupMiddleware(seed, 0);
        }
        else if(CompetitionMiddleware.Instance.UserID == dwarfPlayer)
        {
            PhotonNetwork.LocalPlayer.NickName = "Dwarf";
            NetworkMiddleware.S.SetupMiddleware(seed, 0);
        }
        else if (CompetitionMiddleware.Instance.UserID == giantPlayer) {
            PhotonNetwork.LocalPlayer.NickName = "Giant";
            NetworkMiddleware.S.SetupMiddleware(seed, 1);
        }
        else if (CompetitionMiddleware.Instance.UserID == humanPlayer) {
            PhotonNetwork.LocalPlayer.NickName = "Human";
            NetworkMiddleware.S.SetupMiddleware(seed, 2);
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
        Debug.LogFormat("ReadyCount {0}", count);
        return count;
    }

    private string actorNumberToUserID(int actorNumber) {
        foreach (var entry in _playerEntries) {
            if (entry.Value.actorNumber == actorNumber) {
                return entry.Value.userId;
            }
        }
        return null;
    }


    [PunRPC]
    private void RegisterPlayerReady(int actorNumber) {
        Debug.Log($"Player {actorNumber} ready");
        string userId = actorNumberToUserID(actorNumber);
        _playerEntries[userId].Ready = true;
        if (ReadyCount() == 3) {
            CallBeginRun();
        }
    }

    public void RegisterPlayerReadyLocal() {
        photonView.RPC("RegisterPlayerReady", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public void LaunchGameWithAIsLocal() {
        string dwarfID, giantID, humanID;
        CharacterMapping = CreateCharacterMapping();
        dwarfID = CharacterMapping[0] == "uniform" ? "uniform" : null;
        giantID = CharacterMapping[1] == "uniform" ? "uniform" : null;
        humanID = CharacterMapping[2] == "uniform" ? "uniform" : null;
        CompetitionMiddleware.Instance.CallLaunchGame(PhotonNetwork.CurrentRoom.Name, dwarfID, giantID, humanID);
    }

    private List<string> CreateCharacterMapping() {
        List<string> characterMapping = new List<string>();
        foreach (var entry in _playerEntries) {
            characterMapping.Add(entry.Value.userId);
        }
        while (characterMapping.Count < 3) {
            characterMapping.Add("uniform");
        }
        characterMapping.OrderBy(_ => Random.value).ToList();
        return characterMapping;
    }
}
