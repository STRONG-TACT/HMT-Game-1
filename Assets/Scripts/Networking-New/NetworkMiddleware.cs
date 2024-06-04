using System;
using System.Collections;
using System.Collections.Generic;
using GameConstant;
using Photon.Pun;
using Photon.Realtime;
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

    public void SetupMiddleware(int randomSeed_, int characterID_) {
        randomSeed = randomSeed_;
        myCharacterID = characterID_;
        Random.InitState(randomSeed);
        Debug.Log($"Player {characterID_} middleware setup with random seed {randomSeed}");
    }

    public int NextRandomInt(int min, int max) {
        return Random.Range(min, max);
    }

    public float NextRandom() {
        return Random.value;
    }

    #region Common RPCs

    /// <summary>
    /// This is similar to the CallReadForNextPhase method but is meant to only be called when a 
    /// Ready state is set automatically rather than by player action. In this case the RPCs should 
    /// only come from MasterClient to avoid duplicate messaging.
    /// </summary>
    /// <param name="charID"></param>
    /// <param name="ready"></param>
    public void CallReadyForNextPhaseAuto(int charID, bool ready) {
        if (IntegratedGameManager.S.isNetworkGame && PhotonNetwork.IsMasterClient) {
            photonView.RPC("ReadyForNextPhaseLocal", RpcTarget.All, charID, ready);
        }
        else {
            ReadyForNextPhaseLocal(charID, ready);
        }
    }

    public void CallReadyForNextPhase(int charID, bool ready) {
        if (ready) {
            CompetitionMiddleware.Instance.LogSubmit(charID);
        }
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
                UIManager.S.UpdateCharacterPinUI();
                IntegratedGameManager.S.CheckPingPhaseEnd();
            }

            if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Planning) {
                UIManager.S.UpdateCharacterPlanUI();
                IntegratedGameManager.S.CheckPlanPhaseEnd();
            }
        }
    }

/*    public void CallSpawnCharacter(int charId, int row, int col) {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("SpawnCharacterLocal", RpcTarget.All, charId, row, col);
        }
        else {
            SpawnCharacterLocal(charId, row, col);
        }
    }

    [PunRPC]
    private void SpawnCharacterLocal(int charId, int row, int col) {
        IntegratedGameManager.S.SpawnCharacter(charId, row, col);
    }

    public void CallCharacterDied(int charId) {
        if(IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("CharacterDiedLocal", RpcTarget.All, charId);
        }
        else {
            CharacterDiedLocal(charId);
        }
    }

    [PunRPC]
    private void CharacterDiedLocal(int charId) {




        IntegratedGameManager.S.inSceneCharacters[charId].Die();
    }
*/
    #endregion

    #region Pinning Phase RPCs

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
        IntegratedGameManager.S.inSceneCharacters[charID].MovePingCursor(direction);
    }

    public void CallDropPinAt(int charId, int pinTypeIdx, int row, int col) {
        CompetitionMiddleware.Instance.LogPlacePin(charId, pinTypeIdx, row, col);
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC(
                "DropPinAtLocal",
                RpcTarget.All,
                charId,
                pinTypeIdx,
                row, col);
        }
        else {
            DropPinAtLocal(charId, pinTypeIdx, row, col);
        }

    }

    [PunRPC]
    private void DropPinAtLocal(int charId, int pinTypeIdx, int row, int col) {
        PinningSystem.S.InstantiatePin(charId, pinTypeIdx, row, col);
        IntegratedGameManager.S.inSceneCharacters[charId].PinPlaced();
        UIManager.S.UpdateCommonHUD();
        UIManager.S.UpdateCharacterPinUI();
    }

    #endregion

    #region Planning Phase RPCs

    public void CallAddMoveToCharacter(int charID, Character.Direction direction) {
        CompetitionMiddleware.Instance.LogAddPlan(charID, direction);
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
        CompetitionMiddleware.Instance.LogUndoPlan(charID);
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

    #endregion

    // handling player disconnect
    public override void OnPlayerLeftRoom(Player player)
    {
        // If we're in the survey scene, ignore this whole callback
        // Maybe even if the game is in an endstate

        Debug.Log($"Player {player.NickName} has left the room");
        CompetitionMiddleware.Instance.LogDisconnect(player.NickName);

        if (UIManager.S) { 
            UIManager.S.ShowOtherPlayerDisconnectUI(player.NickName);

            // The ShowOtherPlayerDisonnectedUI should have a button that takes you to the survey scene
            
        }

#if HMT_BUILD
        // If we're at the end of a game we might want to stay alive for a bit so the AI can know the game is over

        if (!CompetitionMiddleware.Instance.overrideAIMode) {
            Application.Quit();
        }
#endif


    }

    public void CallLogLevelResult(Dictionary<string, Dictionary<string, string>> playerInfo)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CompetitionMiddleware.Instance.CallReportResult(playerInfo);
        }
        Debug.LogWarning("!!");
    }
}
