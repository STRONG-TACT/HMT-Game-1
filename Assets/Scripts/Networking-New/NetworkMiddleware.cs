using System;
using System.Collections;
using System.Collections.Generic;
using GameConstant;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            Debug.LogWarning($"Duplicate of networkmiddleware find in {SceneManager.GetActiveScene().name}");
            Destroy(this.gameObject);
        }
        else {
            Debug.LogWarning($"networkmiddleware spawned in {SceneManager.GetActiveScene().name}");
            S = this;
            DontDestroyOnLoad(this.gameObject);
            //SceneManager.activeSceneChanged += OnSceneChanged;
        }
    }

    /*
    void OnSceneChanged(Scene current, Scene next)
    {
        if (next.name == GlobalConstant.LOBBY_SCENE)
        {
            S = null;
            Debug.Log("NetworkMiddleware destroyed");
            Destroy(this.gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }
    */


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
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("ReadyForNextPhaseLocal", RpcTarget.All, charID, ready);
        }
        else {
            ReadyForNextPhaseLocal(charID, ready);
        }
    }


    public void CallSyncStartPlayerturn()
    {
        if (IntegratedGameManager.S.isNetworkGame)
        {
            if (CompetitionMiddleware.Instance.IsAI)
                photonView.RPC("SyncStartPlayerTurnLocal", RpcTarget.All,
                    CompetitionMiddleware.Instance.RegisteredAgents.Count);
            else
                photonView.RPC("SyncStartPlayerTurnLocal", RpcTarget.All, 1);
        }
        else
        {
            SyncStartPlayerTurnLocal(3);
        }
    }

    [PunRPC]
    private void SyncStartPlayerTurnLocal(int numPlayer)
    {

        foreach (Character chara in IntegratedGameManager.S.inSceneCharacters)
        {
            chara.ReadyForNextPhase = false;
        }

        IntegratedGameManager.S.readyForPlayerTurnCount += numPlayer;
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
                if (!IntegratedGameManager.S.isNetworkGame)
                {
                    IntegratedGameManager.S.CheckPingPhaseEnd();
                }
                else if(PhotonNetwork.IsMasterClient)
                    IntegratedGameManager.S.CheckPingPhaseEnd();
                //IntegratedGameManager.S.CheckPingPhaseEnd();
            }

            if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Planning) {
                UIManager.S.UpdateCharacterPlanUI();
                if (!IntegratedGameManager.S.isNetworkGame)
                {
                    IntegratedGameManager.S.CheckPlanPhaseEnd();
                }
                else if (PhotonNetwork.IsMasterClient)
                    IntegratedGameManager.S.CheckPlanPhaseEnd();
            }
        }
    }

    public void CallGotoNextPhase() {
        if (IntegratedGameManager.S.isNetworkGame) {
            photonView.RPC("GotoNextPhaseLocal", RpcTarget.All);
        }
        else {
            GotoNextPhaseLocal();
        }
    }

    [PunRPC]
    public void  GotoNextPhaseLocal() {
        IntegratedGameManager.S.GotoNextPhase();
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
        if(SuveryHandler.S != null)
        {
            Debug.Log("In survey scene, ignore player disconnect");
            return;
        }
        if(IntegratedGameManager.S != null)
        {
            if(IntegratedGameManager.S.gameStatus == GameStatus.GameEnd)
            {
                return;
            }
        }
        if (CompetitionMiddleware.Instance.IsAI)
        {
            Application.Quit();
        }


        Debug.Log($"Player {player.NickName} has left the room");
        CompetitionMiddleware.Instance.LogDisconnect(player.NickName);

        if (UIManager.S) { 
            UIManager.S.ShowOtherPlayerDisconnectUI(player.NickName);

            // The ShowOtherPlayerDisonnectedUI should have a button that takes you to the survey scene
            
        }

#if HMT_BUILD
        // If we're at the end of a game we might want to stay alive for a bit so the AI can know the game is over

        if (!CompetitionMiddleware.Instance.overrideAIMode) {
            CompetitionMiddleware.Instance.LogEndGame("OtherPlayerDisconnected");
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

    #region Combat RPC

    public void CallSyncExecuteCombat(Combat.FightType type, Tile tile, bool visibility)
    {


        if (IntegratedGameManager.S.isNetworkGame)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                
                Combat.S.ExecuteCombat(type, tile, visibility);
                //Reinitialize seed and send it to all clients to make sure that subsequent monster action is consistent
                randomSeed = Random.Range(0, 10000);
                photonView.RPC("SyncCombatResult", RpcTarget.All, randomSeed, Combat.S.result, Combat.S.charaIDs.ToArray(), Combat.S.charaDiceStats.ToArray(),
                    Combat.S.enemyScores.ToArray(), Combat.S.enemyDiceStats.ToArray(), Combat.S.charaScores.ToArray(), Combat.S.challenges.ToArray(), Combat.S.enemyScore, Combat.S.charaScore, visibility);
            }
            if (CompetitionMiddleware.Instance.IsAI)
                photonView.RPC("CombatResultReadyLocal", RpcTarget.All, CompetitionMiddleware.Instance.RegisteredAgents.Count);
            else 
                photonView.RPC("CombatResultReadyLocal", RpcTarget.All, 1);
        }
        else
        {

            Combat.S.ExecuteCombat(type, tile, visibility);
            CombatResultReadyLocal(3);
            randomSeed = Random.Range(0, 10000);
        }
    }

    [PunRPC]
    private void SyncCombatResult(
        int newSeed,
        bool result,
        int[] charaIDs,
        int[] charaDiceStats,
        int[] enemyScores,
        int[] enemyDiceStats,
        int[] charaScores,
        string[] challenges,
        int enemyScore,
        int charaScore,
        bool visibility)
    {
        randomSeed = newSeed;
        Combat.S.charaIDs = new List<int>(charaIDs);
        Combat.S.result = result;
        Combat.S.charaDiceStats = new List<int>(charaDiceStats);
        Combat.S.enemyScores = new List<int>(enemyScores);
        Combat.S.enemyDiceStats = new List<int>(enemyDiceStats);
        Combat.S.charaScores = new List<int>(charaScores);
        Combat.S.challenges = new List<string>(challenges);
        Combat.S.enemyScore = enemyScore;
        Combat.S.charaScore = charaScore;
    }

    [PunRPC]
    private void CombatResultReadyLocal(int numPlayer)
    {
        IntegratedGameManager.S.CombatResultSyncedCount += numPlayer;
    }



    #endregion




}
