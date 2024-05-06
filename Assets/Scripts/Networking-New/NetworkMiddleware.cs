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

    public UIManager uiManager;

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
        
        IntegratedGameManager.S.inSceneCharacters[CharID].ReadyForNextPhase = ready;
        uiManager = FindObjectOfType<UIManager>();
        uiManager.UpdateCharacterActionStatus(CharID, ready = ready);
        
        if (ready)
        {
            if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Pinning)
            {
                IntegratedGameManager.S.CheckPingPhaseEnd();
            }

            if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Planning)
            {
                IntegratedGameManager.S.CheckPlanPhaseEnd();
            }
        }
    }

    public void MovePingCursorOnCharacterLocal(Character.Direction direction, int charID) {
        photonView.RPC("MovePingCursorOnCharacterRPC", RpcTarget.All, direction, charID);
    }

    [PunRPC]
    private void MovePingCursorOnCharacterRPC(Character.Direction direction, int charID) {
        if (charID != myCharacterID) {
            IntegratedGameManager.S.inSceneCharacters[charID].MovePingCursor(direction);
        }
    }


    public void AddMoveToCharacterLocal(Character.Direction direction, int charID)
    {
        photonView.RPC("AddMoveToCharacterRPC", RpcTarget.All, direction, charID);
    }

    [PunRPC]
    private void AddMoveToCharacterRPC(Character.Direction direction, int charID)
    {
        if (charID != myCharacterID)
        {
            IntegratedGameManager.S.inSceneCharacters[charID].ActionPlan.Add(direction);
        }
        else
        {
            IntegratedGameManager.S.localChar.AddActionToPlan(direction);
            IntegratedGameManager.S.player.UpdateCharacterUI();
            IntegratedGameManager.S.uiManager.UpdateActionPointsRemaining(IntegratedGameManager.S.localChar.ActionPointsRemaining);
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
            IntegratedGameManager.S.inSceneCharacters[charID].ActionPlan.RemoveAt(IntegratedGameManager.S.inSceneCharacters[charID].ActionPlan.Count-1);
        }
        else
        {
            IntegratedGameManager.S.localChar.UndoPlanStep();
            IntegratedGameManager.S.uiManager.UpdateActionPointsRemaining(IntegratedGameManager.S.localChar.ActionPointsRemaining);
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
        IntegratedGameManager.S.NewPlayerPin();
    }

    [PunRPC]
    private void DropPinAtRpc(int pinTypeIdx, int row, int col, int charId)
    {
        PinningSystem.S.AddPin(pinTypeIdx, row, col, charId);
    }
}
