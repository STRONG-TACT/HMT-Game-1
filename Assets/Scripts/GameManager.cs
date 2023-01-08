using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;
    private PhotonView photonView;
    private GameData gameData;
    public GameAssets gameAssets;

    public bool isGameStart;
    public bool[] isFinishedTutorial = new bool[3] { false, false, false };

    public GameObject tilePrefabs;

    // UI
    private GameObject turnIndicator;
    private GameObject actionUI;
    private GameObject goalUI;

    [HideInInspector] public int turn; // Indicate which player's turn
    public GameObject mainPlayer; 
    [HideInInspector] public List<int> playerIDs = new List<int>(); // All the players' Photon ViewID
    [HideInInspector] public int goalCount; // Goal collected, shown on the UI
    [HideInInspector] public int moveLeft; // Player's remaining moves, shown on the UI

    /*    public GameObject Player1Items;
    public GameObject Player2Items;
    public GameObject Player3Items;*/

    private void Awake()
    {
        instance = this;
        playerIDs.Add(0);
        playerIDs.Add(0);
        playerIDs.Add(0);
        gameData = FindObjectOfType<GameData>();
        gameAssets = FindObjectOfType<GameAssets>();

        if (gameData.gameLevel == 1)
        {
            isGameStart = false;
        }
        else
        {
            isGameStart = true;
        }
    }
    private void Start()
    {
        turnIndicator = GameObject.Find("UI").transform.GetChild(0).gameObject;
        actionUI = GameObject.Find("UI").transform.GetChild(1).gameObject;
        goalUI = GameObject.Find("UI").transform.GetChild(2).gameObject;

        
        turn = 1; // Player 1 goes first
        photonView = GetComponent<PhotonView>();

        if (isGameStart) //Start game in the levels that does not have tutorial
        {
            GameObject.Find("WaitForPlayerTxt").SetActive(false);
            turnIndicator.SetActive(true);
            actionUI.SetActive(true);
            goalUI.SetActive(true);
            ChangeActionUI();
            ChangeTurnIndicator();
            //ChangeTurnIndicator();
        }

        if (SceneManager.GetActiveScene().name == "Level_5")
        {
            SetTiles();
        }
        //setVisalbleObject();
    }
    private void Update()
    {
        if (Player.changeTurn) // if turn changed
        {
            Player.changeTurn = false;
            //Debug.Log("Change Turn");
            CallChangeTurn();
        }
    }

    public void CallStartGame()
    {
        photonView.RPC("StartGame", RpcTarget.All);
    }

    public void CallEndTutorial()
    {
        photonView.RPC("EndTutorial", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public void CallChangeTurn()
    {
        photonView.RPC("ChangeTurn", RpcTarget.All);
    }
    public void CallGoalCount(int playerGetGoal)
    {
        photonView.RPC("GoalCount", RpcTarget.All, playerGetGoal);
    }
    public void CallMoveLeft(int num)
    {
        photonView.RPC("MoveLeft", RpcTarget.All, num);
    }
    public void CallAddPlayerID(int playerNum, int id)
    {
        photonView.RPC("AddPlayerID", RpcTarget.All, playerNum, id);
    }
    public void CallEndGame()
    {
        photonView.RPC("EndGame", RpcTarget.All);
    }

    public void CallNextLevel()
    {
        if (gameData.gameLevel == 5)
        {
            CallEndGame();
        }
        else
        {
            photonView.RPC("LoadNextLevel", RpcTarget.All);
        }
    }

    [PunRPC]   
    public void ChangeTurn()
    {
        if (turn == 3) // the next turn for player 3 is player 1
        {
            turn = 1;
        }
        else
        {
            turn += 1;
        }

        ChangeTurnIndicator();
    }

    private void ChangeTurnIndicator()
    {
        /*        if (instance.turn == PhotonNetwork.LocalPlayer.ActorNumber) 
                {
                    turnIndicatorText.text = "Your turn";
                }*/
        Image characterIcon = GameObject.Find("CharactorIcon").GetComponent<Image>();
        if (turn == 1)
        {
            characterIcon.sprite = gameAssets.dwarfIcon;
        }
        else if (turn == 2)
        {
            characterIcon.sprite = gameAssets.giantIcon;
        }
        else if (turn == 3)
        {
            characterIcon.sprite = gameAssets.humanIcon;
        }
    }

    private void ChangeActionUI()
    {
        GameObject actionIcons = GameObject.Find("ActionIcons");
        for (int i = 0; i < 6; i++)
        {
            if (i < moveLeft) 
                actionIcons.transform.GetChild(i).gameObject.SetActive(true);
            else 
                actionIcons.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    private void ChangeGoalUI(string player)
    {
        GameObject goalIcons;
        if (player == "dwarf")
        {
            goalIcons = GameObject.Find("GoalDwarf");
        }
        else if (player == "giant")
        {
            goalIcons = GameObject.Find("GoalGiant");
        }
        else 
        {
            goalIcons = GameObject.Find("GoalHuman");
        }

        Color newColor = goalIcons.GetComponent<Image>().color;
        newColor.a = 1f;
        goalIcons.GetComponent<Image>().color = newColor;
    }

    [PunRPC]
    public void StartGame()
    {
        GameObject.Find("WaitForPlayerTxt").SetActive(false);
        turnIndicator.SetActive(true);
        actionUI.SetActive(true);
        ChangeActionUI();
        ChangeTurnIndicator();
        isGameStart = true;
    }
    [PunRPC]
    public void EndTutorial(int playerNum)
    {
        isFinishedTutorial[playerNum-1] = true;

        if (CheckEndTutoial() && PhotonNetwork.IsMasterClient)
        {
            CallStartGame();
        }
    }

    [PunRPC]
    public void GoalCount(int playerGetGoalNum)
    {
        string playerGetGoal;
        if (playerGetGoalNum == 1)
        {
            playerGetGoal = "dwarf";
        }
        else if (playerGetGoalNum == 2)
        {
            playerGetGoal = "giant";
        }
        else
        {
            playerGetGoal = "human";
        }
        ChangeGoalUI(playerGetGoal);
        goalCount++;
    }

    [PunRPC]
    public void MoveLeft(int num)
    {
        moveLeft = num;
        ChangeActionUI();
    }

    [PunRPC]
    public void AddPlayerID(int playerNum, int id)
    {
        playerIDs[playerNum-1] = id;
    }

    [PunRPC]
    public void EndGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("EndGame"); 
        }
    }

    [PunRPC]
    public void LoadNextLevel()
    {
        int nextLevel = gameData.gameLevel + 1;
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Level_" + nextLevel.ToString()); // Load Next Level
        }
    }

    private void SetTiles()
    {
        GameObject tile;
        float tileDist = gameData.tileSize + gameData.tileGapLength;

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                tile = Instantiate(tilePrefabs, new Vector3(tileDist * i, 0.227f, tileDist * j), Quaternion.identity);
                tile.transform.parent = GameObject.Find("Tiles").transform;
            }
        }
    }

    private bool CheckEndTutoial()
    {
        bool isEnd = true;
        for (int i = 0; i < isFinishedTutorial.Length; i++)
        {
            if (!isFinishedTutorial[i])
            {
                isEnd = false;
            }
        }

        return isEnd;
    }

    /*    private void setVisalbleObject()
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
            {
                Player1Items.SetActive(true);
            }
            else if (PhotonNetwork.LocalPlayer.ActorNumber == 2)
            {
                Player2Items.SetActive(true);
            }
            else if (PhotonNetwork.LocalPlayer.ActorNumber == 3)
            {
                Player3Items.SetActive(true);
            }
        }*/
}
