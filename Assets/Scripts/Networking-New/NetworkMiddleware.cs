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

    public void ReadyForNextPhaseLocal(int CharID, bool ready)
    {
        photonView.RPC("ReadyForNextPhaseRPC", RpcTarget.All, CharID, ready);
    }

    [PunRPC]
    private void ReadyForNextPhaseRPC(int CharID, bool ready)
    {
        NetworkGameManager.S.inSceneCharacters[CharID].ReadyForNextPhase = ready;
        if (ready)
        {
            if (NetworkGameManager.S.gameStatus == GameStatus.Player_Pinning)
            {
                NetworkGameManager.S.CheckPingPhaseEnd();
            }

            if (NetworkGameManager.S.gameStatus == GameStatus.Player_Planning)
            {
                NetworkGameManager.S.CheckPlanPhaseEnd();
            }
        }
    }

    public void MovePingCursorOnCharacterLocal(NetworkCharacter.Direction direction, int charID) {
        photonView.RPC("MovePingCursorOnCharacterRPC", RpcTarget.All, direction, charID);
    }

    [PunRPC]
    private void MovePingCursorOnCharacterRPC(NetworkCharacter.Direction direction, int charID) {
        if (charID != myCharacterID) {
            NetworkGameManager.S.inSceneCharacters[charID].MovePingCursor(direction);
        }
    }


    public void AddMoveToCharacterLocal(NetworkCharacter.Direction direction, int charID)
    {
        photonView.RPC("AddMoveToCharacterRPC", RpcTarget.All, direction, charID);
    }

    [PunRPC]
    private void AddMoveToCharacterRPC(NetworkCharacter.Direction direction, int charID)
    {
        if (charID != myCharacterID)
        {
            NetworkGameManager.S.inSceneCharacters[charID].ActionPlan.Add(direction);
        }
        else
        {
            NetworkGameManager.S.localChar.AddActionToPlan(direction);
            NetworkGameManager.S.player.UpdateCharacterUI();
            NetworkGameManager.S.uiManager.UpdateActionPointsRemaining(NetworkGameManager.S.localChar.ActionPointsRemaining);
        }
    }

    public void UndoPlanStepLocal(int charID)
    {
        photonView.RPC("UndoPlanStepRpc", RpcTarget.All, charID);
    }

    [PunRPC]
    private void UndoPlanStepRpc(int charID)
    {
        if (charID != myCharacterID)
        {
            NetworkGameManager.S.inSceneCharacters[charID].ActionPlan.RemoveAt(NetworkGameManager.S.inSceneCharacters[charID].ActionPlan.Count-1);
        }
        else
        {
            NetworkGameManager.S.localChar.UndoPlanStep();
            NetworkGameManager.S.uiManager.UpdateActionPointsRemaining(NetworkGameManager.S.localChar.ActionPointsRemaining);
        }
    }

    public void DropPinAtLocal(int pinTypeIdx, int row, int col, int charId)
    {
        photonView.RPC(
            "DropPinAtRpc", 
            RpcTarget.All,
            pinTypeIdx, 
            row, col, 
            charId);
        NetworkGameManager.S.NewPlayerPin();
    }

    [PunRPC]
    private void DropPinAtRpc(int pinTypeIdx, int row, int col, int charId)
    {
        PinningSystem.S.AddPin(pinTypeIdx, row, col, charId);
    }
}
