using System;
using System.Collections;
using System.Collections.Generic;
using GameConstant;
using Photon.Pun;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkMiddleware : MonoBehaviourPunCallbacks
{
    // ======== Used During Room ========
    
    // Actor 2 character map
    // public Dictionary<int, int> actor2character;

    // random seed, set to be the same across the network
    // so that (hopefully) we don't need to sync random dice rolls separately
    public int randomSeed = -1;
    
    // referenced by game manager
    public int myCharacterID = -1;

    // ======== Used During Gameplay ========

    public static NetworkMiddleware S;

    private void Awake()
    {
        if (S) Destroy(this.gameObject);
        else
        {
            S = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {

    }

    public void SetupMiddleware(int randomSeed_, int characterID_)
    {
        randomSeed = randomSeed_;
        myCharacterID = characterID_ - 1;
        Random.InitState(randomSeed);
        Debug.Log($"Player {characterID_} middleware setup with random seed {randomSeed}");
    }

    public int NextRandomInt(int min, int max)
    {
        return Random.Range(min, max);
    }

    public void ReadyForNextPhaseLocal(int CharID, bool ready) {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("ReadyForNextPhaseRPC", RpcTarget.All, CharID, ready);
        }
        else {
            ReadyForNextPhaseRPC(CharID, ready);
        }
    }

    [PunRPC]
    private void ReadyForNextPhaseRPC(int CharID, bool ready)
    {
        
        IntegratedGameManager.S.inSceneCharacters[CharID].ReadyForNextPhase = ready;
        UIManager.S.UpdateCommonHUD();
        UIManager.S.UpdateCharacterActionStatus(CharID, ready);
        
        if (ready) {
            if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Pinning) {
                IntegratedGameManager.S.CheckPingPhaseEnd();
            }

            if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Planning) {
                IntegratedGameManager.S.CheckPlanPhaseEnd();
            }
        }
    }

    public void MovePingCursorOnCharacterLocal(Character.Direction direction, int charID) {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("MovePingCursorOnCharacterRPC", RpcTarget.All, direction, charID);
        }
        else {
            MovePingCursorOnCharacterRPC(direction, charID);
        }
    }

    [PunRPC]
    private void MovePingCursorOnCharacterRPC(Character.Direction direction, int charID) {
        if (charID != myCharacterID) {
            IntegratedGameManager.S.inSceneCharacters[charID].MovePingCursor(direction);
        }
    }


    public void AddMoveToCharacterLocal(Character.Direction direction, int charID)
    {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("AddMoveToCharacterRPC", RpcTarget.All, direction, charID);
        }
        
        else {
            AddMoveToCharacterRPC(direction, charID);
        }
    }

    [PunRPC]
    private void AddMoveToCharacterRPC(Character.Direction direction, int charID) {
        IntegratedGameManager.S.inSceneCharacters[charID].AddActionToPlan(direction);
        UIManager.S.UpdateCommonHUD();
        UIManager.S.UpdateCharacterPlanUI();
    }

    public void UndoPlanStepLocal(int charID) {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("UndoPlanStepRpc", RpcTarget.All, charID);
        }
        else {
            UndoPlanStepRpc(charID);
        }
    }

    [PunRPC]
    private void UndoPlanStepRpc(int charID) {
        IntegratedGameManager.S.inSceneCharacters[charID].UndoPlanStep();
        UIManager.S.UpdateCommonHUD();
        UIManager.S.UpdateCharacterPlanUI();
    }

    public void DropPinAtLocal(int pinTypeIdx, int row, int col, int charId) {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC(
                "DropPinAtRpc", 
                RpcTarget.All,
                pinTypeIdx, 
                row, col, 
                charId);
        }
        else {
            DropPinAtRpc(pinTypeIdx, row, col, charId);
        }
        
    }

    [PunRPC]
    private void DropPinAtRpc(int pinTypeIdx, int row, int col, int charId) {
        PinningSystem.S.AddPin(pinTypeIdx, row, col, charId);
        IntegratedGameManager.S.inSceneCharacters[charId].PinPlaced();
        UIManager.S.UpdateCommonHUD();
        UIManager.S.UpdateCharacterPinUI();
    }
}
