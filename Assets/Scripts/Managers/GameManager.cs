using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using Photon.Pun.Demo.PunBasics;
using UnityEditor;
using static CombatSystem;

/// <summary>
/// This is the top level game manager that handles state including current player turn.
/// It is also the root class for all RPC calls through Photon.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance = null;
    private PhotonView photonView;
    public GameData gameData {get; private set;}
    private UIManager uiManager;
    public GameAssets gameAssets;
    public bool debugRPCReceipts = false;

    public bool isFirstLevel;
    public bool[] isFinishedTutorial = new bool[3] { false, false, false };

    public GameObject tilePrefabs;

    private int currentPlayersTurn; // Indicate which character's turn
    public Character mainCharacter;
    //[HideInInspector] public List<int> playerIDs = new List<int>(); // All the characters' Photon ViewID
    [HideInInspector] public int goalCount; // Goal collected, shown on the UI
    private int currentActionPoints; // Character's remaining moves, shown on the UI

    public Character CurrentTurnCharacter {
        get {
            return gameData.inSceneCharacters[CurrentTurnCharacterId];
        }
    }
    public int CurrentTurnCharacterId {
        get {
            return PlayerMapper.Instance.GetCharacterFromPlayer(CurrentTurnPlayerNum);
        }
    }

    public int CurrentTurnPlayerNum { get => currentPlayersTurn; private set => currentPlayersTurn = value; }
    public int CurrentActionPoints { get => currentActionPoints; private set => currentActionPoints = value; }

    /*    public GameObject Player1Items;
    public GameObject Player2Items;
    public GameObject Player3Items;*/

    private void Awake() {
        Instance = this;
        //playerIDs.Add(0);
        //playerIDs.Add(0);
        //playerIDs.Add(0);
        uiManager = FindObjectOfType<UIManager>();
        gameData = FindObjectOfType<GameData>();
        gameAssets = FindObjectOfType<GameAssets>();

        if (gameData.gameLevel == 1) {
            isFirstLevel = false;
        }
        else {
            isFirstLevel = true;
        }
    }
    private IEnumerator Start() {
        //CurrentTurnPlayerNum = 1; // Character 1 goes first

        //Debug.LogFormat("Photon Local Player ID:{0}", PhotonNetwork.LocalPlayer.ActorNumber);
        //Debug.LogFormat("Current Turn: {0}", CurrentTurnPlayerNum);
        
        photonView = GetComponent<PhotonView>();

        //Start game in the levels that does not have tutorial
        if (isFirstLevel) {
            uiManager.HideWaitingForPlayers();
            uiManager.InitGameUI();
        }
        //uiManager.UpdateTurnIndicator(gameData.characterConfigs[CurrentTurnPlayerNum].type);
        //uiManager.UpdateActionPointsUI(CurrentActionPoints);

        //I may want to do this for all levels
        if (SceneManager.GetActiveScene().name == "Level_5") {
            SetTiles();
        }

        while (!PlayerMapper.Instance.Inititialized || !gameData.Initialized) {
            yield return null;
        }
        StartTurn(1);
        yield break;
    }

    private void StartTurn(int playerNum) {
        CurrentTurnPlayerNum = playerNum;
        CurrentActionPoints = CurrentTurnCharacter.config.movement;
        uiManager.UpdateTurnIndicator(CurrentTurnCharacter.config.type);
        uiManager.UpdateActionPointsUI(CurrentActionPoints);
    }

    public void CallStartGame() {
        photonView.RPC("StartGame", RpcTarget.All);
    }

    [PunRPC]
    public void StartGame() {
        LogRPCReceipt("StartGame");
        uiManager.HideWaitingForPlayers();
        uiManager.InitGameUI();
        StartTurn(1);
        //uiManager.UpdateActionPointsUI(CurrentActionPoints);
        //uiManager.UpdateTurnIndicator(gameData.characterConfigs[CurrentTurnPlayerNum].type);
        isFirstLevel = true;
    }

    public void CallEndTutorial() {
        photonView.RPC("EndTutorial", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    public void EndTutorial(int playerNum) {
        LogRPCReceipt("EndTutorial: {0}", playerNum);
        isFinishedTutorial[playerNum - 1] = true;

        if (CheckEndTutorial() && PhotonNetwork.IsMasterClient) {
            CallStartGame();
        }
    }

    public void CallNextTurn() {
        photonView.RPC("NextTurn", RpcTarget.All);
    }

    [PunRPC]
    public void NextTurn() {
        LogRPCReceipt("NextTurn");
        switch (CurrentTurnPlayerNum) {
            case 1:
                StartTurn(2);
                break;
            case 2:
                StartTurn(3);
                break;
            case 3:
                if (gameData.inlcudeMonsterTurn) {
                    StartTurn(4);
                }
                else {
                    StartTurn(1);
                }
                break;
            case 4:
                StartTurn(1);
                break;
        }


        //CurrentTurnPlayerNum++;
        ////CurrentTurnPlayerNum %= 3;
        //CallUpdateActionPoints(gameData.characterConfigs[CurrentTurnPlayerNum].movement);
        //uiManager.UpdateTurnIndicator(gameData.characterConfigs[CurrentTurnPlayerNum].type);
    }

    public void CallGoalReached(string characterGoal) {
        photonView.RPC("GoalReached", RpcTarget.All, characterGoal);
    }

    [PunRPC]
    public void GoalReached(string characterGoal) {
        LogRPCReceipt("GoalReached: {0}", characterGoal);
        uiManager.UpdateGoalUI(characterGoal);
        goalCount++;
    }


    public void CallUpdateActionPoints(int num) {
        if (PhotonNetwork.IsMasterClient) {
            photonView.RPC("UpdateActionPoints", RpcTarget.All, num);
        }
    }
    
    [PunRPC]
    public void UpdateActionPoints(int num) {
        LogRPCReceipt("UpdateActionPoints: {0}", num);
        CurrentActionPoints = num;
        uiManager.UpdateActionPointsUI(CurrentActionPoints);
    }

    public void CallMoveCharacter(int characterId, Character.Direction direction) {
        photonView.RPC("MoveCharacter", RpcTarget.All, characterId, direction);
    }

    [PunRPC]
    public void MoveCharacter(int characterId, Character.Direction direction) {
        LogRPCReceipt("MoveCharacter: {0}, {1}", characterId, direction);
        if (characterId == CurrentTurnCharacterId) {
            gameData.inSceneCharacters[characterId].Move(direction);
        }
    }

    public void CallStartFight(int characterPhotonID, int targetPhotonID) {
        photonView.RPC("StartFight", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, characterPhotonID, targetPhotonID);
    }

    [PunRPC]
    public void StartFight(int initiatingPlayer, int characterID, int targetID) {
        LogRPCReceipt("StartFight: {0}, {1}, {2}", initiatingPlayer, characterID, targetID);
        Character character = PhotonNetwork.GetPhotonView(characterID).GetComponent<Character>();
        GameObject target = PhotonNetwork.GetPhotonView(targetID).gameObject;
        CombatSystem.Instance.StartFight(character, target);

        if (initiatingPlayer == PhotonNetwork.LocalPlayer.ActorNumber) {
            //start a fight with UI
        }
        else {
            //start a fight without UI
        }
        
    }

    public void CallRollDie() {
        photonView.RPC("RollDie", RpcTarget.MasterClient);
    }

    [PunRPC]
    public void RollDie() {
        CombatSystem.Instance.RollAttack();
    }

    public void CallEndGame() {
        photonView.RPC("EndGame", RpcTarget.All);
    }

    [PunRPC]
    public void EndGame() {
        if (PhotonNetwork.IsMasterClient) {
            LogRPCReceipt("EndGame");
            PhotonNetwork.LoadLevel("EndGame");
        }
    }

    public void CallNextLevel() {
        if (gameData.gameLevel == 5) {
            CallEndGame();
        }
        else {
            photonView.RPC("LoadNextLevel", RpcTarget.All);
        }
    }

    [PunRPC]
    public void LoadNextLevel() {
        LogRPCReceipt("LoadNextLevel");
        int nextLevel = gameData.gameLevel + 1;
        if (PhotonNetwork.IsMasterClient) {
            PhotonNetwork.LoadLevel("Level_" + nextLevel.ToString()); // Load Next Level
        }
    }

    private void SetTiles() {
        GameObject tile;
        float tileDist = gameData.tileSize + gameData.tileGapLength;

        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                tile = Instantiate(tilePrefabs, new Vector3(tileDist * i, 0.227f, tileDist * j), Quaternion.identity);
                tile.transform.parent = GameObject.Find("Tiles").transform;
            }
        }
    }

    private bool CheckEndTutorial() {
        bool isEnd = true;
        for (int i = 0; i < isFinishedTutorial.Length; i++) {
            if (!isFinishedTutorial[i]) {
                isEnd = false;
            }
        }

        return isEnd;
    }

    internal void EndTurn() {
        if (PhotonNetwork.IsMasterClient) {
            CallNextTurn();
        }
    }

    private void LogRPCReceipt(string formatString, params object[] args) {
        if (debugRPCReceipts && false) {
            Debug.LogFormat("<color=blue>[RPC Recieve]</color> " + formatString, args);
        }
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Escape)) {
            showDebugGUI = !showDebugGUI;
        }
    }

    private bool showDebugGUI = false;
    private Rect debugGUIRect = Rect.zero;
    private Vector2 scrollPos = Vector2.zero;

    private void OnGUI() {
        if (showDebugGUI) {
            if (debugGUIRect == Rect.zero) {
                debugGUIRect = new Rect(0, Screen.height / 2, Screen.width / 2, Screen.height / 2);
            }
            GUILayout.BeginArea(debugGUIRect);
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.Label(string.Format("PhotonPlayerNumber:{0}", PhotonNetwork.LocalPlayer.ActorNumber));
            GUILayout.Label(string.Format("LocalPlayerNumber:{0}", PlayerMapper.Instance.LocalPlayerNumber));
            GUILayout.Label(string.Format("LocalCharacterNumber:{0}", PlayerMapper.Instance.LocalCharacterNumber));
            GUILayout.Label(string.Format("CurrentTurnPlayerNum:{0}", CurrentTurnPlayerNum));
            GUILayout.Label(string.Format("CurrentTurnCharacterId:{0}", CurrentTurnCharacterId));
            GUILayout.Label(string.Format("CurrentActionPoints:{0}", CurrentActionPoints));
            GUILayout.Label(string.Format("GoalReached:{0}", goalCount));
            GUILayout.Label("Characters");
            for(int i = 0; i < gameData.inSceneCharacters.Length; i++) {
                GUILayout.Label(string.Format("Character {0} Character: {1}", i, gameData.inSceneCharacters[i].name));
            }
            GUILayout.Label("Character Mapping");
            Dictionary<int,int> mapping = PlayerMapper.Instance.CharacterMapping;
            foreach(var kvp in mapping) {
                GUILayout.Label(string.Format("Player {0} Character: {1}", kvp.Key, kvp.Value));
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
    }
}
