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
    public List<LocalMonster> inSceneMonsters;
    private LocalUIManager uiManager;
    public LocalPlayer player;
    public bool debugRPCReceipts = false;

    public bool isFirstLevel;

    public GameObject tilePrefabs;

    public Character mainCharacter;
    //[HideInInspector] public List<int> playerIDs = new List<int>(); // All the characters' Photon ViewID
    public int goalCount; // Goal collected, shown on the UI
    private int currentActionPoints;

    public bool isPlayerTurn = true;
    private int planSubmittedCount = 0;
    private bool[] isSubmitted;
    private bool[] isEmpty;
    private bool[] isFull;
    public int moveFinishedCount = 0;

    private List<int> DwarfPlan;
    private List<int> GiantPlan;
    private List<int> HumanPlan;

    private Queue<int> DwarfMoves;
    private Queue<int> GiantMoves;
    private Queue<int> HumanMoves;
    private Queue<LocalTile> eventQueue;

    private void Awake()
    {
        Instance = this;
        uiManager = FindObjectOfType<LocalUIManager>();
        gameData = FindObjectOfType<LocalGameData>();
        player = FindObjectOfType<LocalPlayer>();
        goalCount = 0;

        for (int i = 0; i < gameData.inSceneMonsters.Length; i++)
        {
            inSceneMonsters.Add(gameData.inSceneMonsters[i]);
        }

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
        isPlayerTurn = true;
        planSubmittedCount = 0;

        DwarfPlan = new List<int>();
        GiantPlan = new List<int>();
        HumanPlan = new List<int>();

        isSubmitted = new bool[3] { false, false, false };
        isEmpty = new bool[3] { true, true, true };
        isFull = new bool[3] { false, false, false };

        StartPlayerPlanningPhase();
    }

    private void StartPlayerPlanningPhase()
    {
        // Local version of player planning stage
        player.myCharacter = gameData.inSceneCharacters[0];
        player.myCharacter.startPlanning();
        player.charaSwitched(0, isSubmitted[0], isEmpty[0], isFull[0]);

        uiManager.ShowCharacterPlanUI(gameData.inSceneCharacters[0].name, getMovePoints(0));
    }

    public void newPlayerMovePlan(int index, int move)
    {
        if (move >= 0 && move < 5)
        {
            updateActionQueue(index, move);
        }

        player.myCharacter.planNewStep((LocalCharacter.Direction)move);

        uiManager.ShowMoveLeft(getMovePoints(index));
    }

    public void switchCharacter(int index)
    {
        player.myCharacter.pausePlanning();
        
        player.myCharacter = gameData.inSceneCharacters[index];
        player.myCharacter.startPlanning();
        uiManager.ShowCharacterPlanUI(gameData.inSceneCharacters[index].name, getMovePoints(index));
        player.charaSwitched(index, isSubmitted[index], isEmpty[index], isFull[index]);
    }

    public void backOneMove(int index)
    {
        if (!isEmpty[index])
        {
            int moveRemoved = moveLastFromActionQueue(index);

            player.myCharacter.backOnePlannedStep((LocalCharacter.Direction)moveRemoved);

            uiManager.ShowMoveLeft(getMovePoints(index));
        }
    }

    public void newPlanSubmitted(int index)
    {
        // When a player submit move plan
        if (!isSubmitted[index])
        {
            planSubmittedCount += 1;
            isSubmitted[index] = true;

            player.planUpdated(true, false, true);
        }

        if (planSubmittedCount == 3)
        {
            StartCharacterMovingPhase();
        }
    }

    public void StartCharacterMovingPhase()
    {
        // Local version of character moving
        Debug.Log("Planning phase ended.");
        uiManager.HideCharacterPlanUI();

        foreach(LocalCharacter chara in gameData.inSceneCharacters)
        {
            chara.endPlanning();
        }

        moveFinishedCount = 0;
        DwarfMoves = new Queue<int>();
        GiantMoves = new Queue<int>();
        HumanMoves = new Queue<int>();
        eventQueue = new Queue<LocalTile>();

        for (int i = 0; i < DwarfPlan.Count; i ++)
        {
            DwarfMoves.Enqueue(DwarfPlan[i]);
        }
        for (int i = 0; i < GiantPlan.Count; i++)
        {
            GiantMoves.Enqueue(GiantPlan[i]);
        }
        for (int i = 0; i < HumanPlan.Count; i++)
        {
            HumanMoves.Enqueue(HumanPlan[i]);
        }

        StartCoroutine(CharacterMoveByStep());
    }

    private IEnumerator CharacterMoveByStep()
    {
        uiManager.ShowCharacterMovingUI();

        // A flag, whether this round of step triggers a combat
        bool hasCombat = false;

        if (moveFinishedCount <= 0)
        {
            isEmpty = new bool[3] { false, false, false };
        }

        while (moveFinishedCount < 3)
        {
            if (!isEmpty[0])
            {
                //Debug.Log("In Loop.");
                if (DwarfMoves.Count == 0)
                {
                    isEmpty[0] = true;
                    moveFinishedCount += 1;
                }
                else
                {
                    gameData.inSceneCharacters[0].moveOneStep((LocalCharacter.Direction)DwarfMoves.Dequeue());
                }
            }

            if (!isEmpty[1])
            {
                if (GiantMoves.Count == 0)
                {
                    isEmpty[1] = true;
                    moveFinishedCount += 1;
                }
                else
                {
                    gameData.inSceneCharacters[1].moveOneStep((LocalCharacter.Direction)GiantMoves.Dequeue());
                }
            }

            if (!isEmpty[2])
            {
                if (HumanMoves.Count == 0)
                {
                    isEmpty[2] = true;
                    moveFinishedCount += 1;
                }
                else
                {
                    gameData.inSceneCharacters[2].moveOneStep((LocalCharacter.Direction)HumanMoves.Dequeue());
                }
            }

            yield return new WaitForSeconds(0.8f);

            if (eventQueue.Count != 0)
            {
                StartCoroutine(ExecuteCombatOneByOne());
                hasCombat = true;
                break;
            }
        }

        // If everyone finishes and no combat happens
            // if there are combats, deal with them and then come back
        if (moveFinishedCount == 3 && !hasCombat)
        {
            Debug.Log("Moving phase ended.");
            StartMonsterTurn();
        }
    }

    private void StartMonsterTurn()
    {
        isPlayerTurn = false;
        moveFinishedCount = 0;

        foreach (LocalMonster m in inSceneMonsters)
        {
            m.MonsterTurnStart();
        }

        StartCoroutine(MonsterMoveByStep());
    }

    private IEnumerator MonsterMoveByStep()
    {
        uiManager.ShowMonsterTurnUI();
        bool hasCombat = false;

        while (moveFinishedCount < inSceneMonsters.Count)
        {
            foreach(LocalMonster m in inSceneMonsters)
            {
                if (!m.turnFinished)
                {
                    // if monster still have moves
                    m.moveOneStep();
                }
            }

            yield return new WaitForSeconds(1.2f);

            if (eventQueue.Count != 0)
            {
                StartCoroutine(ExecuteCombatOneByOne());
                hasCombat = true;
                break;
            }
        }

        if (moveFinishedCount == inSceneMonsters.Count && !hasCombat)
        {
            Debug.Log("Monster moving phase ended.");
            StartPlayerTurn();
        }
    }

    public void monsterMoveFinished()
    {
        moveFinishedCount += 1;
    }

    private IEnumerator ExecuteCombatOneByOne()
    {
        Debug.Log("An event happened.");

        while (eventQueue.Count != 0)
        {
            bool win = false;
            LocalTile t = eventQueue.Dequeue();

            if (!t.isBarrier)
            {
                win = Combat.ExecuteCombat(Combat.FightType.Monster, t, uiManager);
            }
            else
            {
                if (t.tileType == LocalTile.TileType.Trap)
                {
                    win = Combat.ExecuteCombat(Combat.FightType.Trap, t, uiManager);
                }else if (t.tileType == LocalTile.TileType.Rock)
                {
                    win = Combat.ExecuteCombat(Combat.FightType.Rock, t, uiManager);
                }
            }

            if (win)
            {
                // if the character(s) won the battle, destory the enemies
                Debug.Log("Character won.");

                foreach (LocalMonster m in t.enemyList)
                {
                    inSceneMonsters.Remove(m);
                    Destroy(m.gameObject);
                }

                t.enemyList.Clear();
            }
            else
            {
                // If not, reduce health
                // If character's turn, all remaining steps should be cleared.
                Debug.Log("Enemy won.");

                foreach (LocalCharacter c in t.charaList)
                {
                    c.HealthReduced();

                    if (isPlayerTurn)
                    {
                        switch (c.CharacterId)
                        {
                            case 0:
                                DwarfMoves.Clear();
                                break;
                            case 1:
                                GiantMoves.Clear();
                                break;
                            case 2:
                                HumanMoves.Clear();
                                break;
                            default:
                                break;
                        }
                    }
                }
                
                
            }

            yield return new WaitForSeconds(1.5f);
        }

        if (isPlayerTurn)
        {
            StartCoroutine(CharacterMoveByStep());
        }else if (!isPlayerTurn)
        {
            StartCoroutine(MonsterMoveByStep());
        }
    }

    public void updateEventQueue(LocalTile tile)
    {
        eventQueue.Enqueue(tile);
    }

    private int getMovePoints(int index)
    {
        switch (index)
        {
            case 0:
                return gameData.inSceneCharacters[index].config.movement - DwarfPlan.Count;

            case 1:
                return gameData.inSceneCharacters[index].config.movement - GiantPlan.Count;

            case 2:
                return gameData.inSceneCharacters[index].config.movement - HumanPlan.Count;

            default:
                return -1;
        }
    }

    private void updateActionQueue(int index, int move)
    {
        switch (index)
        {
            case 0:
                DwarfPlan.Add(move);

                if(gameData.inSceneCharacters[index].config.movement == DwarfPlan.Count)
                {
                    isFull[index] = true;
                }
                else if (DwarfPlan.Count == 1)
                {
                    isEmpty[index] = false;
                }

                break;
            case 1:
                GiantPlan.Add(move);

                if (gameData.inSceneCharacters[index].config.movement == GiantPlan.Count)
                {
                    isFull[index] = true;
                }
                else if (GiantPlan.Count == 1)
                {
                    isEmpty[index] = false;
                }

                break;
            case 2:
                HumanPlan.Add(move);

                if (gameData.inSceneCharacters[index].config.movement == HumanPlan.Count)
                {
                    isFull[index] = true;
                }
                else if (HumanPlan.Count == 1)
                {
                    isEmpty[index] = false;
                }


                break;
            default:
                break;
        }

        player.planUpdated(false, false, isFull[index]);
    }

    private int moveLastFromActionQueue(int index)
    {
        int move = -1;
        switch (index)
        {
            case 0:
                move = DwarfPlan[DwarfPlan.Count - 1];
                DwarfPlan.RemoveAt(DwarfPlan.Count - 1);

                if (DwarfPlan.Count == 0)
                {
                    isEmpty[index] = true;
                }
                if (isFull[index])
                {
                    isFull[index] = false;
                }
                
                break;
            case 1:
                move = GiantPlan[GiantPlan.Count - 1];
                GiantPlan.RemoveAt(GiantPlan.Count - 1);

                if (GiantPlan.Count == 0)
                {
                    isEmpty[index] = true;
                }
                if (isFull[index])
                {
                    isFull[index] = false;
                }

                break;
            case 2:
                move = HumanPlan[HumanPlan.Count - 1];
                HumanPlan.RemoveAt(HumanPlan.Count - 1);

                if (HumanPlan.Count == 0)
                {
                    isEmpty[index] = true;
                }
                if (isFull[index])
                {
                    isFull[index] = false;
                }

                break;
            default:
                break;
        }

        player.planUpdated(false, isEmpty[index], false);
        return move;
    }

    public void GoalReached(int charaID)
    {
        goalCount += 1;
    }

    public void NextLevel()
    {
        Debug.Log("Moving to next level.");
    }
}
