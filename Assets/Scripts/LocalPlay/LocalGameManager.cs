using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using Photon.Pun.Demo.PunBasics;
using UnityEditor;

public class LocalGameManager : MonoBehaviour
{
    public static LocalGameManager Instance = null;
    public LocalGameData gameData { get; private set; }
    private LocalUIManager uiManager;
    public LocalPlayer player;
    public bool debugRPCReceipts = false;

    public bool isFirstLevel;
    public bool[] isFinishedTutorial = new bool[3] { false, false, false };

    public GameObject tilePrefabs;

    public Character mainCharacter;
    //[HideInInspector] public List<int> playerIDs = new List<int>(); // All the characters' Photon ViewID
    [HideInInspector] public int goalCount; // Goal collected, shown on the UI
    private int currentActionPoints;

    private int planSubmitted = 0;

    private Queue<int> DwarfMoves;
    private Queue<int> GiantMoves;
    private Queue<int> HumanMoves;

    private void Awake()
    {
        Instance = this;
        uiManager = FindObjectOfType<LocalUIManager>();
        gameData = FindObjectOfType<LocalGameData>();
        player = FindObjectOfType<LocalPlayer>();

        if (gameData.gameLevel == 1)
        {
            isFirstLevel = true;
        }
        else
        {
            isFirstLevel = false;
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (!isFirstLevel)
        {
            StartLevel();
        }
        else {
            StartCoroutine(uiManager.PlayTutorial());
        }
    }

    public void StartLevel()
    {
        uiManager.InitGameUI();
        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        //Player start to plan their moves
        planSubmitted = 0;

        PlayerPlanning();
    }

    private void PlayerPlanning()
    {
        // Local version of player planning stage
        player.myCharacter = gameData.inSceneCharacters[planSubmitted];
        player.isPlanning = true;
        player.myCharacter.startPlanning();

        uiManager.ShowCharacterPlanUI(gameData.inSceneCharacters[planSubmitted].name);
    }

    public void PlanUpdated(int currentMoves)
    {
        uiManager.ShowMoveLeft(currentMoves);
    }

    public void PlanSubmitted()
    {
        // When a player submit move plan
        planSubmitted += 1;

        if (planSubmitted == 3)
        {
            CharacterMoving();
        }
        else
        {
            PlayerPlanning();
        }
    }

    public void CharacterMoving()
    {
        // Local version of character moving
    }
}
