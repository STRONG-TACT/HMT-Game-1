using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// This is the top level game manager that handles state including current player turn.
/// It is also the root class for all RPC calls through Photon.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance = null;
    private PhotonView photonView;
    private GameData gameData;
    private UIManager uiManager;
    public GameAssets gameAssets;

    public bool isFirstLevel;
    public bool[] isFinishedTutorial = new bool[3] { false, false, false };

    public GameObject tilePrefabs;

    [HideInInspector] public int turn; // Indicate which character's turn
    public Character mainCharacter;
    //[HideInInspector] public List<int> playerIDs = new List<int>(); // All the characters' Photon ViewID
    [HideInInspector] public int goalCount; // Goal collected, shown on the UI
    [HideInInspector] public int currentActionPoints; // Character's remaining moves, shown on the UI

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
    private void Start() {
        turn = 0; // Character 1 goes first
        photonView = GetComponent<PhotonView>();

        //Start game in the levels that does not have tutorial
        if (isFirstLevel) {
            uiManager.HideWaitingForPlayers();
            uiManager.InitGameUI();
            uiManager.UpdateTurnIndicator(gameData.characterConfigs[turn].type);
            uiManager.UpdateActionPointsUI(currentActionPoints);
        }

        //I may want to do this for all levels
        if (SceneManager.GetActiveScene().name == "Level_5") {
            SetTiles();
        }
    }

    public void CallStartGame() {
        photonView.RPC("StartGame", RpcTarget.All);
    }

    [PunRPC]
    public void StartGame() {
        uiManager.HideWaitingForPlayers();
        uiManager.InitGameUI();
        uiManager.UpdateActionPointsUI(currentActionPoints);
        uiManager.UpdateTurnIndicator(gameData.characterConfigs[turn].type);
        isFirstLevel = true;
    }

    public void CallEndTutorial() {
        photonView.RPC("EndTutorial", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    public void EndTutorial(int playerNum) {
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
        turn++;
        turn %= 3;

        uiManager.UpdateTurnIndicator(gameData.characterConfigs[turn].type);
    }

    public void CallGoalCount(int playerGetGoal) {
        photonView.RPC("GoalCount", RpcTarget.All, playerGetGoal);
    }

    [PunRPC]
    public void GoalCount(int playerGetGoalNum) {
        string playerGetGoal;
        if (playerGetGoalNum == 1) {
            playerGetGoal = "dwarf";
        }
        else if (playerGetGoalNum == 2) {
            playerGetGoal = "giant";
        }
        else {
            playerGetGoal = "human";
        }
        uiManager.UpdateGoalUI(playerGetGoal);
        goalCount++;
    }


    public void CallUpdateActionPoints(int num) {
        photonView.RPC("UpdateActionPoints", RpcTarget.All, num);
    }
    
    [PunRPC]
    public void UpdateActionPoints(int num) {
        currentActionPoints = num;
        uiManager.UpdateActionPointsUI(currentActionPoints);
    }

    //Add Player is now handled in the PlayerMapper
    //public void CallAddPlayerID(int playerNum, int id) {
    //    photonView.RPC("AddPlayerID", RpcTarget.All, playerNum, id);
    //}

    //[PunRPC]
    //public void AddPlayerID(int playerNum, int id) {
    //    playerIDs[playerNum - 1] = id;
    //}

    public void CallEndGame() {
        photonView.RPC("EndGame", RpcTarget.All);
    }

    [PunRPC]
    public void EndGame() {
        if (PhotonNetwork.IsMasterClient) {
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
        CallNextTurn();
    }
}
