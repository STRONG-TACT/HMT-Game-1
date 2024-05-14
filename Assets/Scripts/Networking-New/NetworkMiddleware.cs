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

    private void Awake() {
        if (S) {
            Destroy(this.gameObject);
        }
        else {
            S = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {

    }

    public void SetupMiddleware(int randomSeed_, int characterID_) {
        randomSeed = randomSeed_;
        myCharacterID = characterID_ - 1;
        Random.InitState(randomSeed);
        Debug.Log($"Player {characterID_} middleware setup with random seed {randomSeed}");
    }

    public int NextRandomInt(int min, int max) {
        return Random.Range(min, max);
    }

    public void CallReadyForNextPhase(int charID, bool ready) {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("ReadyForNextPhaseLocal", RpcTarget.All, charID, ready);
        }
        else {
            ReadyForNextPhaseLocal(charID, ready);
        }
    }

    [PunRPC]
    private void ReadyForNextPhaseLocal(int charID, bool ready) {
        
        IntegratedGameManager.S.inSceneCharacters[charID].ReadyForNextPhase = ready;
        UIManager.S.UpdateCommonHUD();
        UIManager.S.UpdateCharacterActionStatus(charID, ready);
        
        if (ready) {
            if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Pinning) {
                IntegratedGameManager.S.CheckPingPhaseEnd();
            }

            if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Planning) {
                IntegratedGameManager.S.CheckPlanPhaseEnd();
            }
        }
    }

    public void CallMovePingCursorOnCharacter(int charID, Character.Direction direction) {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("MovePingCursorOnCharacterLocal", RpcTarget.All,charID, direction);
        }
        else {
            MovePingCursorOnCharacterLocal(charID, direction);
        }
    }

    [PunRPC]
    private void MovePingCursorOnCharacterLocal(int charID, Character.Direction direction) {
        if (charID != myCharacterID) {
            IntegratedGameManager.S.inSceneCharacters[charID].MovePingCursor(direction);
        }
    }


    public void CallAddMoveToCharacter(int charID, Character.Direction direction) {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("AddMoveToCharacterLocal", RpcTarget.All, charID, direction);
        }
        
        else {
            AddMoveToCharacterLocal(charID, direction);
        }
    }

    [PunRPC]
    private void AddMoveToCharacterLocal(int charID, Character.Direction direction) {
        IntegratedGameManager.S.inSceneCharacters[charID].AddActionToPlan(direction);
        UIManager.S.UpdateCommonHUD();
        UIManager.S.UpdateCharacterPlanUI();
    }

    public void CallUndoPlanStep(int charID) {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("UndoPlanStepLocal", RpcTarget.All, charID);
        }
        else {
            UndoPlanStepLocal(charID);
        }
    }

    [PunRPC]
    private void UndoPlanStepLocal(int charID) {
        IntegratedGameManager.S.inSceneCharacters[charID].UndoPlanStep();
        UIManager.S.UpdateCommonHUD();
        UIManager.S.UpdateCharacterPlanUI();
    }

    public void CallDropPinAt(int charId, int pinTypeIdx, int row, int col) {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC(
                "DropPinAtLocal", 
                RpcTarget.All,
                pinTypeIdx, 
                row, col, 
                charId);
        }
        else {
            DropPinAtLocal(charId, pinTypeIdx, row, col);
        }
        
    }

    [PunRPC]
    private void DropPinAtLocal(int charId, int pinTypeIdx, int row, int col) {
        PinningSystem.S.InstantiatePin(pinTypeIdx, row, col, charId);
        IntegratedGameManager.S.inSceneCharacters[charId].PinPlaced();
        UIManager.S.UpdateCommonHUD();
        UIManager.S.UpdateCharacterPinUI();
    }
}
